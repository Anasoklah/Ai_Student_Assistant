import os
import tempfile

import fitz

from services.pdf_slice_service import PdfSliceService


class DummyLogger:
    def info(self, message):
        pass

    def warning(self, message):
        pass

    def error(self, message):
        pass


def test_slice_pdf_creates_subset_pdf_with_requested_pages():
    service = PdfSliceService(DummyLogger())

    with tempfile.TemporaryDirectory() as tmpdir:
        source_path = os.path.join(tmpdir, "source.pdf")
        output_path = os.path.join(tmpdir, "subset.pdf")

        doc = fitz.open()
        try:
            doc.new_page()
            doc[0].insert_text((72, 72), "Page 1")
            doc.new_page()
            doc[1].insert_text((72, 72), "Page 2")
            doc.new_page()
            doc[2].insert_text((72, 72), "Page 3")
            doc.save(source_path)
        finally:
            doc.close()

        sliced_path = service.slice_pdf(source_path, 2, 3)

        assert os.path.exists(sliced_path)
        with fitz.open(sliced_path) as sliced_doc:
            assert sliced_doc.page_count == 2
            assert "Page 2" in sliced_doc.load_page(0).get_text()
            assert "Page 3" in sliced_doc.load_page(1).get_text()

        os.remove(sliced_path)
