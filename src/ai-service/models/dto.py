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


    # Add this new class alongside your existing DTOs

class TocEntry(BaseModel):
    """
    Represents one entry from the Table of Contents.
    
    Example:
        TocEntry(
            title="الاعداد العادية",
            page_number=20,
            level="Section",
            parent_chapter="الوحدة الأولى: الأعداد والعمليات"
        )
    """
    title: str                              # The title of the chapter/section
    page_number: Optional[int] = None       # Starting page number (null for some chapter headers)
    level: str = "Section"                  # "Chapter" or "Section"
    parent_chapter: Optional[str] = None    # Which chapter this section belongs to


class DocumentStructure(BaseModel):
    """
    The complete structure of a document extracted from its Table of Contents.
    This is what gets sent to .NET after parsing.
    """
    chapters: List[TocEntry] = Field(default_factory=list)
    sections: List[TocEntry] = Field(default_factory=list)
    total_entries: int = 0
    extraction_method: str = "unknown"  # "toc_parser", "ai_fallback", "manual"


class JobRecord(BaseModel):
    job_id: str
    book_id: str
    status: JobStatus = JobStatus.PROCESSING
    status_message: str = "Processing started."
    pages_done: int = 0
    pages_total: int = 0
    pages: List[PageResult] = Field(default_factory=list)

class StructureExtractionResponse(BaseModel):
    """
    Returned from POST /extract-book-structure.
    Used once when importing a new book.
    """
    success: bool
    structure: Optional[DocumentStructure] = None
    error_message: Optional[str] = None