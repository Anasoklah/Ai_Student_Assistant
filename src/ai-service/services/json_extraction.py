r"""
Safe JSON extraction and repair for free-form LLM responses.

Only Gemini decodes under a hard schema constraint. Groq/OpenRouter can emit
prose, markdown fences, or JSON with LaTeX backslashes that aren't valid JSON
escapes. This module recovers JSON from that WITHOUT destroying content.

Critical difference from the old `_fix_json_escapes`:
    The old code DELETED stray backslashes (`re.sub(r'\\([^"\\/bfnrtu])', r'\1')`),
    which turned `\frac` into `frac` and `\times` into a literal TAB + "imes".
    That silently corrupted every equation off the Gemini path.

    Here we ESCAPE stray backslashes (`\` -> `\\`) so `\frac` survives JSON
    decoding as the literal `\frac`, preserving LaTeX exactly.
"""

from __future__ import annotations

import json
import re
from typing import Any


_FENCE_RE = re.compile(r"```(?:json)?\s*\n?(.*?)\n?\s*```", re.DOTALL)


def _strip_to_json_object(text: str) -> str:
    """Return the most likely JSON-object substring from a raw response."""
    text = (text or "").strip()
    if not text:
        return text

    if text.startswith("{"):
        return text

    fence = _FENCE_RE.search(text)
    if fence:
        return fence.group(1).strip()

    # Fall back to the widest {...} span.
    start = text.find("{")
    end = text.rfind("}")
    if start != -1 and end != -1 and end > start:
        return text[start:end + 1]

    return text


def _escape_invalid_backslashes(text: str) -> str:
    r"""
    Turn backslashes that are NOT part of a valid JSON escape into `\\`.

    JSON technically treats \b \f \n \r \t as valid escapes, but LaTeX commands
    collide with them: `\frac`, `\times`, `\beta`, `\nabla`, `\rho` all START
    with one of those letters. If we honoured them, `json.loads` would silently
    turn `\frac` into FORMFEED+"rac" and `\times` into TAB+"imes" even when the
    JSON parses "successfully" — the exact corruption that ruined equations off
    the Gemini path.

    So we deliberately recognise ONLY `\"`, `\\`, `\/` and `\uXXXX` as intended
    escapes and double every other backslash. A model that already escaped its
    LaTeX as `\\frac` is unaffected (the `\\` is preserved); a model that wrote
    raw `\frac` gets it doubled to `\\frac` so it decodes back to the literal
    `\frac`. The only casualty is an intended escaped-newline/tab, which becomes
    a literal `\n`/`\t` — a non-issue for content that is embedded after
    whitespace normalisation, and a good trade for never mangling an equation.
    """
    result = []
    i = 0
    n = len(text)
    valid = set('"\\/')
    while i < n:
        ch = text[i]
        if ch == "\\" and i + 1 < n:
            nxt = text[i + 1]
            if nxt == "u":
                # Valid only if followed by exactly 4 hex digits.
                if re.match(r"[0-9a-fA-F]{4}", text[i + 2:i + 6] or ""):
                    result.append(text[i:i + 6])
                    i += 6
                    continue
                result.append("\\\\")
                i += 1
                continue
            if nxt in valid:
                result.append(text[i:i + 2])
                i += 2
                continue
            # Stray backslash (LaTeX command, chemistry, etc.) -> preserve it.
            result.append("\\\\")
            i += 1
            continue
        result.append(ch)
        i += 1
    return "".join(result)


def parse_json_lenient(raw_text: str) -> tuple[dict[str, Any] | None, str | None]:
    """
    Best-effort parse of a model response into a dict.

    Returns (data, None) on success or (None, reason) on failure.
    Repair only ever ADDS escaping — it never removes characters — so it cannot
    silently drop LaTeX the way the old delete-backslash approach did.
    """
    if not raw_text or not raw_text.strip():
        return None, "empty_response"

    candidate = _strip_to_json_object(raw_text)

    # LaTeX-safe repair FIRST, always. We cannot rely on "strict parse succeeded"
    # as a signal of correctness: a raw `\frac` makes json.loads succeed while
    # silently corrupting the equation into control characters. Doubling stray
    # backslashes up front makes the common case (raw LaTeX) decode faithfully;
    # already-escaped `\\frac` passes through untouched.
    repaired = _escape_invalid_backslashes(candidate)
    try:
        data = json.loads(repaired)
        if isinstance(data, dict):
            return data, None
        return None, "not_an_object"
    except json.JSONDecodeError:
        pass

    # Fall back to the untouched candidate in case the repair over-escaped a
    # genuinely valid document (e.g. one that really did use \n line breaks).
    try:
        data = json.loads(candidate)
        if isinstance(data, dict):
            return data, None
        return None, "not_an_object"
    except json.JSONDecodeError as exc:
        # Detect the truncation signature (unterminated string / early EOF).
        msg = str(exc)
        if "Unterminated" in msg or ("Expecting" in msg and exc.pos >= len(candidate) - 2):
            return None, "truncated_or_malformed_json"
        return None, "invalid_json"
