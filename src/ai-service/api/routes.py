import shutil
import tempfile
from fastapi import APIRouter, UploadFile, File, Form, Depends, BackgroundTasks, HTTPException
from models.dto import JobStatusResponse
from services.extraction_manager import ExtractionManager

router = APIRouter(prefix="/api/v1/extraction", tags=["Extraction"])

def get_extraction_manager() -> ExtractionManager:
    from app import extraction_manager
    return extraction_manager

@router.post("/extract-pdf-async", response_model=JobStatusResponse)
async def extract_pdf_async(
    background_tasks: BackgroundTasks,
    book_id: str = Form(..., description="The unique ID of the book from .NET database"),
    file: UploadFile = File(...), 
    manager: ExtractionManager = Depends(get_extraction_manager)
):
    """
    Accepts the PDF file, saves it securely, triggers a background thread, and responds immediately.
    """
    if not file.filename.lower().endswith('.pdf'):
        raise HTTPException(status_code=400, detail="Invalid file type. Only PDFs are allowed.")

    try:
        manager.logger.info(f"Initiating async extraction request for book_id: {book_id}")
        
        # حفظ الملف في مسار مؤقت آمن
        # لا نقوم بحذفه هنا في الـ finally لأن الـ Background Task ستحتاجه لقراءته في خيط آخر
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            shutil.copyfileobj(file.file, tmp)
            temp_file_path = tmp.name

        # تسجيل المهمة في الـ BackgroundTasks الخاصة بـ FastAPI
        # سيقوم الـ API بتمرير السيطرة للـ Background worker وإغلاق اتصال الـ HTTP مع الـ .NET فوراً
        background_tasks.add_task(
            manager.process_pdf_in_background, 
            pdf_path=temp_file_path, 
            book_id=book_id
        )

        # الرد الفوري السريع
        return JobStatusResponse(
            book_id=book_id,
            message="PDF accepted successfully. Processing started in the background."
        )

    except Exception as e:
        manager.logger.error(f"Failed to queue background job for book {book_id}. Error: {str(e)}")
        raise HTTPException(status_code=500, detail="Could not initialize background processing pipeline.")