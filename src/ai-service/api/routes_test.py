from fastapi import APIRouter, UploadFile, File, Form, Depends, HTTPException

router = APIRouter(prefix="/api/v1/extraction", tags=["Test"])

_ALLOWED_IMAGE_TYPES = {"image/png", "image/jpeg", "image/jpg", "image/webp"}
_ALLOWED_IMAGE_EXTS = ("png", "jpg", "jpeg", "webp")


def get_opencode_service():
    from app import opencode_service
    return opencode_service


async def _read_image(file: UploadFile | None) -> bytes | None:
    if not file:
        return None
    if file.content_type not in _ALLOWED_IMAGE_TYPES:
        ext = file.filename.lower().split(".")[-1] if file.filename else ""
        if ext not in _ALLOWED_IMAGE_EXTS:
            raise HTTPException(status_code=400, detail="Only PNG/JPG/WebP images are supported.")
    return await file.read()


@router.post("/test-model")
async def try_single_model(
    file: UploadFile | None = File(None, description="Optional image file"),
    prompt: str = Form("Extract all educational concepts from this content in Arabic."),
    model: str = Form(..., description="Model to try, e.g. big-pickle."),
    opencode: "OpenCodeService" = Depends(get_opencode_service),
):
    """
    Test a single OpenCode Zen model with arbitrary text or image input.

    - If a `file` is provided, it's sent as a vision request.
    - If only `prompt` is provided, it's sent as a text request.

    Returns the raw model response (or the HTTP error) so you can inspect it.
    """
    if not opencode or not opencode.enabled:
        raise HTTPException(status_code=400, detail="OpenCode service is not configured. Set OPENCODE_API_KEY.")

    image_bytes = await _read_image(file)
    messages = opencode._build_messages(prompt, image_bytes)
    status, content, raw = opencode._post(model, messages)

    return {
        "model": model,
        "http": status,
        "ok": status == 200 and bool(content and content.strip()),
        "content": content,
        "raw": raw,
    }


@router.post("/probe-models")
async def probe_models(
    file: UploadFile | None = File(None, description="Optional image file — if given, an image call is run for each model too."),
    prompt: str = Form("Reply with the single word OK."),
    opencode: "OpenCodeService" = Depends(get_opencode_service),
):
    """
    Probe ALL free models to build a capability matrix.

    Runs a text call for every model, and (when a `file` is provided) an image call too.
    Does not stop at the first success — reports each model's text/image result so you can
    see which models accept text only vs which also accept images.
    """
    if not opencode or not opencode.enabled:
        raise HTTPException(status_code=400, detail="OpenCode service is not configured. Set OPENCODE_API_KEY.")

    image_bytes = await _read_image(file)
    return opencode.probe_all(prompt=prompt, image_bytes=image_bytes)
