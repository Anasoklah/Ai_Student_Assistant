
import json
import google.generativeai as genai
from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder

class GeminiService:
    def __init__(self, config, logger):
        self.logger = logger
        genai.configure(api_key=config.GEMINI_API_KEY)
        # استخدام نموذج سريع ودقيق للاستخراج الهيكلي
        self.model = genai.GenerativeModel("gemini-1.5-flash")

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)

        try:
            response = self.model.generate_content(
                prompt,
                generation_config={
                    "response_mime_type": "application/json",
                    "response_schema": ExtractionResponse
                }
            )
            
            data = json.loads(response.text)
            return ExtractionResponse(**data)

        except Exception as e:
            
            self.logger.error(f"Gemini processing failed for page {page_number}. Error: {str(e)}")
            return ExtractionResponse(
                success=False, 
                page_number=page_number, 
                concepts=[], 
                error_message=str(e)
            )