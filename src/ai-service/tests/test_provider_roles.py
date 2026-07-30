"""
Tests for the text/vision provider role split.

The default roles reserve Gemini + Groq for the vision path and OpenRouter +
OpenCode for the text path, so each provider only spends its free-tier quota on
the path it's assigned to.
"""

from services.extraction_manager import ExtractionManager
from services.provider_roles import provider_allows, resolve_role


class DummyLogger:
    def info(self, message):
        pass

    def warning(self, message):
        pass

    def error(self, message):
        pass


class DummyStore:
    def add_page_result(self, job_id, page_result):
        pass

    def mark_ready(self, *a, **k):
        pass

    def mark_failed(self, *a, **k):
        pass


class RoleConfig:
    """Config with the intended default role split."""
    PROVIDER_PRIORITY = ["opencode", "gemini", "groq", "openrouter"]
    GEMINI_ROLE = "vision"
    GROQ_ROLE = "vision"
    OPENROUTER_ROLE = "text"
    OPENCODE_ROLE = "text"


class _Svc:
    """Stand-in provider exposing the two extraction entry points."""
    def extract_concepts_from_text(self, page_number, text):
        pass

    def extract_concepts_from_image(self, page_number, image_bytes):
        pass


class _OpenCodeSvc(_Svc):
    enabled = True


def _manager(config):
    return ExtractionManager(
        None, _Svc(), _Svc(), _Svc(), DummyStore(), DummyLogger(), config,
        opencode_service=_OpenCodeSvc(),
    )


def test_resolve_role_defaults_to_both_without_config():
    assert resolve_role(None, "gemini") == "both"


def test_resolve_role_unknown_value_falls_back_to_both():
    class C:
        GEMINI_ROLE = "nonsense"
    assert resolve_role(C(), "gemini") == "both"


def test_provider_allows_respects_role():
    cfg = RoleConfig()
    assert provider_allows(cfg, "gemini", is_vision=True) is True
    assert provider_allows(cfg, "gemini", is_vision=False) is False
    assert provider_allows(cfg, "openrouter", is_vision=False) is True
    assert provider_allows(cfg, "openrouter", is_vision=True) is False


def test_text_path_uses_only_text_role_providers():
    manager = _manager(RoleConfig())
    names = [name for name, _ in manager._get_provider_order(is_vision=False)]
    assert names == ["opencode_text", "openrouter_text"]


def test_vision_path_uses_only_vision_role_providers():
    manager = _manager(RoleConfig())
    names = [name for name, _ in manager._get_provider_order(is_vision=True)]
    assert names == ["gemini_vision", "groq_vision"]


def test_missing_config_keeps_all_providers_on_both_paths():
    # config=None -> every provider resolves to "both" (legacy behavior preserved).
    manager = _manager(None)
    text_names = [n for n, _ in manager._get_provider_order(is_vision=False)]
    vision_names = [n for n, _ in manager._get_provider_order(is_vision=True)]
    assert "gemini_text" in text_names and "openrouter_text" in text_names
    assert "gemini_vision" in vision_names and "openrouter_vision" in vision_names
