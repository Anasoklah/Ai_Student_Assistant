import json
import re
import httpx

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder


class GroqService:
    """
    Free fallback extraction service using Groq's API.
    Uses OpenAI-compatible chat completions with vision support.
    No billing required — just sign up and get an API key.
    """

    API_URL = "https://api.groq.com/openai/v1/chat/completions"

    def __init__(self, config, logger):
        self.logger = logger
        self.api_key = getattr(config, "GROQ_API_KEY", None)
        self.model = getattr(config, "GROQ_MODEL", "llama-3.1-8b")
        self.timeout = getattr(config, "GROQ_TIMEOUT_SECONDS", 30)
        self.enabled = bool(self.api_key and self.api_key != "your-groq-api-key-here")

        if self.enabled:
            self.logger.info(f"Groq fallback enabled. Model: {self.model}")
        else:
            self.logger.warning("Groq API key not set. Fallback will be unavailable.")

        # Create HTTP client ONCE, reuse it
        self.http_client = httpx.Client(
            timeout=self.timeout,
            limits=httpx.Limits(
                max_connections=10,  # Don't overwhelm the API
                max_keepalive_connections=5,
            )
        )

    def _extract_json(self, text: str) -> str:
        """Try to extract JSON from text that may contain other content."""
        text = text.strip()

        # Try direct parse first
        if text.startswith("{"):
            return text

        # Try extracting from markdown code blocks
        match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
        if match:
            return match.group(1).strip()

        # Try finding a JSON object anywhere in the text
        match = re.search(r"\{.*\}", text, re.DOTALL)
        if match:
            return match.group(0)

        return text

    def _fix_json_escapes(self, text: str) -> str:
        """Fix invalid backslash escapes that models produce (e.g. \$m\$ -> $m$)."""
        return re.sub(r'\\([^"\\\/bfnrtu])', r'\1', text)

    def _parse_response(self, response_text: str, page_number: int) -> ExtractionResponse:
        """Parse JSON response from Groq into ExtractionResponse."""
        self.logger.info(f"Groq raw response for page {page_number}: {response_text[:300]}")

        json_str = self._extract_json(response_text)
        json_str = self._fix_json_escapes(json_str)
        data = json.loads(json_str)

        # Fill in missing required fields with defaults
        if "success" not in data:
            data["success"] = True
        if "page_number" not in data:
            data["page_number"] = page_number
        if "concepts" not in data:
            data["concepts"] = []

        return ExtractionResponse(**data)

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="Groq not configured.")

        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)
        return self._call_api(page_number, prompt)

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="Groq not configured.")

        prompt = PromptBuilder.build_image_extraction_prompt()
        return self._call_api(page_number, prompt, image_bytes)

    def _call_api(self, page_number: int, prompt: str, image_bytes: bytes = None) -> ExtractionResponse:
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

            if response.status_code == 429:
                raise Exception("RESOURCE_EXHAUSTED")

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
