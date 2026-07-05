import json
from google import genai
from google.genai import types

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder


class GeminiService:
    """
    Primary extraction service using Google Gemini.
    Raises Exception with "RESOURCE_EXHAUSTED" on 429 so the caller
    can fall back to an alternative provider.
    """

    def __init__(self, config, logger):
        self.logger = logger
        self.model_name = getattr(config, "GEMINI_MODEL", "gemini-1.5-flash")
        self.timeout_ms = getattr(config, "GEMINI_TIMEOUT_SECONDS", 120) * 1000
        self.client = genai.Client(api_key=config.GEMINI_API_KEY)

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
            error_str = str(e)
            if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str:
                self.logger.warning(f"Gemini rate-limited (429) on page {page_number}.")
                raise Exception("RESOURCE_EXHAUSTED")
            self.logger.error(f"Gemini processing failed for page {page_number}. Error: {error_str}")
            return ExtractionResponse(
                success=False,
                page_number=page_number,
                concepts=[],
                error_message=error_str,
            )

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        """
        Sends a page image to Gemini's multimodal API with an extraction prompt.
        Used as fallback when text extraction yields empty content (scanned/image PDFs).
        """
        prompt = PromptBuilder.build_image_extraction_prompt()

        try:
            image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/png")

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
            error_str = str(e)
            if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str:
                self.logger.warning(f"Gemini rate-limited (429) on image for page {page_number}.")
                raise Exception("RESOURCE_EXHAUSTED")
            self.logger.error(f"Gemini image processing failed for page {page_number}. Error: {error_str}")
            return ExtractionResponse(
                success=False,
                page_number=page_number,
                concepts=[],
                error_message=error_str,
            )
