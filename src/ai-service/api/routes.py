import os
import shutil
import tempfile
from typing import Optional
from fastapi import APIRouter, UploadFile, File, Form, Depends, BackgroundTasks, HTTPException
from fastapi.responses import FileResponse
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


@router.post("/slice-pdf")
async def slice_pdf(
    file: UploadFile = File(...),
    page_start: int = Form(1, description="First page to include (1-indexed, inclusive)"),
    page_end: Optional[int] = Form(None, description="Last page to include (1-indexed, inclusive). None = last page."),
    manager: ExtractionManager = Depends(get_extraction_manager),
):
    """
    Slices the uploaded PDF to the requested page range and returns the sliced PDF directly.
    No text extraction or rendering is performed.
    """
    if not file.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Invalid file type. Only PDFs are allowed.")

    uploaded_path = None
    sliced_path = None

    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            shutil.copyfileobj(file.file, tmp)
            uploaded_path = tmp.name

        pdf_total = manager.pdf_service.count_pages(uploaded_path)
        start = max(1, page_start)
        end = min(page_end or pdf_total, pdf_total)
        if start > end:
            raise HTTPException(status_code=400, detail=f"page_start ({start}) is greater than page_end ({end}).")

        sliced_path = manager.pdf_service.slice_pdf(uploaded_path, start, end)
        os.remove(uploaded_path)
        uploaded_path = None

        def cleanup():
            if sliced_path and os.path.exists(sliced_path):
                os.remove(sliced_path)

        return FileResponse(
            sliced_path,
            media_type="application/pdf",
            filename=f"sliced_{start}-{end}.pdf",
            background=cleanup,
        )

    except HTTPException:
        if uploaded_path and os.path.exists(uploaded_path):
            os.remove(uploaded_path)
        if sliced_path and os.path.exists(sliced_path):
            os.remove(sliced_path)
        raise
    except Exception as e:
        manager.logger.error(f"Failed to slice PDF. Error: {str(e)}")
        if uploaded_path and os.path.exists(uploaded_path):
            os.remove(uploaded_path)
        if sliced_path and os.path.exists(sliced_path):
            os.remove(sliced_path)
        raise HTTPException(status_code=500, detail="Failed to slice PDF.")
