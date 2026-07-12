import json
from google import genai
from google.genai import types

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder


class GeminiService:
    def __init__(self, config, logger):
        self.logger = logger
        self.model_name = getattr(config, "GEMINI_MODEL", "gemini-2.5-flash")
        self.timeout_ms = getattr(config, "GEMINI_TIMEOUT_SECONDS", 120) * 1000
        self.client = genai.Client(api_key=config.GEMINI_API_KEY)

    def call_with_prompt_and_image(self, prompt: str, image_bytes: bytes) -> str | None:
        """
        Send a custom prompt + image to Gemini and return raw response text.
        Used by StructureExtractor for vision-based TOC extraction.
        Returns None on failure.
        """
        if not image_bytes:
            return None

        try:
            image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/jpeg")
            response = self.client.models.generate_content(
                model=self.model_name,
                contents=[prompt, image_part],
                config=types.GenerateContentConfig(
                    response_mime_type="application/json",
                    http_options=types.HttpOptions(timeout=self.timeout_ms),
                ),
            )
            return response.text
        except Exception as e:
            self.logger.error(f"Gemini vision call failed: {e}")
            return None

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)

        try:
            response = self.client.models.generate_content(
                model=self.model_name,
                contents=prompt,
                config=types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=ExtractionResponse,
                    http_options=types.HttpOptions(timeout=self.timeout_ms),
                ),
            )

            data = json.loads(response.text)
            return ExtractionResponse(**data)

        except Exception as e:
            self.logger.error(f"Gemini text processing failed for page {page_number}. Error: {str(e)}")
            return ExtractionResponse(
                success=False,
                page_number=page_number,
                concepts=[],
                error_message=str(e),
            )

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        if not image_bytes:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[], error_message="No image bytes provided.")

        prompt = PromptBuilder.build_image_extraction_prompt()

        try:
            image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/jpeg")
            response = self.client.models.generate_content(
                model=self.model_name,
                contents=[prompt, image_part],
                config=types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=ExtractionResponse,
                    http_options=types.HttpOptions(timeout=self.timeout_ms),
                ),
            )

            data = json.loads(response.text)
            return ExtractionResponse(**data)

        except Exception as e:
            self.logger.error(f"Gemini vision processing failed for page {page_number}. Error: {str(e)}")
            return ExtractionResponse(
                success=False,
                page_number=page_number,
                concepts=[],
                error_message=str(e),
            )
