import base64
import json

import httpx

from models.dto import ExtractionResponse
from services.prompt_builder import PromptBuilder
from services.json_extraction import parse_json_lenient


class OpenCodeService:
    """
    Extraction service using OpenCode Zen API.
    OpenAI-compatible endpoint supporting multiple free models.
    Falls back through the free model list if one fails.
    """

    API_URL = "https://opencode.ai/zen/v1/chat/completions"

    # These are "limited time" stealth models — re-probe periodically with
    # probe_all(). Split by capability so we NEVER send an image to a text-only
    # model (that just burns a call and returns an error).
    #
    # TEXT_MODELS: every model confirmed to accept text.
    # VISION_MODELS: the subset confirmed to also accept images. Images are only
    # ever sent to these.
    TEXT_MODELS = [
        "big-pickle",
        "deepseek-v4-flash-free",
        "mimo-v2.5-free",        
        "gpt-5-nano",
        "minimax-m2.5-free",
        "nemotron-3-ultra-free",
        "north-mini-code-free",
        "hy3-free",
    ]

    VISION_MODELS = [
        "mimo-v2.5-free",
        "big-pickle",
    ]

    # Back-compat alias for probe_all(): the full model set.
    FREE_MODELS = TEXT_MODELS

    SYSTEM_MSG = (
        "You are a JSON extraction assistant. You MUST respond with valid JSON only. "
        "No markdown, no explanations, no text before or after. "
        "The JSON must contain: success (boolean), page_number (integer), concepts (array)."
    )

    def __init__(self, config, logger):
        self.logger = logger
        self.api_key = getattr(config, "OPENCODE_API_KEY", "")
        self.timeout = getattr(config, "OPENCODE_TIMEOUT_SECONDS", 60)
        self.enabled = bool(self.api_key)

        # Allow env overrides so the (churning) stealth-model lists can be
        # updated without a code change. Fall back to the class defaults.
        self.text_models = getattr(config, "OPENCODE_TEXT_MODELS", None) or self.TEXT_MODELS
        self.vision_models = getattr(config, "OPENCODE_VISION_MODELS", None) or self.VISION_MODELS

        if self.enabled:
            self.logger.info(
                f"OpenCode Zen enabled. text_models={', '.join(self.text_models)} "
                f"vision_models={', '.join(self.vision_models)}"
            )
        else:
            self.logger.warning("OPENCODE_API_KEY not set. OpenCode fallback unavailable.")

        self.http_client = httpx.Client(
            timeout=self.timeout,
            limits=httpx.Limits(max_connections=10, max_keepalive_connections=5),
        )

    # --- request helpers ---------------------------------------------------

    def _build_messages(self, prompt: str, image_bytes: bytes | None = None) -> list:
        """Build the chat payload. Adds an image_url block only when image_bytes is given."""
        if image_bytes:
            b64 = base64.b64encode(image_bytes).decode("utf-8")
            user_content = [
                {"type": "text", "text": prompt},
                {"type": "image_url", "image_url": {"url": f"data:image/png;base64,{b64}"}},
            ]
        else:
            user_content = prompt

        return [
            {"role": "system", "content": self.SYSTEM_MSG},
            {"role": "user", "content": user_content},
        ]

    def _post(self, model: str, messages: list) -> tuple[int, str | None, dict | None]:
        """
        Single HTTP call to the completions endpoint.
        Returns (status_code, content_or_None, raw_json_or_None).
        status_code is 0 on transport-level failure (timeout, connection error).
        """
        try:
            resp = self.http_client.post(
                self.API_URL,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                },
                json={"model": model, "messages": messages, "temperature": 0.1},
                timeout=self.timeout,
            )
        except httpx.TimeoutException:
            self.logger.warning(f"OpenCode model {model} timed out")
            return 0, None, {"error": "Request timed out"}
        except Exception as e:
            self.logger.warning(f"OpenCode model {model} transport error: {e}")
            return 0, None, {"error": str(e)}

        try:
            raw = resp.json()
        except json.JSONDecodeError:
            return resp.status_code, None, {"error": resp.text[:300]}

        content = None
        choices = raw.get("choices") if isinstance(raw, dict) else None
        if choices:
            content = choices[0].get("message", {}).get("content")

        return resp.status_code, content, raw

    # --- JSON parsing ------------------------------------------------------

    def _parse_response(self, response_text: str, page_number: int) -> ExtractionResponse:
        """
        Parse a model response into ExtractionResponse using the shared
        LaTeX-safe parser (same as Groq/OpenRouter). The old local
        `_fix_json_escapes` DELETED stray backslashes, silently corrupting
        every equation — parse_json_lenient escapes them instead.
        """
        data, reason = parse_json_lenient(response_text)
        if data is None:
            return ExtractionResponse(
                success=False, page_number=page_number, concepts=[],
                error_message=f"OpenCode JSON parse failed: {reason}",
            )

        if "success" not in data:
            data["success"] = True
        if "page_number" not in data:
            data["page_number"] = page_number
        if "concepts" not in data:
            data["concepts"] = []
        return ExtractionResponse(**data)

    # --- public extraction API ---------------------------------------------

    def extract_concepts_from_text(self, page_number: int, text: str, model: str | None = None) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenCode not configured.")
        if not text.strip():
            return ExtractionResponse(success=True, page_number=page_number, concepts=[])

        prompt = PromptBuilder.build_extraction_prompt(text)
        return self._call_api(page_number, prompt, model=model)

    def extract_concepts_from_image(self, page_number: int, image_bytes: bytes, model: str | None = None) -> ExtractionResponse:
        if not self.enabled:
            return ExtractionResponse(success=False, page_number=page_number, concepts=[],
                                      error_message="OpenCode not configured.")

        prompt = PromptBuilder.build_image_extraction_prompt()
        return self._call_api(page_number, prompt, image_bytes, model=model)

    def _call_api(self, page_number: int, prompt: str, image_bytes: bytes | None = None, model: str | None = None) -> ExtractionResponse:
        # Image requests only ever go to vision-capable models; text requests
        # use the text list. This is what enforces "don't send images to the
        # text-only models".
        if model:
            models_to_try = [model]
        elif image_bytes:
            models_to_try = self.vision_models
        else:
            models_to_try = self.text_models

        messages = self._build_messages(prompt, image_bytes)
        last_error = None

        for m in models_to_try:
            status, content, raw = self._post(m, messages)

            if status == 429:
                self.logger.warning(f"OpenCode model {m} rate limited, trying next")
                last_error = "Rate limited"
                continue

            if status != 200:
                err = (raw or {}).get("error") if isinstance(raw, dict) else None
                last_error = f"OpenCode API error {status}: {err or (raw and str(raw)[:300])}"
                self.logger.warning(f"OpenCode model {m} failed: {last_error}")
                continue

            if not content or not content.strip():
                last_error = "Empty response"
                self.logger.warning(f"OpenCode model {m} returned empty content")
                continue

            parsed = self._parse_response(content, page_number)
            if parsed.error_message:
                last_error = parsed.error_message
                self.logger.warning(f"OpenCode model {m}: {parsed.error_message}")
                continue
            return parsed

        return ExtractionResponse(
            success=False, page_number=page_number, concepts=[],
            error_message=last_error or "All OpenCode models failed",
        )

    # --- capability probing ------------------------------------------------

    def _probe_one(self, model: str, prompt: str, image_bytes: bytes | None) -> dict:
        messages = self._build_messages(prompt, image_bytes)
        status, content, raw = self._post(model, messages)
        ok = status == 200 and bool(content and content.strip())
        entry = {"ok": ok, "http": status}
        if ok:
            entry["snippet"] = content.strip()[:200]
        else:
            err = (raw or {}).get("error") if isinstance(raw, dict) else None
            entry["error"] = err or (str(raw)[:200] if raw else "no content")
        return entry

    def probe_all(self, prompt: str = "Reply with the single word OK.", image_bytes: bytes | None = None) -> dict:
        """
        Probe every model in FREE_MODELS to discover capabilities.
        Runs a text call for each model, and (if image_bytes given) an image call too.
        Unlike a fallback loop, it does NOT stop at the first success — it reports the
        full capability matrix so you can see which models accept text vs images.
        """
        if not self.enabled:
            return {"enabled": False, "error": "OpenCode not configured.", "results": []}

        results = []
        for m in self.FREE_MODELS:
            entry = {"model": m, "text": self._probe_one(m, prompt, None)}
            if image_bytes:
                entry["image"] = self._probe_one(m, prompt, image_bytes)
            results.append(entry)

        return {"enabled": True, "probed_image": bool(image_bytes), "results": results}
