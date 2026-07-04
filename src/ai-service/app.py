import logging
from fastapi import FastAPI
from api.routes import router as extraction_router
from Config import Config  # افترضنا وجود كائن الإعدادات هنا
from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from services.extraction_manager import ExtractionManager

# 1. إعداد الـ Logger العام للنظام باللغة الإنجليزية
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    handlers=[
        logging.StreamHandler() # لطباعة السجلات مباشرة في الـ Terminal / Docker Console
    ]
)
logger = logging.getLogger("AIServiceApp")

# 2. تحميل الإعدادات (تأكد من قراءة قيم الـ ENV بمشروعك)
config = Config()

# 3. بناء شجرة الاعتماديات (Composition Root) تماماً كخطوة Program.cs في .NET
logger.info("Initializing system services and dependencies...")
pdf_service = PdfSliceService(logger)
gemini_service = GeminiService(config, logger)

# جعل الـ Manager متاحاً للـ Routes (تم استيراده هناك عبر دالة get_extraction_manager)
extraction_manager = ExtractionManager(pdf_service, gemini_service, logger)

# 4. بناء تطبيق FastAPI وتضمين الـ Routers
app = FastAPI(
    title="Syrian Study Assistant - AI Service",
    description="Python API for PDF Chunking and Gemini Extraction",
    version="1.0.0"
)

# تسجيل الـ Routes
app.include_router(extraction_router)

@app.get("/health", tags=["Infrastructure"])
async def health_check():
    """
    Health check endpoint for Docker/Kubernetes probes.
    """
    return {"status": "Healthy", "service": "ai-service"}

# لتشغيل السيرفر محلياً أثناء التطوير عند استدعاء الملف مباشرة
if __name__ == "__main__":
    import uvicorn
    logger.info("Starting Uvicorn server on port 8000...")
    uvicorn.run("app:app", host="0.0.0.0", port=8000, reload=True)