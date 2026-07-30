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

class GroqService:
    """
    Free fallback extraction service using Groq's API.
    Uses OpenAI-compatible chat completions with vision support.
    """

    API_URL = "https://api.groq.com/openai/v1/chat/completions"

    def __init__(self, config, logger):
        self.logger = logger
        self.api_key = getattr(config, "GROQ_API_KEY", None)
        self.model = getattr(config, "GROQ_MODEL", "llama-3.3-70b-versatile")
        self.vision_model = getattr(config, "GROQ_VISION_MODEL", self.model)
        self.timeout = getattr(config, "GROQ_TIMEOUT_SECONDS", 120)
        self.temperature = getattr(config, "GROQ_TEMPERATURE", 0.1)
        self.max_output_tokens = getattr(config, "GROQ_MAX_OUTPUT_TOKENS", 8192)
        # When True, request strict json_schema decoding; on the first schema
        # rejection we downgrade this instance to json_object for the session.
        self.structured_output = getattr(config, "GROQ_STRUCTURED_OUTPUT", True)
        # Flipped to False for the session the first time the model rejects json_schema.
        self._schema_supported = True
        self.enabled = bool(self.api_key and self.api_key != "your-groq-api-key-here")

        if self.enabled:
            self.logger.info(
                f"Groq fallback enabled. text_model={self.model} "
                f"vision_model={self.vision_model} structured_output={self.structured_output}"
            )
        else:
            self.logger.warning("Groq API key not set. Fallback will be unavailable.")

        # Create HTTP client ONCE, reuse it
        self.http_client = httpx.Client(
            timeout=self.timeout,
            limits=httpx.Limits(
                max_connections=10,
                max_keepalive_connections=5,
            )
        )

    # --- PUBLIC METHODS ---

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        """
        Extracts concepts from text using Groq's API.
        """
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="Groq not configured.")

        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)
        return self._call_api(page_number, prompt)

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        """
        Extracts concepts from image bytes using Groq's vision models.
        """
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="Groq not configured.")

        prompt = PromptBuilder.build_image_extraction_prompt()
        return self._call_api(page_number, prompt, image_bytes)

    def call_with_prompt_and_image(self, prompt: str, image_bytes: bytes) -> str | None:
        """
        Generic helper to send a custom prompt + image to Groq.
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
                        "image_url": {"url": f"data:image/jpeg;base64,{b64}"},
                    },
                ],
            },
        ]

        try:
            response = self.http_client.post(
                self.API_URL,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": self.model,
                    "messages": messages,
                    "temperature": 0.1,
                },
                timeout=self.timeout,
            )

            if response.status_code != 200:
                self.logger.warning(f"Groq vision call failed with status {response.status_code}: {response.text[:200]}")
                return None

            result = response.json()
            if "choices" not in result or not result["choices"]:
                self.logger.warning("Groq vision call returned no choices")
                return None

            content = result["choices"][0]["message"]["content"]
            return content if content and content.strip() else None

        except Exception as e:
            self.logger.warning(f"Groq vision call failed: {e}")
            return None

    # --- PRIVATE METHODS ---

    def _build_payload(self, prompt: str, image_bytes: bytes, use_schema: bool) -> dict:
        """
        Constructs the API request payload, including system message and image data.
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
                        "image_url": {"url": f"data:image/jpeg;base64,{b64}"},
                    },
                ],
            })
        else:
            messages.append({"role": "user", "content": prompt})

        payload = {
            "model": self.vision_model if image_bytes else self.model,
            "messages": messages,
            "temperature": self.temperature,
            "max_tokens": self.max_output_tokens,
        }
        if self.structured_output and use_schema and self._schema_supported:
            payload["response_format"] = json_schema_response_format()
        elif self.structured_output:
            payload["response_format"] = json_object_response_format()
        return payload

    def _call_api(self, page_number: int, prompt: str, image_bytes: bytes = None) -> ExtractionResponse:
        """
        Internal helper to handle the HTTP request, status codes, and retries for schema fallback.
        """
        try:
            payload = self._build_payload(prompt, image_bytes, use_schema=True)
            response = self.http_client.post(
                self.API_URL,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                },
                json=payload,
                timeout=self.timeout,
            )

            if response.status_code == 429:
                raise Exception("RESOURCE_EXHAUSTED")

            # Fallback if json_schema is not supported by the model
            if response.status_code == 400 and self.structured_output and "response_format" in payload \
                    and payload["response_format"].get("type") == "json_schema":
                self.logger.warning(
                    f"Groq model {payload['model']} rejected json_schema; "
                    f"downgrading to json_object for this session."
                )
                self._schema_supported = False
                payload = self._build_payload(prompt, image_bytes, use_schema=False)
                response = self.http_client.post(
                    self.API_URL,
                    headers={
                        "Authorization": f"Bearer {self.api_key}",
                        "Content-Type": "application/json",
                    },
                    json=payload,
                    timeout=self.timeout,
                )

            if response.status_code != 200:
                error_detail = response.text[:500]
                self.logger.error(f"Groq API error {response.status_code} for page {page_number}: {error_detail}")
                return ExtractionResponse(
                    success=False, page_number=page_number, concepts=[],
                    error_message=f"Groq API error {response.status_code}: {error_detail}",
                )

            result = response.json()

            if "choices" not in result or not result["choices"]:
                self.logger.error(f"Groq response missing 'choices' for page {page_number}: {json.dumps(result)[:500]}")
                return ExtractionResponse(
                    success=False, page_number=page_number, concepts=[],
                    error_message="Groq returned response without 'choices'.",
                )

            content = result["choices"][0]["message"]["content"]
            if not content or not content.strip():
                self.logger.error(f"Groq returned empty content for page {page_number}")
                return ExtractionResponse(
                    success=False, page_number=page_number, concepts=[],
                    error_message="Groq returned empty response.",
                )

            return self._parse_response(content, page_number)

        except httpx.TimeoutException:
            self.logger.error(f"Groq timeout for page {page_number}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message="Groq request timed out.",
            )
        except json.JSONDecodeError as e:
            self.logger.error(f"Groq invalid JSON for page {page_number}: {str(e)}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=f"Groq returned invalid JSON: {str(e)}",
            )
        except Exception as e:
            if "RESOURCE_EXHAUSTED" in str(e):
                raise
            self.logger.error(f"Groq failed for page {page_number}: {str(e)}")
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=str(e),
            )

    def _parse_response(self, response_text: str, page_number: int) -> ExtractionResponse:
        """Parse JSON response from Groq into ExtractionResponse."""
        self.logger.info(f"Groq raw response for page {page_number}: {response_text[:200]}")

        data, reason = parse_json_lenient(response_text)
        if data is None:
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=f"Groq JSON parse failed: {reason}",
            )

        if "success" not in data:
            data["success"] = True
        if "page_number" not in data:
            data["page_number"] = page_number
        if "concepts" not in data:
            data["concepts"] = []

        return ExtractionResponse(**data)
