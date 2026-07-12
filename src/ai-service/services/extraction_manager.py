import os
from enum import Enum

from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from services.groq_service import GroqService
from services.openrouter_service import OpenRouterService
from Jobs.JobStore import JobStore
from models.dto import PageResult , DocumentStructure
from services.structure_extractor import StructureExtractor
from services.toc_parser import TocParser


class HeadingLevel(str, Enum):
    CHAPTER = "Chapter"
    UNIT = "Unit"
    LESSON = "Lesson"
    UNKNOWN = "Unknown"


def ClassifyArabicHeadingLevel(text: str, text_level: int | None) -> HeadingLevel:
    if not text or not text.strip():
        return HeadingLevel.UNKNOWN

    text_lower = text.lower()
    if "الفصل" in text_lower:
        return HeadingLevel.CHAPTER
    if "الوحدة" in text_lower:
        return HeadingLevel.UNIT
    if "الدرس" in text_lower:
        return HeadingLevel.LESSON
    return HeadingLevel.UNKNOWN


class ExtractionManager:
    def __init__(self, pdf_service: PdfSliceService, gemini_service: GeminiService,
                 groq_service: GroqService, openrouter_service: OpenRouterService,
                 job_store: JobStore, logger, config):
        self.pdf_service = pdf_service
        self.gemini_service = gemini_service
        self.groq_service = groq_service
        self.openrouter_service = openrouter_service
        self.job_store = job_store
        self.logger = logger
        self.config = config

        self.toc_parser = TocParser(logger)
        self.structure_extractor = StructureExtractor(
            pdf_service, gemini_service, groq_service, openrouter_service,
            self.toc_parser, logger, config
        )

    def _assess_text_quality(self, text: str) -> float:
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
        providers = []
        for provider_name in getattr(self.config, "PROVIDER_PRIORITY", [ "groq", "gemini","openrouter"]):
            provider_name = provider_name.strip().lower()
            if provider_name == "gemini":
                providers.append(("gemini_text" if not is_vision else "gemini_vision", self.gemini_service.extract_concepts_from_text if not is_vision else self.gemini_service.extract_concepts_from_image))
            elif provider_name == "groq":
                providers.append(("groq_text" if not is_vision else "groq_vision", self.groq_service.extract_concepts_from_text if not is_vision else self.groq_service.extract_concepts_from_image))
            elif provider_name == "openrouter":
                providers.append(("openrouter_text" if not is_vision else "openrouter_vision", self.openrouter_service.extract_concepts_from_text if not is_vision else self.openrouter_service.extract_concepts_from_image))

        return providers

    def _try_provider(self, page_num: int, text: str, pdf_path: str, is_vision: bool):
        providers = self._get_provider_order(is_vision)
        retry_count = max(1, int(getattr(self.config, "PROVIDER_RETRY_COUNT", 1)))
        last_error = None

        for provider_name, provider_call in providers:
            for attempt in range(retry_count):
                try:
                    if not is_vision:
                        result = provider_call(page_num, text)
                    else:
                        image_bytes = self.pdf_service.render_page_as_image(pdf_path, page_num)
                        result = provider_call(page_num, image_bytes)

                    if getattr(result, "success", False) and getattr(result, "concepts", None):
                        return result, provider_name

                    if getattr(result, "error_message", None):
                        last_error = result.error_message
                        self.logger.warning(f"Provider {provider_name} failed for page {page_num}: {result.error_message}")
                except Exception as exc:
                    last_error = str(exc)
                    self.logger.warning(f"Provider {provider_name} failed for page {page_num}: {exc}")

                if attempt < retry_count - 1:
                    self.logger.info(f"Retrying provider {provider_name} for page {page_num} (attempt {attempt + 2}/{retry_count})")

        return None, last_error or "All providers failed"

    def extract_single_image(self, image_bytes: bytes, page_number: int = 1) -> tuple:
        """
        Extract concepts from a single image (screenshot, PNG, JPG, etc.).
        Tries providers in priority order with fallback.
        Returns (ExtractionResponse | None, provider_name | error_message).
        """
        self.logger.info(f"Starting single image extraction ({len(image_bytes)} bytes)")

        providers = self._get_provider_order(is_vision=True)
        retry_count = max(1, int(getattr(self.config, "PROVIDER_RETRY_COUNT", 1)))

        for provider_name, provider_call in providers:
            for attempt in range(retry_count):
                try:
                    result = provider_call(page_number, image_bytes)

                    if getattr(result, "success", False) and getattr(result, "concepts", None):
                        self.logger.info(f"Image extraction succeeded with {provider_name}")
                        return result, provider_name

                    if getattr(result, "error_message", None):
                        self.logger.warning(f"Provider {provider_name} failed for image: {result.error_message}")

                except Exception as exc:
                    self.logger.warning(f"Provider {provider_name} failed for image: {exc}")

                if attempt < retry_count - 1:
                    self.logger.info(f"Retrying provider {provider_name} for image (attempt {attempt + 2}/{retry_count})")

        self.logger.error("All providers failed for image extraction")
        return None, "All providers failed"

    def process_pdf_in_background(
    self,
    pdf_path: str,
    book_id: str,
    job_id: str,
    ):
        self.logger.info(f"Starting background job {job_id} for book_id: {book_id}, path: {pdf_path}")

        try:
            total_pages = self.pdf_service.count_pages(pdf_path)
            self.logger.info(f"PDF has {total_pages} total pages")
            
            
            
            self.logger.info("Step 2: Processing pages for concept extraction...")
            
            for page_num, text in self.pdf_service.extract_pages(pdf_path):
              self._process_single_page(
                pdf_path=pdf_path,
                book_id=book_id,
                job_id=job_id,
                page_num=page_num,
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

    def _process_single_page(
    self,
    pdf_path,
    book_id,
    job_id,
    page_num,
    text,
    ):
        """
        Process a single page: extract concepts and tag with chapter/section context.
        
        Strategy:
        1. Assess text quality via OCR
        2. If text quality >= 0.7: try text-based extraction using providers in priority order
        3. If text quality < 0.7 or text providers all failed: try vision-based extraction 
           using providers in priority order
        """

        self.logger.info(f"Processing page {page_num} for book {book_id} (job {job_id})...")

        quality_score = self._assess_text_quality(text)
        self.logger.info(f"Page {page_num} text quality: {quality_score:.2f}")

        result = None
        extraction_service = None

        if quality_score >= 0.7:
            # Text quality is good — try text-based extraction with provider priority
            self.logger.info(f"Page {page_num}: text quality good, trying text-based extraction")
            result, extraction_service = self._try_provider(page_num, text, pdf_path, is_vision=False)
        
        if result is None:
            # Text extraction failed or text quality was low — fall back to vision
            self.logger.info(f"Page {page_num}: trying vision-based extraction")
            result, extraction_service = self._try_provider(page_num, text, pdf_path, is_vision=True)

        
        self.job_store.add_page_result(
            job_id,
           PageResult(
            page_number=page_num,
            success=bool(getattr(result, "success", False)) if result else False,
            concepts=getattr(result, "concepts", []) if result else [],
            error_message=getattr(result, "error_message", "All providers failed") if not result else None,
            extraction_service=extraction_service,
            text_quality_score=quality_score,
            )
        )

    def extract_book_structure(
    self,
    pdf_path: str,
    toc_page: int,
    ) -> DocumentStructure | None:
        """
        Extract the document structure from the Table of Contents.
        This is intended to be called once when a new book is imported.
        """

        total_pages = self.pdf_service.count_pages(pdf_path)

        self.logger.info(
            f"Extracting document structure from TOC page {toc_page}"
        )

        structure = self.structure_extractor.extract_structure(
            pdf_path=pdf_path,
            total_pages=total_pages,
            toc_page=toc_page,
        )
        
        if structure is None:
         self.logger.warning("Structure extraction failed.")

        return structure