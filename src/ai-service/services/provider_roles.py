"""
Per-provider text/vision role resolution.

Each extraction provider is assigned a ROLE so it only spends its free-tier quota
on the path it's meant for:

- ``text``   -> participates only in the text (good-OCR) extraction path
- ``vision`` -> participates only in the image/vision fallback path
- ``both``   -> participates in both paths (legacy behavior)

Roles come from config (``<PROVIDER>_ROLE`` / ``<Provider>__Role``). A missing
config, unknown role, or empty value resolves to ``both`` so the split can never
silently drop a provider from every path.
"""

# config attribute holding each provider's role
_ROLE_ATTR = {
    "gemini": "GEMINI_ROLE",
    "groq": "GROQ_ROLE",
    "openrouter": "OPENROUTER_ROLE",
    "opencode": "OPENCODE_ROLE",
}

_VALID_ROLES = ("text", "vision", "both")


def resolve_role(config, provider_name: str) -> str:
    """Return the configured role for a provider, defaulting to ``both``."""
    attr = _ROLE_ATTR.get(provider_name.strip().lower())
    if config is None or attr is None:
        return "both"
    role = str(getattr(config, attr, "both") or "both").strip().lower()
    return role if role in _VALID_ROLES else "both"


def provider_allows(config, provider_name: str, is_vision: bool) -> bool:
    """Whether a provider may run for the given path, per its configured role."""
    role = resolve_role(config, provider_name)
    if role == "both":
        return True
    return role == ("vision" if is_vision else "text")
