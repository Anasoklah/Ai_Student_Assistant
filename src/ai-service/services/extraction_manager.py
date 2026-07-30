import os
import time

from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from services.groq_service import GroqService
from services.openrouter_service import OpenRouterService
from services.opencode_service import OpenCodeService
from Jobs.JobStore import JobStore
from models.dto import PageResult , DocumentStructure
from services.structure_extractor import StructureExtractor
from services.toc_parser import TocParser
from services.extraction_validation import validate_extraction, QualityReport
from services.provider_roles import provider_allows

# FLOW OF FUNCTIONS:
#
# process_pdf_in_background (Public) -> Main entry point for background PDF processing.
#   └── _process_single_page (Private)
#       ├── _assess_text_quality (Private)
#       └── _try_provider (Private)
#           ├── _get_provider_order (Private)
#           └── _throttle (Private)
#
# extract_single_image (Public) -> Entry point for processing a single image upload.
#   ├── _get_provider_order (Private)
#   └── _throttle (Private)
#
# extract_book_structure (Public) -> Entry point for extracting TOC structure.

# A page whose OCR text is at least this long plausibly contains real
# educational content; an empty extraction from it is a quality failure
# (the "valid JSON, empty concepts" case) rather than a legitimately blank page.
_SOURCE_CONTENT_MIN_CHARS = 50


