import json
from google import genai
from google.genai import types

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder

# FLOW OF FUNCTIONS:
#
# extract_concepts_from_text (Public) -> Main entry point for text-based extraction.
#   └── Uses PromptBuilder to construct the request.
#
# extract_concepts_from_image (Public) -> Main entry point for vision-based extraction.
#   └── Uses PromptBuilder to construct the request.
#
# call_with_prompt_and_image (Public) -> Generic vision call helper.

class GeminiService:
    """
    Service for interacting with Google's Gemini models.
    Supports text and image extraction with automated fallback between models.
    """

    def __init__(self, config, logger):
        self.logger = logger
        primary_model = getattr(config, "GEMINI_MODEL", "gemini-2.5-flash")
        fallbacks = getattr(config, "GEMINI_MODEL_FALLBACKS", [])
        self.models = [primary_model] + fallbacks
        self.timeout_ms = getattr(config, "GEMINI_TIMEOUT_SECONDS", 120) * 1000
        self.client = genai.Client(api_key=config.GEMINI_API_KEY)
        self.logger.info(
            f"Gemini model fallback chain: {' -> '.join(self.models)}"
        )

    # --- PUBLIC METHODS ---

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        """
        Extracts educational concepts from a string of text.
        Iterates through configured models if failures occur.
        """
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)
        last_error = None

        for i, model_name in enumerate(self.models):
            try:
                response = self.client.models.generate_content(
                    model=model_name,
                    contents=prompt,
                    config=types.GenerateContentConfig(
                        response_mime_type="application/json",
                        response_schema=ExtractionResponse,
                        http_options=types.HttpOptions(timeout=self.timeout_ms),
                    ),
                )
                data = json.loads(response.text)
                if i > 0:
                    self.logger.info(
                        f"Gemini text extraction page {page_number} succeeded "
                        f"with fallback model {model_name}"
                    )
                return ExtractionResponse(**data)
            except Exception as e:
                last_error = str(e)
                if i < len(self.models) - 1:
                    self.logger.warning(
                        f"Gemini model {model_name} failed for text page {page_number}, "
                        f"falling back to {self.models[i + 1]}: {e}"
                    )
                else:
                    self.logger.error(
                        f"All Gemini models failed for text page {page_number}. "
                        f"Last error: {last_error}"
                    )

        return ExtractionResponse(
            success=False,
            page_number=page_number,
            concepts=[],
            error_message=f"All Gemini models failed. Last error: {last_error}",
        )

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        """
        Extracts educational concepts from image bytes (vision).
        Iterates through configured models if failures occur.
        """
        if not image_bytes:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[], error_message="No image bytes provided.")

        prompt = PromptBuilder.build_image_extraction_prompt()
        last_error = None

        for i, model_name in enumerate(self.models):
            try:
                image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/jpeg")
                response = self.client.models.generate_content(
                    model=model_name,
                    contents=[prompt, image_part],
                    config=types.GenerateContentConfig(
                        response_mime_type="application/json",
                        response_schema=ExtractionResponse,
                        http_options=types.HttpOptions(timeout=self.timeout_ms),
                    ),
                )
                data = json.loads(response.text)
                if i > 0:
                    self.logger.info(
                        f"Gemini vision extraction page {page_number} succeeded "
                        f"with fallback model {model_name}"
                    )
                return ExtractionResponse(**data)
            except Exception as e:
                last_error = str(e)
                if i < len(self.models) - 1:
                    self.logger.warning(
                        f"Gemini model {model_name} failed for vision page {page_number}, "
                        f"falling back to {self.models[i + 1]}: {e}"
                    )
                else:
                    self.logger.error(
                        f"All Gemini models failed for vision page {page_number}. "
                        f"Last error: {last_error}"
                    )

        return ExtractionResponse(
            success=False,
            page_number=page_number,
            concepts=[],
            error_message=f"All Gemini models failed. Last error: {last_error}",
        )

    def call_with_prompt_and_image(self, prompt: str, image_bytes: bytes) -> str | None:
        """
        Generic helper to send a prompt and image to Gemini and get the raw response text.
        """
        if not image_bytes:
            return None

        last_error = None
        for i, model_name in enumerate(self.models):
            try:
                image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/jpeg")
                response = self.client.models.generate_content(
                    model=model_name,
                    contents=[prompt, image_part],
                    config=types.GenerateContentConfig(
                        response_mime_type="application/json",
                        http_options=types.HttpOptions(timeout=self.timeout_ms),
                    ),
                )
                if i > 0:
                    self.logger.info(f"Gemini vision succeeded with fallback model {model_name}")
                return response.text
            except Exception as e:
                last_error = str(e)
                if i < len(self.models) - 1:
                    self.logger.warning(
                        f"Gemini model {model_name} failed for vision call, "
                        f"falling back to {self.models[i + 1]}: {e}"
                    )
                else:
                    self.logger.error(f"All Gemini models failed for vision call. Last error: {last_error}")

        return None
