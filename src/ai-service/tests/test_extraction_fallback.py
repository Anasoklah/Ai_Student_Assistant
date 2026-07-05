from services.extraction_manager import ExtractionManager
from services.groq_service import GroqService
from models.dto import ExtractionResponse


class DummyLogger:
    def info(self, message):
        pass

    def warning(self, message):
        pass

    def error(self, message):
        pass


class DummyStore:
    def __init__(self):
        self.pages = []

    def create(self, **kwargs):
        pass

    def add_page_result(self, job_id, page_result):
        self.pages.append(page_result)

    def mark_ready(self, *args, **kwargs):
        pass

    def mark_failed(self, *args, **kwargs):
        pass


def test_assess_text_quality_flags_corrupted_text():
    manager = ExtractionManager(None, None, None, None, DummyStore(), DummyLogger(), None)

    corrupted = "▓▒░■● this is mostly garbage"
    readable = "هذا نص واضح ومفهوم ومناسب لاستخراج المفاهيم التعليمية"

    assert manager._assess_text_quality(corrupted) < 0.7
    assert manager._assess_text_quality(readable) >= 0.7


def test_groq_service_initializes_enabled_flag_when_unconfigured():
    class DummyConfig:
        GROQ_API_KEY = ""
        GROQ_MODEL = "llama-3.1-8b"
        GROQ_TIMEOUT_SECONDS = 30

    service = GroqService(DummyConfig(), DummyLogger())

    assert hasattr(service, "enabled") is True
    assert service.enabled is False

    result = service.extract_concepts_from_text(1, "هذا نص")
    assert result.success is False
    assert "configured" in result.error_message.lower()


def test_process_pdf_in_background_falls_back_to_groq_when_gemini_fails():
    class DummyPdfService:
        def extract_pages(self, pdf_path):
            yield 1, "هذا نص واضح ومفهوم ومناسب لاستخراج المفاهيم التعليمية من الصفحة الأولى"
            return b"image"

    class DummyGeminiService:
        def extract_concepts_from_text(self, page_number, text):
            return ExtractionResponse(success=False, page_number=page_number, concepts=[], error_message="RESOURCE_EXHAUSTED")

        def extract_concepts_from_image(self, page_number, image_bytes):
            return ExtractionResponse(success=False, page_number=page_number, concepts=[], error_message="RESOURCE_EXHAUSTED")

    class DummyGroqService:
        def extract_concepts_from_text(self, page_number, text):
            return ExtractionResponse(success=True, page_number=page_number, concepts=[{"title": "Concept", "content": "ok", "keywords": []}])

        def extract_concepts_from_image(self, page_number, image_bytes):
            return ExtractionResponse(success=True, page_number=page_number, concepts=[{"title": "Concept", "content": "ok", "keywords": []}])

    class DummyOpenRouterService:
        def extract_concepts_from_text(self, page_number, text):
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        def extract_concepts_from_image(self, page_number, image_bytes):
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

    store = DummyStore()
    manager = ExtractionManager(
        DummyPdfService(),
        DummyGeminiService(),
        DummyGroqService(),
        DummyOpenRouterService(),
        store,
        DummyLogger(),
        None,
    )

    manager.process_pdf_in_background("dummy.pdf", "book-1", "job-1")

    assert store.pages[0].success is True
    assert store.pages[0].extraction_service == "groq_text"