class ExtractionManager:
    """
    Orchestrates the extraction of educational concepts from PDFs and images.
    Uses multiple AI providers with a priority-based fallback and quality validation.
    """

    def __init__(self, pdf_service: PdfSliceService, gemini_service: GeminiService,
                 groq_service: GroqService, openrouter_service: OpenRouterService,
                 job_store: JobStore, logger, config,
                 opencode_service: OpenCodeService | None = None):
        self.pdf_service = pdf_service
        self.gemini_service = gemini_service
        self.groq_service = groq_service
        self.openrouter_service = openrouter_service
        self.opencode_service = opencode_service
        self.job_store = job_store
        self.logger = logger
        self.config = config

        self.toc_parser = TocParser(logger)
        self.structure_extractor = StructureExtractor(
            pdf_service, gemini_service, groq_service, openrouter_service,
            self.toc_parser, logger, config
        )
        
        self._last_request_time: dict[str, float] = {}
        self._min_interval = {
            "groq": 60 / 15,        # pace to 15 RPM
            "gemini": 60 / 10,      # pace to 10 RPM
            "openrouter": 60 / 20,  # pace to 20 RPM
            "opencode": 60 / 30,    # pace to 30 RPM
        }

    # --- PUBLIC METHODS ---

    def process_pdf_in_background(
        self,
        pdf_path: str,
        book_id: str,
        job_id: str,
        page_start: int = 1,
    ):
        """
        Public entry point for background processing of a PDF file.
        Iterates through pages and triggers extraction for each.
        """
        self.logger.info(f"Starting background job {job_id} for book_id: {book_id}, path: {pdf_path}")

        try:
            total_pages = self.pdf_service.count_pages(pdf_path)
            self.logger.info(f"PDF has {total_pages} total pages")
            
            self.logger.info("Step 2: Processing pages for concept extraction...")
            
            for page_num, text in self.pdf_service.extract_pages(pdf_path):
                original_page_num = page_num + page_start - 1
                self._process_single_page(
                    pdf_path=pdf_path,
                    book_id=book_id,
                    job_id=job_id,
                    page_num=original_page_num,
                    local_page_num=page_num,
                    text=text,
                )

            self.job_store.mark_ready(job_id)
            self.logger.info(f"Job {job_id} (book {book_id}) completed successfully.")

        except Exception as e:
            self.logger.error(f"Fatal error in job {job_id} for book {book_id}: {str(e)}")
            self.job_store.mark_failed(job_id, str(e))

        finally:
            if os.path.exists(pdf_path):
                os.remove(pdf_path)
                self.logger.info(f"Cleaned up temp file for job {job_id}: {pdf_path}")

    def extract_single_image(self, image_bytes: bytes, page_number: int = 1) -> tuple:
        """
        Public entry point to extract concepts from a single uploaded image.
        Tries providers in priority order until one succeeds validation.
        """
        self.logger.info(f"Starting single image extraction ({len(image_bytes)} bytes)")

        providers = self._get_provider_order(is_vision=True)
        retry_count = max(1, int(getattr(self.config, "PROVIDER_RETRY_COUNT", 1)))
        keywords_min = int(getattr(self.config, "KEYWORDS_MIN", 3))
        keywords_max = int(getattr(self.config, "KEYWORDS_MAX", 7))

        last_error = None
        best_effort = None          # (result, provider_name)
        best_effort_score = -1.0

        for provider_name, provider_call in providers:
            for attempt in range(retry_count):
                try:
                    self._throttle(provider_name)
                    result = provider_call(page_number, image_bytes)

                    if getattr(result, "error_message", None):
                        last_error = result.error_message

                    report = validate_extraction(
                        result,
                        source_has_content=False,
                        keywords_min=keywords_min,
                        keywords_max=keywords_max,
                    )
                    self.logger.info(
                        f"image-extraction provider={provider_name} "
                        f"attempt={attempt + 1}/{retry_count} {report.summary()}"
                    )

                    if report.accepted and getattr(result, "concepts", None):
                        self.logger.info(f"Image extraction succeeded with {provider_name}")
                        return result, provider_name, False

                    if getattr(result, "concepts", None) and report.score > best_effort_score:
                        best_effort = (result, provider_name)
                        best_effort_score = report.score

                    if getattr(result, "error_message", None):
                        self.logger.warning(f"Provider {provider_name} failed for image: {result.error_message}")

                except Exception as exc:
                    last_error = str(exc)
                    self.logger.warning(f"Provider {provider_name} failed for image: {exc}")

                if attempt < retry_count - 1:
                    self.logger.info(f"Retrying provider {provider_name} for image (attempt {attempt + 2}/{retry_count})")

        if best_effort is not None:
            result, provider_name = best_effort
            self.logger.warning(
                f"image-extraction no provider passed validation; storing best-effort "
                f"from {provider_name} score={best_effort_score:.2f} needs_review=True"
            )
            return result, provider_name, True

        self.logger.error("All providers failed for image extraction")
        return None, last_error or "All providers failed", False

    def extract_book_structure(
        self,
        pdf_path: str,
        toc_page: int,
        toc_page_end: int | None = None
    ) -> DocumentStructure | None:
        """
        Public entry point to extract the document structure from the Table of Contents.
        """
        total_pages = self.pdf_service.count_pages(pdf_path)
        end_page = toc_page_end or toc_page

        self.logger.info(f"Extracting document structure from TOC pages {toc_page}-{end_page}")

        structure = self.structure_extractor.extract_structure(
            pdf_path=pdf_path,
            total_pages=total_pages,
            toc_page=toc_page,
            toc_page_end=end_page,
        )
        
        if structure is None:
            self.logger.warning("Structure extraction failed.")

        return structure

    # --- PRIVATE METHODS (ALPHABETICAL/LOGICAL ORDER) ---

    def _assess_text_quality(self, text: str) -> float:
        """
        Evaluates the quality of OCR-extracted text.
        Returns a score between 0.0 and 1.0.
        """
        if not text or len(text) < 20:
            return 0.0

        score = 1.0
        corruption_chars = ["▓", "▒", "░", "□", "■", "●"]
        corruption_count = sum(text.count(char) for char in corruption_chars)
        if corruption_count:
            score -= min(0.5, corruption_count * 0.1)

        try:
            text.encode("utf-8").decode("utf-8")
        except UnicodeDecodeError:
            score -= 0.3

        lines = text.splitlines()
        if len(lines) > 200 or (lines and sum(len(line) for line in lines) / len(lines) < 5):
            score -= 0.2

        non_alpha_ratio = sum(1 for char in text if not char.isalnum() and not char.isspace()) / len(text)
        if non_alpha_ratio > 0.3:
            score -= 0.25

        arabic_chars = sum(1 for char in text if "\u0600" <= char <= "\u06FF")
        if arabic_chars > 0 and arabic_chars / len(text) < 0.1:
            score -= 0.2

        return max(0.0, score)

    def _get_provider_order(self, is_vision: bool):
        """
        Determines the order of AI providers based on configuration and capability.
        """
        providers = []
        priority_list = getattr(self.config, "PROVIDER_PRIORITY", ["groq", "gemini", "openrouter"])
        
        for provider_name in priority_list:
            provider_name = provider_name.strip().lower()
            if not provider_allows(self.config, provider_name, is_vision):
                continue
                
            if provider_name == "gemini":
                providers.append((
                    "gemini_text" if not is_vision else "gemini_vision", 
                    self.gemini_service.extract_concepts_from_text if not is_vision else self.gemini_service.extract_concepts_from_image
                ))
            elif provider_name == "groq":
                providers.append((
                    "groq_text" if not is_vision else "groq_vision", 
                    self.groq_service.extract_concepts_from_text if not is_vision else self.groq_service.extract_concepts_from_image
                ))
            elif provider_name == "openrouter":
                providers.append((
                    "openrouter_text" if not is_vision else "openrouter_vision", 
                    self.openrouter_service.extract_concepts_from_text if not is_vision else self.openrouter_service.extract_concepts_from_image
                ))
            elif provider_name == "opencode":
                if self.opencode_service is not None and self.opencode_service.enabled:
                    providers.append((
                        "opencode_text" if not is_vision else "opencode_vision", 
                        self.opencode_service.extract_concepts_from_text if not is_vision else self.opencode_service.extract_concepts_from_image
                    ))

        if not providers:
            self.logger.warning(
                f"No providers resolved for the {'vision' if is_vision else 'text'} path."
            )

        return providers

    def _process_single_page(
        self,
        pdf_path,
        book_id,
        job_id,
        page_num,
        text,
        local_page_num=None,
    ):
        """
        Internal logic to process a single PDF page.
        Attempts text extraction first if quality is good, else falls back to vision.
        """
        self.logger.info(f"Processing page {page_num} for book {book_id} (job {job_id})...")

        quality_score = self._assess_text_quality(text)
        self.logger.info(f"Page {page_num} text quality: {quality_score:.2f}")

        result = None
        extraction_service = None
        needs_review = False

        if quality_score >= 0.7:
            self.logger.info(f"Page {page_num}: text quality good, trying text-based extraction")
            result, extraction_service, needs_review = self._try_provider(
                page_num, text, pdf_path, is_vision=False, render_page_num=local_page_num
            )

        if result is None:
            self.logger.info(f"Page {page_num}: trying vision-based extraction")
            result, extraction_service, needs_review = self._try_provider(
                page_num, text, pdf_path, is_vision=True, render_page_num=local_page_num
            )

        self.job_store.add_page_result(
            job_id,
            PageResult(
                page_number=page_num,
                success=bool(getattr(result, "success", False)) if result else False,
                concepts=getattr(result, "concepts", []) if result else [],
                error_message=getattr(result, "error_message", "All providers failed") if not result else None,
                extraction_service=extraction_service,
                text_quality_score=quality_score,
                needs_review=needs_review if result else False,
            )
        )

    def _throttle(self, provider_key: str):
        """
        Implements rate limiting for providers to avoid hitting API ceilings.
        """
        base_provider = provider_key.split("_")[0]
        min_gap = self._min_interval.get(base_provider, 2.0)
        last = self._last_request_time.get(provider_key, 0)
        elapsed = time.monotonic() - last
        if elapsed < min_gap:
            time.sleep(min_gap - elapsed)
        self._last_request_time[provider_key] = time.monotonic()

    def _try_provider(self, page_num: int, text: str, pdf_path: str, is_vision: bool, render_page_num: int | None = None):
        """
        Attempts extraction using available providers in priority order.
        Returns the first accepted result or the best-effort result if all fail.
        """
        providers = self._get_provider_order(is_vision)
        retry_count = max(1, int(getattr(self.config, "PROVIDER_RETRY_COUNT", 1)))
        last_error = None
        render_page_num = render_page_num if render_page_num is not None else page_num

        source_has_content = (not is_vision) and bool(text and text.strip())
        keywords_min = int(getattr(self.config, "KEYWORDS_MIN", 3))
        keywords_max = int(getattr(self.config, "KEYWORDS_MAX", 7))

        best_effort = None
        best_effort_score = -1.0

        for provider_name, provider_call in providers:
            for attempt in range(retry_count):
                self._throttle(provider_name)
                report = None
                try:
                    if not is_vision:
                        result = provider_call(page_num, text)
                    else:
                        image_bytes = self.pdf_service.render_page_as_image(pdf_path, render_page_num)
                        result = provider_call(page_num, image_bytes)

                    if getattr(result, "error_message", None):
                        last_error = result.error_message

                    report = validate_extraction(
                        result,
                        source_has_content=source_has_content,
                        keywords_min=keywords_min,
                        keywords_max=keywords_max,
                    )
                    self.logger.info(
                        f"extraction page={page_num} provider={provider_name} "
                        f"attempt={attempt + 1}/{retry_count} {report.summary()}"
                    )

                    if report.accepted:
                        return result, provider_name, False

                    if getattr(result, "concepts", None) and report.score > best_effort_score:
                        best_effort = (result, provider_name)
                        best_effort_score = report.score

                    if getattr(result, "error_message", None):
                        self.logger.warning(
                            f"Provider {provider_name} rejected for page {page_num}: "
                            f"{result.error_message}"
                        )
                except Exception as exc:
                    last_error = str(exc)
                    self.logger.warning(f"Provider {provider_name} failed for page {page_num}: {exc}")

                if attempt < retry_count - 1:
                    self.logger.info(f"Retrying provider {provider_name} for page {page_num} (attempt {attempt + 2}/{retry_count})")

        if best_effort is not None:
            result, provider_name = best_effort
            self.logger.warning(
                f"extraction page={page_num} no provider passed validation; "
                f"storing best-effort from {provider_name} "
                f"score={best_effort_score:.2f} needs_review=True"
            )
            return result, provider_name, True

        return None, last_error or "All providers failed", False
