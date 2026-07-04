import os
import requests # سنستخدمها لإرسال النتائج للـ .NET
from services.pdf_slice_service import PdfSliceService
from services.gemini_service import GeminiService
from models.dto import PageExtractionPayload

class ExtractionManager:
    def __init__(self, pdf_service: PdfSliceService, gemini_service: GeminiService, logger, config):
        self.pdf_service = pdf_service
        self.gemini_service = gemini_service
        self.logger = logger
        self.config = config # سنحتاج عنوان سيرفر الـ .NET من هنا

    def process_pdf_in_background(self, pdf_path: str, book_id: str):
        """
        طريقة المعالجة الخلفية: تقطع الملف وتدفع النتائج صفحة بصفحة دون حجز الـ HTTP connection
        """
        self.logger.info(f"Starting async background job for book_id: {book_id}, path: {pdf_path}")
        
        try:
            for page_num, text in self.pdf_service.extract_pages(pdf_path):
                self.logger.info(f"Processing page {page_num} for book {book_id} in background...")
                
                # استدعاء Gemini
                gemini_res = self.gemini_service.extract_concepts_from_text(page_num, text)
                
                # بناء الـ Payload الموجه للـ .NET Backend
                payload = PageExtractionPayload(
                    book_id=book_id,
                    page_number=page_num,
                    success=gemini_res.success,
                    concepts=gemini_res.concepts,
                    error_message=gemini_res.error_message
                )
                
                # إرسال النتيجة فوراً (Webhook approach)
                self.notify_backend(payload)

            self.logger.info(f"Successfully completed background job for book_id: {book_id}")

        except Exception as e:
            self.logger.error(f"Fatal error in background job for book {book_id}: {str(e)}")
            # هنا يمكن إعلام الـ .NET بفشل العملية بالكامل إذا لزم الأمر
        
        finally:
            # تنظيف الملف بعد انتهاء المهمة الخلفية تماماً
            if os.path.exists(pdf_path):
                os.remove(pdf_path)
                self.logger.info(f"Background worker cleaned up temp file: {pdf_path}")

    def notify_backend(self, payload: PageExtractionPayload):
        """
        تقوم بعمل POST request إلى الـ .NET Backend لتسليم البيانات المستخرجة
        """
        # مسار الـ Endpoint في الـ .NET Web API
        url = f"{self.config.NET_BACKEND_URL}/api/v1/webhook/concepts"
        try:
            self.logger.info(f"Pushing page {payload.page_number} results to .NET backend...")
            # إرسال الـ JSON كـ Payload
            response = requests.post(url, json=payload.model_dump(), timeout=30)
            
            if response.status_code == 200 or response.status_code == 204:
                self.logger.info(f"Successfully delivered page {payload.page_number} to .NET.")
            else:
                self.logger.error(f".NET backend responded with status: {response.status_code} for page {payload.page_number}")
        
        except Exception as e:
            self.logger.error(f"Failed to connect to .NET backend to deliver page {payload.page_number}. Error: {str(e)}")