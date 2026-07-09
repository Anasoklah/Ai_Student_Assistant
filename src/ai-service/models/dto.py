from pydantic import BaseModel, Field
from typing import List, Optional
from enum import Enum


class ExtractedConcept(BaseModel):
    title: str
    content: str
    keywords: List[str] = Field(default_factory=list)


class ExtractionResponse(BaseModel):
    success: bool
    page_number: int
    concepts: List[ExtractedConcept] = Field(default_factory=list)
    error_message: Optional[str] = None


class JobStatus(str, Enum):
    PROCESSING = "Processing"
    READY = "Ready"
    FAILED = "Failed"


class PageResult(BaseModel):
    page_number: int
    success: bool
    concepts: List[ExtractedConcept] = Field(default_factory=list)
    error_message: Optional[str] = None
    extraction_service: Optional[str] = None
    text_quality_score: Optional[float] = None


class JobRecord(BaseModel):
    """
    The full internal state of one extraction job.
    Lives only in JobStore (in-memory). Never returned directly to callers —
    the API surfaces trimmed views of this (JobStatusResponse / JobResultResponse).
    """
    job_id: str
    book_id: str
    status: JobStatus = JobStatus.PROCESSING
    status_message: str = "Processing started."
    pages_done: int = 0
    pages_total: int = 0
    pages: List[PageResult] = Field(default_factory=list)


# ---- API response shapes ----

class JobAcceptedResponse(BaseModel):
    """Returned immediately from POST /extract-pdf-async."""
    job_id: str
    book_id: str
    status: str = JobStatus.PROCESSING.value
    message: str = "PDF accepted. Processing started in the background."


class JobStatusResponse(BaseModel):
    """Returned from GET /jobs/{job_id} — cheap, poll-friendly."""
    job_id: str
    book_id: str
    status: str
    message: str
    pages_done: int = 0
    pages_total: int = 0


class JobResultResponse(BaseModel):
    """Returned from GET /jobs/{job_id}/result — only valid once status == Ready."""
    job_id: str
    book_id: str
    pages: List[PageResult]


class ImageExtractionResponse(BaseModel):
    """Returned synchronously from POST /extract-image."""
    success: bool
    page_number: int = 1
    concepts: List[ExtractedConcept] = Field(default_factory=list)
    error_message: Optional[str] = None
    extraction_service: Optional[str] = None
