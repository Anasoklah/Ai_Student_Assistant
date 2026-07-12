import os
import tempfile
import fitz  # PyMuPDF
from typing import Generator, Optional, Tuple


class PdfSliceService:
    def __init__(self, logger):
        self.logger = logger
        self.dpi = 150
        self.max_image_bytes = 5 * 1024 * 1024

    def count_pages(self, pdf_path: str) -> int:
        """Cheap page count used to populate pages_total before background work starts."""
        doc = fitz.open(pdf_path)
        try:
            return len(doc)
        finally:
            doc.close()

    def extract_pages(self, pdf_path: str) -> Generator[Tuple[int, str], None, None]:
        """
        Streams (page_number, text) tuples one page at a time.
        """
        doc = fitz.open(pdf_path)
        try:
            self.logger.info(f"Successfully opened PDF file: {pdf_path}. Total pages: {len(doc)}")
            for page_num in range(len(doc)):
                page = doc.load_page(page_num)
                yield page_num + 1, page.get_text()
        except Exception as e:
            self.logger.error(f"Failed to read PDF file at {pdf_path}. Error: {str(e)}")
            raise
        finally:
            doc.close()

    def slice_pdf(self, pdf_path: str, page_start: int, page_end: int) -> str:
        """Create a temporary PDF containing only the requested page range."""
        source_doc = fitz.open(pdf_path)
        try:
            total = len(source_doc)
            start = max(0, page_start - 1)
            end = min(page_end, total)

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
        """Render a single page to JPEG bytes for vision-based fallback."""
        doc = fitz.open(pdf_path)
        try:
            if page_number < 1 or page_number > len(doc):
                self.logger.warning(f"page_number {page_number} out of range (1-{len(doc)}).")
                return None

            page = doc.load_page(page_number - 1)
            zoom = self.dpi / 72.0
            mat = fitz.Matrix(zoom, zoom)
            pix = page.get_pixmap(matrix=mat, alpha=False)
            jpeg_bytes = pix.tobytes("jpeg")

            if len(jpeg_bytes) > self.max_image_bytes:
                self.logger.info(f"Image too large ({len(jpeg_bytes)} bytes), reducing DPI...")
                zoom = 100 / 72.0
                mat = fitz.Matrix(zoom, zoom)
                pix = page.get_pixmap(matrix=mat, alpha=False)
                jpeg_bytes = pix.tobytes("jpeg")

            return jpeg_bytes
        except Exception as e:
            self.logger.error(f"Failed to render page {page_number} as image. Error: {str(e)}")
            return None
        finally:
            doc.close()

    def slice_pdf_with_toc(self, pdf_path: str, toc_page: int, page_start: int, page_end: int) -> Tuple[str, dict]:
        """
        Create a temporary PDF where page 1 is the TOC page, followed by the requested page range.
        
        Returns:
            Tuple of (temp_file_path, page_map)
            page_map: dict mapping sliced_page_number -> original_page_number
                     Example: {1: 7, 2: 26, 3: 27, 4: 28, 5: 29, 6: 30}
        """
        source_doc = fitz.open(pdf_path)
        try:
            total = len(source_doc)
            
            # Validate inputs
            if toc_page < 1 or toc_page > total:
                raise ValueError(f"TOC page {toc_page} out of range (1-{total})")
            
            start = max(1, page_start)
            end = min(page_end, total)
            
            if start > end:
                raise ValueError(f"Invalid page range: start={page_start}, end={page_end}, total={total}")
            
            # Build the page mapping
            page_map = {}
            next_sliced_page = 1
            
            # TOC page becomes page 1 in sliced PDF
            page_map[next_sliced_page] = toc_page
            next_sliced_page += 1
            
            # User pages become pages 2+ in sliced PDF
            for orig_page in range(start, end + 1):
                page_map[next_sliced_page] = orig_page
                next_sliced_page += 1
            
            # Create new document
            sliced_doc = fitz.open()
            try:
                # Step 1: Insert TOC page first
                sliced_doc.insert_pdf(source_doc, from_page=toc_page - 1, to_page=toc_page - 1)
                self.logger.info(f"Added TOC page (original {toc_page}) as sliced page 1")
                
                # Step 2: Insert user's page range
                sliced_doc.insert_pdf(source_doc, from_page=start - 1, to_page=end - 1)
                self.logger.info(f"Added user pages (original {start}-{end}) as sliced pages 2-{1 + (end - start + 1)}")
                
                # Save to temp file
                tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".sliced.pdf")
                sliced_doc.save(tmp.name)
                
                self.logger.info(
                    f"Sliced PDF with TOC: {sliced_doc.page_count} pages. Page mapping: {page_map}"
                )
                return tmp.name, page_map
            finally:
                sliced_doc.close()
        finally:
            source_doc.close()


    def extract_pages_with_original_numbers(self, pdf_path: str, page_map: dict) -> Generator[Tuple[int, str], None, None]:
        """
        Streams pages with their ORIGINAL page numbers.
        
        Args:
            pdf_path: Path to the sliced PDF
            page_map: Dictionary mapping sliced_page_number -> original_page_number
                     Example: {1: 7, 2: 26, 3: 27, 4: 28, 5: 29, 6: 30}
        """
        doc = fitz.open(pdf_path)
        try:
            self.logger.info(f"Successfully opened sliced PDF: {pdf_path}. Total pages: {len(doc)}")
            for sliced_page_num in range(len(doc)):
                page = doc.load_page(sliced_page_num)
                original_page_num = page_map.get(sliced_page_num + 1, sliced_page_num + 1)
                yield original_page_num, page.get_text()
        except Exception as e:
            self.logger.error(f"Failed to read sliced PDF at {pdf_path}. Error: {str(e)}")
            raise
        finally:
            doc.close()           