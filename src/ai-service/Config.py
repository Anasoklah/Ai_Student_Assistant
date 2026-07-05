import os
from dotenv import load_dotenv

load_dotenv()


class Config:
    def __init__(self):
        self.GEMINI_API_KEY = os.environ.get("GEMINI_API_KEY", "")
        self.GEMINI_MODEL = os.environ.get("GEMINI_MODEL", "gemini-2.0-flash")
        self.GEMINI_TIMEOUT_SECONDS = int(os.environ.get("GEMINI_TIMEOUT_SECONDS", "120"))
        self.MAX_UPLOAD_SIZE_BYTES = int(os.environ.get("MAX_UPLOAD_SIZE_BYTES", str(100 * 1024 * 1024)))
        self.BOILERPLATE_REPEAT_RATIO = float(os.environ.get("BOILERPLATE_REPEAT_RATIO", "0.6"))
        self.NET_BACKEND_URL = os.environ.get("NET_BACKEND_URL", "http://localhost:5000")

        # OpenRouter fallback settings
        self.OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY", "")
        self.OPENROUTER_MODEL = os.environ.get("OPENROUTER_MODEL", "nvidia/nemotron-nano-12b-v2-vl:free")
        self.OPENROUTER_TIMEOUT_SECONDS = int(os.environ.get("OPENROUTER_TIMEOUT_SECONDS", "120"))

        # Groq fallback settings (free, no billing required)
        self.GROQ_API_KEY = os.environ.get("GROQ_API_KEY", "")
        self.GROQ_MODEL = os.environ.get("GROQ_MODEL", "meta-llama/llama-4-scout-17b-16e-instruct")
        self.GROQ_TIMEOUT_SECONDS = int(os.environ.get("GROQ_TIMEOUT_SECONDS", "120"))

        # Provider fallback tuning
        self.PROVIDER_RETRY_COUNT = int(os.environ.get("PROVIDER_RETRY_COUNT", "1"))
        self.PROVIDER_PRIORITY = os.environ.get(
            "PROVIDER_PRIORITY",
            "gemini,groq,openrouter",
        ).split(",")
