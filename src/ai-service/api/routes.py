import os
import uuid
import tempfile

from fastapi import APIRouter, UploadFile, File, Form, Depends, BackgroundTasks, HTTPException
from typing import Optional

from models.dto import JobAcceptedResponse, JobStatusResponse, JobResultResponse, JobStatus
from services.extraction_manager import ExtractionManager

router = APIRouter(prefix="/api/v1/extraction", tags=["Extraction"])


def get_extraction_manager() -> ExtractionManager:
    from app import extraction_manager
    return extraction_manager


@router.post("/extract-pdf-async", response_model=JobAcceptedResponse)
async def extract_pdf_async(
    background_tasks: BackgroundTasks,
    book_id: str = Form(..., description="The unique ID of the book from .NET database"),
    file: UploadFile = File(...),
    page_start: int = Form(1, description="First page to process (1-indexed, inclusive)"),
    page_end: Optional[int] = Form(None, description="Last page to process (1-indexed, inclusive). None = last page."),
    manager: ExtractionManager = Depends(get_extraction_manager),
):
    """
    Accepts the PDF, slices it to the requested page range, creates a job record,
    kicks off background processing on the sliced file, and returns a job_id immediately.
    """
    if file.content_type != "application/pdf" and not file.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Invalid file type. Only PDFs are allowed.")

    job_id = str(uuid.uuid4())
    uploaded_path = None
    sliced_path = None

    try:
        manager.logger.info(f"Accepting extraction request for book_id: {book_id} (job {job_id}), pages {page_start}-{page_end}")

        # 1. Save uploaded file to temp
        max_size = manager.config.MAX_UPLOAD_SIZE_BYTES
        size = 0
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            while chunk := await file.read(1024 * 1024):
                size += len(chunk)
                if size > max_size:
                    tmp.close()
                    os.remove(tmp.name)
                    raise HTTPException(status_code=413, detail="File exceeds maximum allowed size.")
                tmp.write(chunk)
            uploaded_path = tmp.name

        # 2. Validate page range against the original PDF
        pdf_total = manager.pdf_service.count_pages(uploaded_path)
        start = max(1, page_start)
        end = min(page_end or pdf_total, pdf_total)
        if start > end:
            raise HTTPException(status_code=400, detail=f"page_start ({start}) is greater than page_end ({end}).")
        pages_total = end - start + 1

        # 3. Slice the PDF to only the requested pages
        sliced_path = manager.pdf_service.slice_pdf(uploaded_path, start, end)

        # 4. Delete the original uploaded file (no longer needed)
        os.remove(uploaded_path)
        uploaded_path = None

        # 5. Create job and kick off background processing on the sliced file
        manager.job_store.create(job_id=job_id, book_id=book_id, pages_total=pages_total,
                                 page_start=start, page_end=end)

        background_tasks.add_task(
            manager.process_pdf_in_background,
            pdf_path=sliced_path,
            book_id=book_id,
            job_id=job_id,
        )

        return JobAcceptedResponse(job_id=job_id, book_id=book_id, page_start=start, page_end=end)

    except HTTPException:
        if uploaded_path and os.path.exists(uploaded_path):
            os.remove(uploaded_path)
        if sliced_path and os.path.exists(sliced_path):
            os.remove(sliced_path)
        raise
    except Exception as e:
        manager.logger.error(f"Failed to queue job for book {book_id}. Error: {str(e)}")
        if uploaded_path and os.path.exists(uploaded_path):
            os.remove(uploaded_path)
        if sliced_path and os.path.exists(sliced_path):
            os.remove(sliced_path)
        raise HTTPException(status_code=500, detail="Could not initialize background processing pipeline.")


@router.get("/jobs/{job_id}", response_model=JobStatusResponse)
async def get_job_status(job_id: str, manager: ExtractionManager = Depends(get_extraction_manager)):
    """Cheap, poll-friendly status check. .NET should call this on an interval (e.g. every 5-10s)."""
    job = manager.job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job not found.")

    return JobStatusResponse(
        job_id=job.job_id,
        book_id=job.book_id,
        status=job.status.value,
        message=job.status_message,
        pages_done=job.pages_done,
        pages_total=job.pages_total,
        page_start=job.page_start,
        page_end=job.page_end,
    )


@router.get("/jobs/{job_id}/result", response_model=JobResultResponse)
async def get_job_result(job_id: str, manager: ExtractionManager = Depends(get_extraction_manager)):
    """Full result payload. Only call this once, after status == Ready."""
    job = manager.job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job not found.")

    if job.status != JobStatus.READY:
        raise HTTPException(
            status_code=409,
            detail=f"Job is not ready yet. Current status: {job.status.value}",
        )

    return JobResultResponse(job_id=job.job_id, book_id=job.book_id, pages=job.pages)
