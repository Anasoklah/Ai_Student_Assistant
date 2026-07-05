import os

from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from services.openrouter_service import OpenRouterService
from services.groq_service import GroqService
from Jobs.JobStore import JobStore
from models.dto import PageResult


class ExtractionManager:
    """
    Orchestrates PDF extraction with a multi-provider fallback strategy:
    1. Render each page as an image
    2. Try Gemini vision → 429 → OpenRouter → fails → Groq
    """

    def __init__(self, pdf_service: PdfSliceService, gemini_service: GeminiService,
                 openrouter_service: OpenRouterService, groq_service: GroqService,
                 job_store: JobStore, logger, config):
        self.pdf_service = pdf_service
        self.gemini_service = gemini_service
        self.openrouter_service = openrouter_service
        self.groq_service = groq_service
        self.job_store = job_store
        self.logger = logger
        self.config = config

    def _try_extract_image(self, page_number: int, image_bytes: bytes):
        """Try image extraction: Gemini → OpenRouter → Groq."""
        # 1. Try Gemini
        try:
            result = self.gemini_service.extract_concepts_from_image(page_number, image_bytes)
            if result.success and result.concepts:
                return result
        except Exception as e:
            if "RESOURCE_EXHAUSTED" not in str(e):
                self.logger.warning(f"Gemini failed on page {page_number}: {e}")

        # 2. Try OpenRouter
        self.logger.info(f"Trying OpenRouter on page {page_number}...")
        try:
            result = self.openrouter_service.extract_concepts_from_image(page_number, image_bytes)
            if result.success and result.concepts:
                return result
        except Exception as e:
            self.logger.warning(f"OpenRouter failed on page {page_number}: {e}")

        # 3. Try Groq
        self.logger.info(f"Trying Groq on page {page_number}...")
        return self.groq_service.extract_concepts_from_image(page_number, image_bytes)

    def process_pdf_in_background(self, pdf_path: str, book_id: str, job_id: str):
        """
        Processes an already-sliced PDF (only contains the requested pages).
        Renders each page as an image and sends it to the LLM for extraction.
        """
        self.logger.info(f"Starting background job {job_id} for book_id: {book_id}, path: {pdf_path}")

        try:
            for page_num, _ in self.pdf_service.extract_pages(pdf_path):
                self.logger.info(f"Processing page {page_num} for book {book_id} (job {job_id})...")

                # Render page as image and send to LLM
                image_bytes = self.pdf_service.render_page_as_image(pdf_path, page_num)
                if image_bytes:
                    result = self._try_extract_image(page_num, image_bytes)
                else:
                    self.logger.error(f"Failed to render page {page_num} as image.")
                    from models.dto import ExtractionResponse
                    result = ExtractionResponse(success=False, page_number=page_num, concepts=[],
                                                error_message="Failed to render page as image.")

                self.job_store.add_page_result(
                    job_id,
                    PageResult(
                        page_number=page_num,
                        success=result.success,
                        concepts=result.concepts,
                        error_message=result.error_message,
                    ),
                )

            self.job_store.mark_ready(job_id)
            self.logger.info(f"Job {job_id} (book {book_id}) completed successfully.")

        except Exception as e:
            self.logger.error(f"Fatal error in job {job_id} for book {book_id}: {str(e)}")
            self.job_store.mark_failed(job_id, str(e))

        finally:
            if os.path.exists(pdf_path):
                os.remove(pdf_path)
                self.logger.info(f"Cleaned up sliced temp file for job {job_id}: {pdf_path}")
