from services.openrouter_service import OpenRouterService


class DummyLogger:
    def info(self, message):
        pass

    def warning(self, message):
        pass

    def error(self, message):
        pass


class DummyConfig:
    OPENROUTER_API_KEY = "test-key"
    OPENROUTER_MODEL = "first-model:free"
    OPENROUTER_MODEL_FALLBACKS = ["second-model:free"]
    OPENROUTER_VISION_MODEL = "vision-model:free"
    OPENROUTER_VISION_MODEL_FALLBACKS = []
    OPENROUTER_TIMEOUT_SECONDS = 30
    OPENROUTER_STRUCTURED_OUTPUT = False


class DummyResponse:
    def __init__(self, status_code, content=""):
        self.status_code = status_code
        self.text = content
        self._content = content

    def json(self):
        return {"choices": [{"message": {"content": self._content}}]}


def test_text_extraction_uses_next_model_after_rate_limit(monkeypatch):
    requested_models = []
    responses = [
        DummyResponse(429, "rate limited"),
        DummyResponse(200, '{"success": true, "page_number": 1, "concepts": []}'),
    ]

    def fake_post(*args, **kwargs):
        requested_models.append(kwargs["json"]["model"])
        return responses.pop(0)

    monkeypatch.setattr("services.openrouter_service.httpx.post", fake_post)
    service = OpenRouterService(DummyConfig(), DummyLogger())

    result = service.extract_concepts_from_text(1, "Study material")

    assert result.success is True
    assert requested_models == ["first-model:free", "second-model:free"]


def test_text_extraction_reports_when_all_models_are_rate_limited(monkeypatch):
    def fake_post(*args, **kwargs):
        return DummyResponse(429, "rate limited")

    monkeypatch.setattr("services.openrouter_service.httpx.post", fake_post)
    service = OpenRouterService(DummyConfig(), DummyLogger())

    result = service.extract_concepts_from_text(1, "Study material")

    assert result.success is False
    assert result.error_message == "All configured OpenRouter models are rate limited."
