"""
Word Formatter — Unified API response helpers (Step 1.2)

Every API endpoint must return responses through these helpers so that the
JSON envelope stays consistent across the entire service:

    {"success": true,  "code": 0,    "message": "OK",  "data": ...}
    {"success": false, "code": 1001, "message": "...",  "data": null}

Error codes follow API.md §4.1 (1000-1010), defined in shared.constants.
HTTP status codes follow API.md §4.2.
"""

from typing import Any

# ErrorCode lives in shared.constants (canonical location, Step 1.5).
# Re-exported here so callers can still write ``from backend.utils.response
# import ErrorCode`` without a circular dependency on shared.constants.
from shared.constants import ErrorCode  # noqa: F401


# Human-readable messages keyed by error code (keeps callers concise).
_ERROR_MESSAGES: dict[int, str] = {
    ErrorCode.SUCCESS: "OK",
    ErrorCode.UNKNOWN: "Unknown error",
    ErrorCode.FILE_NOT_FOUND: "File not found",
    ErrorCode.FILE_UNREADABLE: "File unreadable",
    ErrorCode.TEMPLATE_NOT_FOUND: "Template not found",
    ErrorCode.TEMPLATE_CORRUPT: "Template corrupted",
    ErrorCode.CONFIG_ERROR: "Configuration error",
    ErrorCode.PARAM_ERROR: "Invalid parameter",
    ErrorCode.TASK_NOT_FOUND: "Task not found",
    ErrorCode.TASK_CANCELLED: "Task cancelled",
    ErrorCode.OUTPUT_DIR_NOT_FOUND: "Output directory not found",
    ErrorCode.FORMAT_FAILED: "Format failed",
}


# ============================================================
# HTTP status code mapping (API.md §4.2)
# ============================================================

# Default HTTP status for each error code.  Callers can override via the
# ``http_status`` parameter of ``error_response`` when needed.
_DEFAULT_HTTP_STATUS: dict[int, int] = {
    ErrorCode.UNKNOWN: 500,
    ErrorCode.FILE_NOT_FOUND: 404,
    ErrorCode.FILE_UNREADABLE: 400,
    ErrorCode.TEMPLATE_NOT_FOUND: 404,
    ErrorCode.TEMPLATE_CORRUPT: 400,
    ErrorCode.CONFIG_ERROR: 400,
    ErrorCode.PARAM_ERROR: 400,
    ErrorCode.TASK_NOT_FOUND: 404,
    ErrorCode.TASK_CANCELLED: 400,
    ErrorCode.OUTPUT_DIR_NOT_FOUND: 404,
    ErrorCode.FORMAT_FAILED: 500,
}


# ============================================================
# Response builders
# ============================================================


def success_response(
    data: Any = None,
    message: str = "OK",
) -> dict[str, Any]:
    """Build a success envelope.

    Args:
        data: Payload to embed in ``data`` (any JSON-serialisable value).
        message: Human-readable success message (default ``"OK"``).

    Returns:
        Dict with ``success=True``, ``code=0``, ``message``, ``data``.
    """
    return {
        "success": True,
        "code": ErrorCode.SUCCESS,
        "message": message,
        "data": data,
    }


def error_response(
    code: int,
    message: str | None = None,
    http_status: int | None = None,
) -> tuple[dict[str, Any], int]:
    """Build an error envelope together with the HTTP status code.

    Args:
        code: Business error code from ``ErrorCode``.
        message: Override message; if *None* the default for *code* is used.
        http_status: Override HTTP status; if *None* the default mapping is used.

    Returns:
        ``(body_dict, http_status_code)`` — callers pass both to
        ``JSONResponse(content=body, status_code=status)``.
    """
    if message is None:
        message = _ERROR_MESSAGES.get(code, "Unknown error")
    if http_status is None:
        http_status = _DEFAULT_HTTP_STATUS.get(code, 400)

    body = {
        "success": False,
        "code": code,
        "message": message,
        "data": None,
    }
    return body, http_status
