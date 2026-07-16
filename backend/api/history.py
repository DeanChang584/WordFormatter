"""History record API endpoints (Step 4.1).

Implements API.md §11 — 3 endpoints for task history under ``/api/history``.

All business logic lives in ``backend.history.manager``.
This module handles only parameter validation and response formatting.
"""

from __future__ import annotations

from fastapi import APIRouter
from fastapi.responses import JSONResponse

from backend.history.manager import history_manager
from backend.utils.logger import get_logger
from backend.utils.response import ErrorCode, error_response, success_response

logger = get_logger("backend.api.history", category="backend")

router = APIRouter(prefix="/history", tags=["history"])


def _error(code: int, message: str | None = None) -> JSONResponse:
    """Shortcut: build error envelope and return as JSONResponse."""
    body, status = error_response(code, message)
    return JSONResponse(content=body, status_code=status)


# ============================================================
# Endpoints (API.md §11)
# ============================================================


# 11.1 — Get recent task list (last 20)
@router.get("")
async def get_history():
    """Return the most recent 20 history records (summary fields only)."""
    history = history_manager.get_all()
    return success_response({"history": history})


# 11.2 — Get task detail
@router.get("/{record_id}")
async def get_history_detail(record_id: str):
    """Return the full detail for a single history record."""
    detail = history_manager.get_detail(record_id)

    if detail is None:
        return _error(ErrorCode.TASK_NOT_FOUND)

    return success_response(detail)


# 11.3 — Clear all history
@router.delete("")
async def clear_history():
    """Delete all history records."""
    history_manager.clear()
    return success_response(message="History cleared")
