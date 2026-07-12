import os
import uuid
import tempfile

from fastapi import APIRouter, UploadFile, File, Form, Depends, BackgroundTasks, HTTPException

from models.dto import JobAcceptedResponse, JobStatusResponse, JobResultResponse, JobStatus, ImageExtractionResponse , StructureExtractionResponse
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
    page_start: int = Form(1, ge=1, description="First page to process (1-indexed, inclusive)"),
    page_end: int | None = Form(None, ge=1, description="Last page to process (1-indexed, inclusive)"),
    manager: ExtractionManager = Depends(get_extraction_manager),
):
    """
    Accepts the PDF, creates a job record, kicks off background processing,
    and returns a job_id immediately. .NET polls GET /jobs/{job_id} afterward
    — this endpoint no longer notifies .NET of anything itself.
    """
    if file.content_type != "application/pdf" and not file.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Invalid file type. Only PDFs are allowed.")

    job_id = str(uuid.uuid4())
    temp_file_path = None
    sliced_file_path = None

    try:
        manager.logger.info(f"Accepting extraction request for book_id: {book_id} (job {job_id})")

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
            temp_file_path = tmp.name
        pdf_total = manager.pdf_service.count_pages(temp_file_path)
        start = max(1, page_start)
        end = min(page_end or pdf_total, pdf_total)
        if start > end:
            raise HTTPException(status_code=400, detail=f"page_start ({start}) is greater than page_end ({end}).")

        sliced_file_path = manager.pdf_service.slice_pdf(
            temp_file_path,
            start,
            end,
)

        pages_total = end - start + 1

        if os.path.exists(temp_file_path):
            os.remove(temp_file_path)
            temp_file_path = None

        manager.job_store.create(job_id=job_id, book_id=book_id, pages_total=pages_total)

        background_tasks.add_task(
        manager.process_pdf_in_background,
        pdf_path=sliced_file_path,
        book_id=book_id,
        job_id=job_id,
        )   

        return JobAcceptedResponse(job_id=job_id, book_id=book_id)

    except HTTPException:
        if temp_file_path and os.path.exists(temp_file_path):
            os.remove(temp_file_path)
        if sliced_file_path and os.path.exists(sliced_file_path):
            os.remove(sliced_file_path)
        raise
    except Exception as e:
        manager.logger.error(f"Failed to queue job for book {book_id}. Error: {str(e)}")
        if temp_file_path and os.path.exists(temp_file_path):
            os.remove(temp_file_path)
        if sliced_file_path and os.path.exists(sliced_file_path):
            os.remove(sliced_file_path)
        raise HTTPException(status_code=500, detail="Could not initialize background processing pipeline.")


@router.post("/extract-image", response_model=ImageExtractionResponse)
async def extract_image(
    file: UploadFile = File(...),
    manager: ExtractionManager = Depends(get_extraction_manager),
):
    """
    Accepts a single image (PNG, JPG, JPEG, WebP), processes it through the vision
    model, and returns extracted concepts synchronously. No job polling required.
    """
    ALLOWED_IMAGE_TYPES = {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
    }

    if file.content_type not in ALLOWED_IMAGE_TYPES:
        ext = os.path.splitext(file.filename.lower())[1] if file.filename else ""
        if ext not in (".png", ".jpg", ".jpeg", ".webp"):
            raise HTTPException(
                status_code=400,
                detail=f"Unsupported image type: {file.content_type or ext}. "
                       f"Allowed: PNG, JPG, JPEG, WebP.",
            )

    max_size = manager.config.MAX_UPLOAD_SIZE_BYTES
    image_bytes = b""

    try:
        while chunk := await file.read(1024 * 512):
            image_bytes += chunk
            if len(image_bytes) > max_size:
                raise HTTPException(status_code=413, detail="Image exceeds maximum allowed size.")

        manager.logger.info(f"Image received: {file.filename}, {len(image_bytes)} bytes, type: {file.content_type}")

        result, provider_or_error = manager.extract_single_image(image_bytes)

        if result is None or not result.success:
            manager.logger.error(f"Image extraction failed: {provider_or_error}")
            return ImageExtractionResponse(
                success=False,
                error_message=provider_or_error,
                extraction_service=None,
            )

        return ImageExtractionResponse(
            success=True,
            concepts=result.concepts,
            extraction_service=provider_or_error,
        )

    except HTTPException:
        raise
    except Exception as e:
        manager.logger.error(f"Image extraction failed: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Image processing failed: {str(e)}")


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

    return JobResultResponse(
        job_id=job.job_id,
        book_id=job.book_id,
        pages=job.pages,
    )


@router.post(
    "/extract-book-structure",
    response_model=StructureExtractionResponse,
)
async def extract_book_structure(
    file: UploadFile = File(...),
    toc_page: int = Form(..., ge=1),
    manager: ExtractionManager = Depends(get_extraction_manager),
):
    """
    Extract the document structure (Table of Contents) from a book.
    This endpoint should be called only once when a new book is imported.
    """

    if file.content_type != "application/pdf" and not file.filename.lower().endswith(".pdf"):
        raise HTTPException(
            status_code=400,
            detail="Invalid file type. Only PDFs are allowed."
        )

    temp_file_path = None

    try:
        max_size = manager.config.MAX_UPLOAD_SIZE_BYTES
        size = 0

        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            while chunk := await file.read(1024 * 1024):
                size += len(chunk)

                if size > max_size:
                    tmp.close()
                    os.remove(tmp.name)
                    raise HTTPException(
                        status_code=413,
                        detail="File exceeds maximum allowed size."
                    )

                tmp.write(chunk)

            temp_file_path = tmp.name

        total_pages = manager.pdf_service.count_pages(temp_file_path)

        if toc_page > total_pages:
            raise HTTPException(
                status_code=400,
                detail=f"toc_page ({toc_page}) is out of range. PDF contains {total_pages} pages."
            )

        structure = manager.extract_book_structure(
            pdf_path=temp_file_path,
            toc_page=toc_page,
        )

        if structure is None:
            return StructureExtractionResponse(
                success=False,
                error_message="Failed to extract document structure."
            )

        return StructureExtractionResponse(
            success=True,
            structure=structure,
        )

    finally:
        if temp_file_path and os.path.exists(temp_file_path):
            os.remove(temp_file_path)