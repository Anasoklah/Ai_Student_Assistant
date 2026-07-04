import fitz  # PyMuPDF
from typing import Generator, Tuple

class PdfSliceService:
    def __init__(self, logger):
        self.logger = logger

    def extract_pages(self, pdf_path: str) -> Generator[Tuple[int, str], None, None]:
        """
        تقرأ ملف الـ PDF وتُرجع نص كل صفحة على حدة (Streaming)
        """
        try:
            doc = fitz.open(pdf_path)
            # سجل باللغة الإنجليزية لمراقبة عدد الصفحات
            self.logger.info(f"Successfully opened PDF file: {pdf_path}. Total pages: {len(doc)}")
            
            for page_num in range(len(doc)):
                page = doc.load_page(page_num)
                text = page.get_text()
                yield page_num + 1, text
                
        except Exception as e:
            # سجل الأخطاء بالإنجليزية لتسهيل الـ Debugging
            self.logger.error(f"Failed to read PDF file at {pdf_path}. Error: {str(e)}")
            raise e