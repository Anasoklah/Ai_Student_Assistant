import os
from dotenv import load_dotenv

load_dotenv()


def _env_first(*names: str, default: str = "") -> str:
    """
    Return the first environment variable that is set and non-empty.

    The backend/.env file uses ASP.NET's ``Section__Key`` convention
    (e.g. ``Groq__Model``), while this Python service historically read
    ``SCREAMING_SNAKE`` names (e.g. ``GROQ_MODEL``). Reading both keeps a
    single .env authoritative for both services and stops the AI service
    from silently falling back to a wrong/empty model name.
    """
    for name in names:
        value = os.environ.get(name)
        if value is not None and value.strip():
            return value.strip()
    return default


def _env_int(*names: str, default: int) -> int:
    value = _env_first(*names, default=str(default))
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def _env_float(*names: str, default: float) -> float:
    value = _env_first(*names, default=str(default))
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


def _env_bool(*names: str, default: bool) -> bool:
    value = _env_first(*names, default=str(default)).lower()
    return value in ("1", "true", "yes", "on")


class Config:
    def __init__(self):
        # ---- Gemini (primary; schema-constrained decoding) ----
        self.GEMINI_API_KEY = _env_first("GEMINI_API_KEY", "Gemini__ApiKey")
        self.GEMINI_MODEL = _env_first("GEMINI_MODEL", "Gemini__Model", default="gemini-2.5-flash")
        self.GEMINI_MODEL_FALLBACKS = [
            m.strip() for m in _env_first(
                "GEMINI_MODEL_FALLBACKS", "Gemini__ModelFallbacks",
                default="gemini-3.1-flash-lite,gemini-2.5-flash"
            ).split(",") if m.strip()
        ]
        self.GEMINI_TIMEOUT_SECONDS = _env_int("GEMINI_TIMEOUT_SECONDS", default=120)
        self.GEMINI_TEMPERATURE = _env_float("GEMINI_TEMPERATURE", default=0.1)
        self.GEMINI_MAX_OUTPUT_TOKENS = _env_int("GEMINI_MAX_OUTPUT_TOKENS", default=8192)
        # Role gates which extraction path this provider serves: text | vision | both.
        # Gemini is reserved for the vision path to spare its free-tier quota.
        self.GEMINI_ROLE = _env_first("GEMINI_ROLE", "Gemini__Role", default="vision")

        self.MAX_UPLOAD_SIZE_BYTES = _env_int("MAX_UPLOAD_SIZE_BYTES", default=500 * 1024 * 1024)

        # ---- OpenRouter fallback ----
        # Accept both the SCREAMING_SNAKE names and the .NET Section__Key names
        # so a single .env drives both services.
        self.OPENROUTER_API_KEY = _env_first("OPENROUTER_API_KEY", "OpenRouter__ApiKey")
        # The free-model list lives in OpenRouterService.FREE_MODELS. This env
        # override (comma-separated) exists only for churn without a code change;
        # leave it unset to use the in-file list.
        self.OPENROUTER_MODELS = [
            model.strip() for model in _env_first(
                "OPENROUTER_MODELS", "OpenRouter__Models"
            ).split(",") if model.strip()
        ] or None

        self.OPENROUTER_TIMEOUT_SECONDS = _env_int("OPENROUTER_TIMEOUT_SECONDS", default=120)
        self.OPENROUTER_TEMPERATURE = _env_float("OPENROUTER_TEMPERATURE", default=0.1)
        self.OPENROUTER_MAX_OUTPUT_TOKENS = _env_int("OPENROUTER_MAX_OUTPUT_TOKENS", default=8192)
        # OpenRouter forwards structured-output support per underlying model; the
        # qwen/gemini defaults above both support json_schema response_format.
        self.OPENROUTER_STRUCTURED_OUTPUT = _env_bool("OPENROUTER_STRUCTURED_OUTPUT", default=True)
        # Role: text | vision | both. OpenRouter is reserved for the text path.
        self.OPENROUTER_ROLE = _env_first("OPENROUTER_ROLE", "OpenRouter__Role", default="text")

        # ---- Groq fallback (free, no billing required) ----
        self.GROQ_API_KEY = _env_first("GROQ_API_KEY", "Groq__ApiKey")
        self.GROQ_VISION_MODEL = _env_first(
            "GROQ_VISION_MODEL", "Groq__VisionModel",
            default="meta-llama/llama-4-scout-17b-16e-instruct",
        )
        self.GROQ_TIMEOUT_SECONDS = _env_int("GROQ_TIMEOUT_SECONDS", default=120)
        self.GROQ_TEMPERATURE = _env_float("GROQ_TEMPERATURE", default=0.1)
        self.GROQ_MAX_OUTPUT_TOKENS = _env_int("GROQ_MAX_OUTPUT_TOKENS", default=8192)
        # Groq supports OpenAI-style json_object mode on its instruct models.
        # json_schema is only supported on a subset, so default to json_object.
        self.GROQ_STRUCTURED_OUTPUT = _env_bool("GROQ_STRUCTURED_OUTPUT", default=True)
        # Role: text | vision | both. Groq is reserved for the vision path.
        self.GROQ_ROLE = _env_first("GROQ_ROLE", "Groq__Role", default="vision")

        # ---- OpenCode Zen settings ----
        self.OPENCODE_API_KEY = _env_first("OPENCODE_API_KEY", "OpenCode__ApiKey")
        self.OPENCODE_TIMEOUT_SECONDS = _env_int("OPENCODE_TIMEOUT_SECONDS", default=60)
        # Optional overrides for the (churning) stealth-model lists. Empty env =
        # use the code defaults in OpenCodeService. Images only go to the vision list.
        self.OPENCODE_TEXT_MODELS = [
            m.strip() for m in _env_first("OPENCODE_TEXT_MODELS", default="").split(",") if m.strip()
        ]
        self.OPENCODE_VISION_MODELS = [
            m.strip() for m in _env_first("OPENCODE_VISION_MODELS", default="").split(",") if m.strip()
        ]
        # Role: text | vision | both. OpenCode is reserved for the text path.
        self.OPENCODE_ROLE = _env_first("OPENCODE_ROLE", "OpenCode__Role", default="text")

        # ---- Provider fallback tuning ----
        self.PROVIDER_RETRY_COUNT = _env_int("PROVIDER_RETRY_COUNT", default=1)
        self.PROVIDER_PRIORITY = [
            p.strip() for p in _env_first(
                "PROVIDER_PRIORITY", default="opencode,gemini,groq,openrouter"
            ).split(",") if p.strip()
        ]

        # ---- Extraction quality validation ----
        # Keyword bounds (requirements #6). Concepts outside these are flagged,
        # not silently dropped.
        self.KEYWORDS_MIN = _env_int("KEYWORDS_MIN", default=3)
        self.KEYWORDS_MAX = _env_int("KEYWORDS_MAX", default=7)
