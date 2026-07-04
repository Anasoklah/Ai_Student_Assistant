import os

GEMINI_API_KEY = os.environ["GEMINI_API_KEY"]

GEMINI_MODEL = os.environ.get(
    "GEMINI_MODEL",
    "gemini-2.5-flash"
)

GEMINI_TIMEOUT_SECONDS = int(
    os.environ.get(
        "GEMINI_TIMEOUT_SECONDS",
        "120"
    )
)

MAX_UPLOAD_SIZE_BYTES = int(
    os.environ.get(
        "MAX_UPLOAD_SIZE_BYTES",
        str(100 * 1024 * 1024)
    )
)

BOILERPLATE_REPEAT_RATIO = float(
    os.environ.get(
        "BOILERPLATE_REPEAT_RATIO",
        "0.6"
    )
)