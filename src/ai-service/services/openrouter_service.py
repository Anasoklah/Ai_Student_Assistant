import json
import httpx

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder
from services.json_extraction import parse_json_lenient
from services.extraction_schema import (
    json_schema_response_format,
    json_object_response_format,
)

# FLOW OF FUNCTIONS:
#
# extract_concepts_from_text (Public) -> Main entry point for text-based extraction.
#   └── _call_api (Private)
#       ├── _build_payload (Private)
#       └── _parse_response (Private)
#
# extract_concepts_from_image (Public) -> Main entry point for vision-based extraction.
#   └── _call_api (Private)
#       ├── _build_payload (Private)
#       └── _parse_response (Private)
#
# call_with_prompt_and_image (Public) -> Generic vision call helper for structure extraction.

class OpenRouterService:
    """
    Fallback extraction service using OpenRouter's free-tier models.
    Uses OpenAI-compatible chat completions API with structured output.
    """

    API_URL = "https://openrouter.ai/api/v1/chat/completions"

    # All free-tier OpenRouter models, tried in order until one succeeds.
    FREE_MODELS = [
        "nvidia/nemotron-3-ultra-550b-a55b:free",
        "inclusionai/ling-3.0-flash:free",
        "nvidia/nemotron-3-super-120b-a12b:free",
        "google/gemma-4-31b-it:free",
        "cohere/north-mini-code:free",
        "openai/gpt-oss-20b:free"
    ]

    def __init__(self, config, logger):
        self.logger = logger
        self.api_key = config.OPENROUTER_API_KEY
        self.models = getattr(config, "OPENROUTER_MODELS", None) or list(self.FREE_MODELS)
        self.timeout = config.OPENROUTER_TIMEOUT_SECONDS
        self.temperature = getattr(config, "OPENROUTER_TEMPERATURE", 0.1)
        self.max_output_tokens = getattr(config, "OPENROUTER_MAX_OUTPUT_TOKENS", 8192)
        self.structured_output = getattr(config, "OPENROUTER_STRUCTURED_OUTPUT", True)
        self._schema_supported = True
        self.enabled = bool(self.api_key and self.api_key != "your-openrouter-api-key-here")

        if self.enabled:
            self.logger.info(
                f"OpenRouter fallback enabled (text-only). "
                f"models={' -> '.join(self.models)} "
                f"structured_output={self.structured_output}"
            )
        else:
            self.logger.warning("OpenRouter API key not set. Fallback will be unavailable.")

    # --- PUBLIC METHODS ---

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        """
        Extracts concepts from text using OpenRouter's free models.
        """
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenRouter not configured.")
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])
        prompt = PromptBuilder.build_extraction_prompt(text)
        return self._call_api(page_number, prompt)

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        """
        Extracts concepts from image bytes using OpenRouter's free models.
        """
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenRouter not configured.")
        prompt = PromptBuilder.build_image_extraction_prompt()
        return self._call_api(page_number, prompt, image_bytes)

    def call_with_prompt_and_image(self, prompt: str, image_bytes: bytes) -> str | None:
        """
        Generic helper to send a custom prompt + image to OpenRouter.
        Returns raw response text or None on failure.
        """
        if not self.enabled or not image_bytes:
            return None

        import base64

        system_msg = (
            "You are a JSON extraction assistant. You MUST respond with valid JSON only. "
            "No markdown, no explanations, no text before or after."
        )

        b64 = base64.b64encode(image_bytes).decode("utf-8")
        messages = [
            {"role": "system", "content": system_msg},
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": prompt},
                    {
                        "type": "image_url",
                        "image_url": {
                            "url": f"data:image/png;base64,{b64}",
                        },
                    },
                ],
            },
        ]

        for index, model in enumerate(self.models):
            try:
                response = httpx.post(
                    self.API_URL,
                    headers={
                        "Authorization": f"Bearer {self.api_key}",
                        "Content-Type": "application/json",
                    },
                    json={
                        "model": model,
                        "messages": messages,
                        "temperature": self.temperature,
                        "max_tokens": self.max_output_tokens,
                        "provider": {
                            "allow_fallbacks": True
                        },
                    },
                    timeout=self.timeout,
                )

                if response.status_code == 429:
                    if index < len(self.models) - 1:
                        self.logger.warning(
                            f"OpenRouter model {model} is rate limited; "
                            f"trying {self.models[index + 1]}."
                        )
                        continue
                    self.logger.warning("All OpenRouter models are rate limited")
                    return None

                if response.status_code != 200:
                    self.logger.warning(f"OpenRouter vision call failed with status {response.status_code}: {response.text[:200]}")
                    return None

                result = response.json()
                if "choices" not in result or not result["choices"]:
                    self.logger.warning("OpenRouter vision call returned no choices")
                    return None

                content = result["choices"][0]["message"]["content"]
                return content if content and content.strip() else None

            except Exception as e:
                self.logger.warning(f"OpenRouter vision call failed for model {model}: {e}")
                return None

    # --- PRIVATE METHODS ---

    def _build_payload(self, prompt: str, image_bytes: bytes | None, use_schema: bool, model: str) -> dict:
        """
        Constructs the API request payload.
        """
        import base64

        system_msg = (
            "You are a JSON extraction assistant. You MUST respond with valid JSON only. "
            "No markdown, no explanations, no text before or after. "
            "The JSON must contain: success (boolean), page_number (integer), concepts (array)."
        )

        messages = [{"role": "system", "content": system_msg}]

        if image_bytes:
            b64 = base64.b64encode(image_bytes).decode("utf-8")
            messages.append({
                "role": "user",
                "content": [
                    {"type": "text", "text": prompt},
                    {
                        "type": "image_url",
                        "image_url": {
                            "url": f"data:image/png;base64,{b64}",
                        },
                    },
                ],
            })
        else:
            messages.append({"role": "user", "content": prompt})

        payload = {
            "model": model,
            "messages": messages,
            "temperature": self.temperature,
            "max_tokens": self.max_output_tokens,
            "provider": {
                "allow_fallbacks": True
            },
        }
        if self.structured_output and use_schema and self._schema_supported:
            payload["response_format"] = json_schema_response_format()
        elif self.structured_output:
            payload["response_format"] = json_object_response_format()
        return payload

    def _call_api(self, page_number: int, prompt: str, image_bytes: bytes | None = None) -> ExtractionResponse:
        """
        Internal helper to handle the HTTP request, status codes, and model fallback logic.
        """
        try:
            models = self.models
            for index, model in enumerate(models):
                self.logger.info(
                    f"OpenRouter request - model: {model}, "
                    f"image size: {len(image_bytes) if image_bytes else 0} bytes"
                )
                payload = self._build_payload(prompt, image_bytes, use_schema=True, model=model)
                response = httpx.post(
                    self.API_URL,
                    headers={
                        "Authorization": f"Bearer {self.api_key}",
                        "Content-Type": "application/json",
                    },
                    json=payload,
                    timeout=self.timeout,
                )

                if response.status_code == 429:
                    if index < len(models) - 1:
                        self.logger.warning(
                            f"OpenRouter model {model} is rate limited for page {page_number}; "
                            f"trying {models[index + 1]}."
                        )
                        continue
                    return ExtractionResponse(
                        success=False, page_number=page_number, concepts=[],
                        error_message="All configured OpenRouter models are rate limited.",
                    )

                # Fallback if json_schema is not supported
                if response.status_code == 400 and self.structured_output and "response_format" in payload \
                        and payload["response_format"].get("type") == "json_schema":
                    self.logger.warning(
                        f"OpenRouter model {payload['model']} rejected json_schema; "
                        f"downgrading to json_object for this session."
                    )
                    self._schema_supported = False
                    payload = self._build_payload(prompt, image_bytes, use_schema=False, model=model)
                    response = httpx.post(
                        self.API_URL,
                        headers={
                            "Authorization": f"Bearer {self.api_key}",
                            "Content-Type": "application/json",
                        },
                        json=payload,
                        timeout=self.timeout,
                    )

                    if response.status_code == 429:
                        if index < len(models) - 1:
                            self.logger.warning(
                                f"OpenRouter model {model} is rate limited for page {page_number}; "
                                f"trying {models[index + 1]}."
                            )
                            continue
                        return ExtractionResponse(
                            success=False, page_number=page_number, concepts=[],
                            error_message="All configured OpenRouter models are rate limited.",
                        )

                if response.status_code in (502, 503, 504):
                    self.logger.warning(f"OpenRouter upstream error {response.status_code} for page {page_number} (model may be overloaded)")
                    return ExtractionResponse(
                        success=False, page_number=page_number, concepts=[],
                        error_message=f"OpenRouter upstream error {response.status_code}: model overloaded or timed out.",
                    )

                if response.status_code != 200:
                    error_detail = response.text[:500]
                    self.logger.error(f"OpenRouter API error {response.status_code} for page {page_number}: {error_detail}")
                    return ExtractionResponse(
                        success=False, page_number=page_number, concepts=[],
                        error_message=f"OpenRouter API error {response.status_code}: {error_detail}",
                    )

                result = response.json()

                if "choices" not in result or not result["choices"]:
                    self.logger.error(f"OpenRouter response missing 'choices' for page {page_number}: {json.dumps(result)[:500]}")
                    return ExtractionResponse(
                        success=False, page_number=page_number, concepts=[],
                        error_message="OpenRouter returned response without 'choices'.",
                    )

                content = result["choices"][0]["message"]["content"]
                if not content or not content.strip():
                    return ExtractionResponse(
                        success=False, page_number=page_number, concepts=[],
                        error_message="OpenRouter returned empty response.",
                    )

                if index > 0:
                    self.logger.info(f"OpenRouter page {page_number} succeeded with fallback model {model}")
                return self._parse_response(content, page_number)

        except httpx.TimeoutException:
            self.logger.error(f"OpenRouter timeout for page {page_number}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message="OpenRouter request timed out.",
            )
        except json.JSONDecodeError as e:
            self.logger.error(f"OpenRouter invalid JSON for page {page_number}: {str(e)}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=f"OpenRouter returned invalid JSON: {str(e)}",
            )
        except Exception as e:
            if "RESOURCE_EXHAUSTED" in str(e):
                raise
            self.logger.error(f"OpenRouter failed for page {page_number}: {str(e)}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=str(e),
            )

    def _parse_response(self, response_text: str, page_number: int) -> ExtractionResponse:
        """Parse JSON response from OpenRouter into ExtractionResponse."""
        self.logger.info(f"OpenRouter raw response for page {page_number}: {response_text[:200]}")

        data, reason = parse_json_lenient(response_text)
        if data is None:
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=f"OpenRouter JSON parse failed: {reason}",
            )

        if "success" not in data:
            data["success"] = True
        if "page_number" not in data:
            data["page_number"] = page_number
        if "concepts" not in data:
            data["concepts"] = []

        return ExtractionResponse(**data)
