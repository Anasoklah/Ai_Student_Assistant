"""
Shared JSON schema + OpenAI-compatible ``response_format`` builders.

Gemini constrains decoding with a Pydantic ``response_schema``. Groq and
OpenRouter expose OpenAI-style structured output instead:

- ``json_schema``  — strict, schema-constrained decoding (best; supported by
  qwen/llama-3.3 on OpenRouter and a subset of Groq models).
- ``json_object``  — guarantees syntactically valid JSON but not the shape
  (the safe floor when a model rejects json_schema).

Keeping the schema in one place means the three providers stay in lockstep with
the ``ExtractionResponse`` contract the .NET backend consumes.
"""

from __future__ import annotations

from typing import Any

# JSON Schema mirroring models.dto.ExtractionResponse. Kept hand-written (rather
# than derived from Pydantic) so we can mark it ``strict`` and forbid extra keys,
# which is what makes json_schema mode reliable across providers.
EXTRACTION_JSON_SCHEMA: dict[str, Any] = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "success": {"type": "boolean"},
        "page_number": {"type": "integer"},
        "concepts": {
            "type": "array",
            "items": {
                "type": "object",
                "additionalProperties": False,
                "properties": {
                    "title": {"type": "string"},
                    "content": {"type": "string"},
                    "keywords": {
                        "type": "array",
                        "items": {"type": "string"},
                    },
                },
                "required": ["title", "content", "keywords"],
            },
        },
    },
    "required": ["success", "page_number", "concepts"],
}


def json_schema_response_format() -> dict[str, Any]:
    """OpenAI-compatible strict json_schema response_format."""
    return {
        "type": "json_schema",
        "json_schema": {
            "name": "extraction_response",
            "strict": True,
            "schema": EXTRACTION_JSON_SCHEMA,
        },
    }


def json_object_response_format() -> dict[str, Any]:
    """OpenAI-compatible json_object response_format (valid JSON, shape not enforced)."""
    return {"type": "json_object"}
