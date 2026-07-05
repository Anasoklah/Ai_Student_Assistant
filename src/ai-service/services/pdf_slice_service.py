import os
import tempfile
import io
import fitz  # PyMuPDF
from typing import Generator, Optional, Tuple


class PdfSliceService:
    def __init__(self, logger):
        self.logger = logger
        self.dpi = 150  # Good enough for LLM vision, smaller files
        self.max_image_bytes = 5 * 1024 * 1024  # 5MB limit

    def count_pages(self, pdf_path: str) -> int:
        """Cheap page count used to populate pages_total before background work starts."""
        doc = fitz.open(pdf_path)
        try:
            return len(doc)
        finally:
            doc.close()

    def slice_pdf(self, pdf_path: str, page_start: int, page_end: int) -> str:
        """
        Creates a new temp PDF containing only pages [page_start, page_end] (1-indexed, inclusive).
        Returns the path to the new sliced PDF. Caller is responsible for deleting it.
        """
        source_doc = fitz.open(pdf_path)
        try:
            total = len(source_doc)
            start = max(0, page_start - 1)  # convert to 0-indexed
            end = min(page_end, total)       # fitz.select uses 0-indexed exclusive end

            if start >= end:
                raise ValueError(f"Invalid page range: start={page_start}, end={page_end}, total={total}")

            sliced_doc = fitz.open()
            try:
                sliced_doc.insert_pdf(source_doc, from_page=start, to_page=end - 1)
                tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".sliced.pdf")
                sliced_doc.save(tmp.name)
                self.logger.info(f"Sliced PDF: pages {page_start}-{page_end} (of {total}) -> {tmp.name} ({sliced_doc.page_count} pages)")
                return tmp.name
            finally:
                sliced_doc.close()
        finally:
            source_doc.close()

    def render_page_as_image(self, pdf_path: str, page_number: int) -> Optional[bytes]:
        """
        Renders a single 1-indexed page to JPEG bytes (smaller than PNG).
        Auto-reduces DPI if image exceeds max_image_bytes.
        """
        doc = fitz.open(pdf_path)
        try:
            if page_number < 1 or page_number > len(doc):
                self.logger.warning(f"page_number {page_number} out of range (1-{len(doc)}).")
                return None
            page = doc.load_page(page_number - 1)

            zoom = self.dpi / 72.0
            mat = fitz.Matrix(zoom, zoom)
            pix = page.get_pixmap(matrix=mat, alpha=False)

            # Convert to JPEG (much smaller than PNG)
            jpeg_bytes = pix.tobytes("jpeg")

            # If too large, reduce DPI and try again
            if len(jpeg_bytes) > self.max_image_bytes:
                self.logger.info(f"Image too large ({len(jpeg_bytes)} bytes), reducing DPI...")
                zoom = 100 / 72.0  # Lower DPI
                mat = fitz.Matrix(zoom, zoom)
                pix = page.get_pixmap(matrix=mat, alpha=False)
                jpeg_bytes = pix.tobytes("jpeg")

            self.logger.info(f"Rendered page {page_number} as JPEG ({len(jpeg_bytes)} bytes, {pix.width}x{pix.height}).")
            return jpeg_bytes
        except Exception as e:
            self.logger.error(f"Failed to render page {page_number} as image. Error: {str(e)}")
            return None
        finally:
            doc.close()

    def extract_pages(self, pdf_path: str) -> Generator[Tuple[int, str], None, None]:
        """
        Streams (page_number, text) tuples for every page in the PDF.
        Pages are 1-indexed.
        """
        doc = fitz.open(pdf_path)
        try:
            total = len(doc)
            self.logger.info(f"Successfully opened PDF file: {pdf_path}. Total pages: {total}")
            for page_num in range(total):
                page = doc.load_page(page_num)
                yield page_num + 1, page.get_text()
        except Exception as e:
            self.logger.error(f"Failed to read PDF file at {pdf_path}. Error: {str(e)}")
            raise
        finally:
            doc.close()
