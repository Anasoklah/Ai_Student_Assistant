import json
import re
import httpx

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder


class OpenRouterService:
    """
    Fallback extraction service using OpenRouter's free-tier vision models.
    Uses OpenAI-compatible chat completions API.
    """

    API_URL = "https://openrouter.ai/api/v1/chat/completions"

    def __init__(self, config, logger):
        self.logger = logger
        self.api_key = config.OPENROUTER_API_KEY
        self.model = config.OPENROUTER_MODEL
        self.timeout = config.OPENROUTER_TIMEOUT_SECONDS
        self.enabled = bool(self.api_key and self.api_key != "your-openrouter-api-key-here")

        if self.enabled:
            self.logger.info(f"OpenRouter fallback enabled. Model: {self.model}")
        else:
            self.logger.warning("OpenRouter API key not set. Fallback will be unavailable.")

    def _extract_json(self, text: str) -> str:
        """Extract JSON from text that may contain other content."""
        text = text.strip()
        
        # Try direct parse first
        if text.startswith("{"):
            return text
        
        # Try extracting from markdown code blocks
        match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
        if match:
            return match.group(1).strip()
        
        # Try finding a JSON object anywhere in the text
        # This is a simple heuristic: find the first { and last }
        try:
            start = text.index("{")
            end = text.rindex("}") + 1
            candidate = text[start:end]
            # Validate it's actually JSON by attempting to parse
            json.loads(candidate)
            return candidate
        except (ValueError, json.JSONDecodeError):
            pass
        
        # If all else fails, return as-is and let parse error handle it
        return text

    def _fix_json_escapes(self, text: str) -> str:
        """Fix invalid backslash escapes that models produce (e.g. \$m\$ -> $m$)."""
        # Remove backslashes before characters that don't need escaping in JSON
        # Keep valid JSON escapes: \n, \t, \", \\, \/, \b, \f, \r, \uXXXX
        return re.sub(r'\\([^"\\\/bfnrtu])', r'\1', text)

    def _parse_response(self, response_text: str, page_number: int) -> ExtractionResponse:
        """Parse JSON response from OpenRouter into ExtractionResponse."""
        self.logger.info(f"OpenRouter raw response for page {page_number}: {response_text[:300]}")
        json_str = self._extract_json(response_text)
        json_str = self._fix_json_escapes(json_str)
        data = json.loads(json_str)

        if "success" not in data:
            data["success"] = True
        if "page_number" not in data:
            data["page_number"] = page_number
        if "concepts" not in data:
            data["concepts"] = []

        return ExtractionResponse(**data)

    def call_with_prompt_and_image(self, prompt: str, image_bytes: bytes) -> str | None:
        """
        Send a custom prompt + image to OpenRouter and return raw response text.
        Used by StructureExtractor for vision-based TOC extraction.
        Returns None on failure.
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

        try:
            response = httpx.post(
                self.API_URL,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": self.model,
                    "messages": messages,
                    "temperature": 0.1,
                    "max_tokens": 2048,
                    "provider": {
                        "allow_fallbacks": True
                    },
                },
                timeout=self.timeout,
            )

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
            self.logger.warning(f"OpenRouter vision call failed: {e}")
            return None

    def extract_concepts_from_text(self, page_number: int, text: str) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenRouter not configured.")
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])
        prompt = PromptBuilder.build_extraction_prompt(text)
        return self._call_api(page_number, prompt)

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenRouter not configured.")
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
                       "image_url": {
                        "url": f"data:image/png;base64,{b64}",
                        },
                    },
                ],
            })
        else:
            messages.append({"role": "user", "content": prompt})

        try:
            self.logger.info(
            f"OpenRouter request - model: {self.model}, "
            f"image size: {len(image_bytes) if image_bytes else 0} bytes"
            )
            response = httpx.post(
                self.API_URL,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                },
               json={
                "model": self.model,
                "messages": messages,
                "temperature": 0.1,
                "max_tokens": 2048,
                "provider": {
                  "allow_fallbacks": True
                        }
                },
                timeout=self.timeout,
            )

            if response.status_code == 429:
                raise Exception("RESOURCE_EXHAUSTED")

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
