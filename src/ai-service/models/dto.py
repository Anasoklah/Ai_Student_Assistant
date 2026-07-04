from pydantic import BaseModel, Field
from typing import List, Optional

# المخرجات الحالية للمفاهيم
class ExtractedConcept(BaseModel):
    title: str
    content: str
    keywords: List[str] = []

# الرد السريع الفوري الذي سيعود للـ .NET
class JobStatusResponse(BaseModel):
    book_id: str
    status: str = "Processing"
    message: str

# البيانات التي سيتم دفعها (Push) إلى الـ .NET بعد انتهاء كل صفحة
class PageExtractionPayload(BaseModel):
    book_id: str
    page_number: int
    success: bool
    concepts: List[ExtractedConcept]
    error_message: Optional[str] = None