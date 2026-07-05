import logging
import sys
from fastapi import FastAPI
from api.routes import router as extraction_router
from Config import Config
from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from services.groq_service import GroqService
from services.openrouter_service import OpenRouterService
from Jobs.JobStore import JobStore
from services.extraction_manager import ExtractionManager

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    handlers=[logging.StreamHandler()],
)
logger = logging.getLogger("AIServiceApp")

config = Config()

# Fail fast: without this key every extraction request would fail individually
# and silently at request time. Better to refuse to start at all.
if not config.GEMINI_API_KEY:
    logger.error("GEMINI_API_KEY is not set. The service cannot start without it.")
    sys.exit(1)

logger.info("Initializing system services and dependencies...")
pdf_service = PdfSliceService(logger)
gemini_service = GeminiService(config, logger)
groq_service = GroqService(config, logger)
openrouter_service = OpenRouterService(config, logger)
job_store = JobStore()

extraction_manager = ExtractionManager(
    pdf_service,
    gemini_service,
    groq_service,
    openrouter_service,
    job_store,
    logger,
    config,
)

app = FastAPI(
    title="Syrian Study Assistant - AI Service",
    description="Python API for PDF Chunking and Gemini Extraction (pull-based job model)",
    version="2.0.0",
)

app.include_router(extraction_router)


@app.get("/health", tags=["Infrastructure"])
async def health_check():
    return {"status": "Healthy", "service": "ai-service"}


if __name__ == "__main__":
    import uvicorn
    logger.info("Starting Uvicorn server on port 8000...")
    uvicorn.run("app:app", host="0.0.0.0", port=8000, reload=True)