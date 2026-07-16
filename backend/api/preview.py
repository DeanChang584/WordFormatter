"""Preview API endpoints (Step 4.2 + Step X.X).

Implements API.md §10:
  • POST /api/preview       — Level 1: parameter-summary text
  • POST /api/preview/pdf   — Level 2: formatted document preview

The preview endpoint formats the document with ``format_docx`` and returns
the path to the formatted ``.docx`` file.  The frontend then converts it to
PDF via WPS/Word COM and displays it in WebView2 + PDF.js.
"""

from __future__ import annotations

import os
import tempfile
import threading
from pathlib import Path
from typing import Any

from fastapi import APIRouter
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from backend.formatter.engine import (
    format_docx,
    convert_doc_to_docx,
    HAS_COM,
)
from backend.preview.generator import generate_preview
from backend.utils.logger import get_logger
from backend.utils.response import ErrorCode, error_response, success_response
from shared.schemas import ProfileConfig

logger = get_logger("backend.api.preview", category="backend")

router = APIRouter(prefix="/preview", tags=["preview"])


def _error(code: int, message: str | None = None) -> JSONResponse:
    """Shortcut: build error envelope and return as JSONResponse."""
    body, status = error_response(code, message)
    return JSONResponse(content=body, status_code=status)


# ============================================================
# Request model
# ============================================================


class PreviewRequest(BaseModel):
    """POST /preview request body (API.md §10)."""

    file: str = ""  # optional file path (reserved for Level 2)
    profile: str | dict[str, Any] = "default"


# ============================================================
# Endpoint
# ============================================================


@router.post("")
async def preview(req: PreviewRequest):
    """Generate a parameter-summary preview for the given profile.

    The ``profile`` field accepts either a template ID string or a full
    ``ProfileConfig`` dict (same contract as ``POST /format/start``).
    """
    # Resolve profile (reuse the same logic as format service)
    try:
        resolved = _resolve_profile(req.profile)
    except Exception as exc:
        logger.warning("Failed to resolve profile for preview: %s", exc)
        return _error(ErrorCode.CONFIG_ERROR, str(exc))

    text = generate_preview(resolved)
    logger.info("Preview generated (%d chars)", len(text))
    return success_response({"preview": text})


# ============================================================
# PDF Preview — request model
# ============================================================


class PdfPreviewRequest(BaseModel):
    """POST /preview/pdf request body."""

    file: str = Field(..., description="Path to the .doc/.docx file")
    profile: str | dict[str, Any] = "default"


# ============================================================
# PDF Preview — task state (in-memory, keyed by task_id)
# ============================================================

_pdf_tasks: dict[str, dict[str, Any]] = {}
_pdf_tasks_lock = threading.Lock()


# ============================================================
# PDF Preview — endpoints
# ============================================================


@router.post("/pdf")
async def preview_pdf(req: PdfPreviewRequest):
    """Start PDF preview generation for a single file.

    Returns HTTP 202 with a ``taskId`` on success. The caller polls
    ``GET /preview/pdf/{taskId}`` for completion.
    """
    if not req.file:
        return _error(ErrorCode.FILE_NOT_FOUND, "请提供文件路径")

    if not os.path.exists(req.file):
        return _error(ErrorCode.FILE_NOT_FOUND, f"文件不存在: {req.file}")

    ext = Path(req.file).suffix.lower()
    if ext not in (".doc", ".docx"):
        return _error(ErrorCode.PARAM_ERROR,
                      "该文件格式暂不支持预览")

    try:
        profile = _resolve_profile(req.profile)
    except Exception as exc:
        logger.warning("Failed to resolve profile for PDF preview: %s", exc)
        return _error(ErrorCode.CONFIG_ERROR, str(exc))

    task_id = _new_pdf_task_id()
    cancel_event = threading.Event()

    with _pdf_tasks_lock:
        _pdf_tasks[task_id] = {
            "status": "preparing",
            "pdf_path": None,
            "error": None,
            "cancel": cancel_event,
        }

    # Start background thread
    thread = threading.Thread(
        target=_run_pdf_task,
        args=(task_id, req.file, profile, cancel_event),
        daemon=True,
    )
    thread.start()

    return JSONResponse(
        content=success_response({"taskId": task_id}),
        status_code=202,
    )


@router.get("/pdf/{task_id}")
async def preview_pdf_status(task_id: str):
    """Poll PDF preview task status.

    Returns ``{state, pdfPath, error}``. When state is ``"completed"``,
    ``pdfPath`` contains the absolute path to the generated PDF.
    """
    with _pdf_tasks_lock:
        task = _pdf_tasks.get(task_id)

    if task is None:
        return _error(ErrorCode.TASK_NOT_FOUND, "预览任务不存在")

    return success_response({
        "state": task["status"],
        "previewPath": task["pdf_path"],
        "error": task["error"],
    })


@router.post("/pdf/{task_id}/cancel")
async def preview_pdf_cancel(task_id: str):
    """Cancel a running PDF preview task."""
    with _pdf_tasks_lock:
        task = _pdf_tasks.get(task_id)

    if task is None:
        return _error(ErrorCode.TASK_NOT_FOUND, "预览任务不存在")

    task["cancel"].set()
    logger.info("PDF preview task %s cancelled", task_id)
    return success_response({"cancelled": True})


# ============================================================
# PDF Preview — background task
# ============================================================


def _run_pdf_task(
    task_id: str,
    filepath: str,
    profile: ProfileConfig,
    cancel: threading.Event,
) -> None:
    """Background thread: format a doc → docx, then export to PDF via COM."""
    try:
        with _pdf_tasks_lock:
            _pdf_tasks[task_id]["status"] = "running"

        ext = Path(filepath).suffix.lower()
        docx_path: str | None = None
        delete_docx = False

        # Step 1: get a .docx to work with
        if ext == ".doc":
            if cancel.is_set():
                return _finish_pdf(task_id, "cancelled", None, None)

            if not HAS_COM:
                return _finish_pdf(
                    task_id, "error", None,
                    "预览 .doc 文件需要安装 pywin32。请使用 .docx 格式。",
                )
            ok, msg, converted = convert_doc_to_docx(filepath)
            if not ok or converted is None:
                return _finish_pdf(task_id, "error", None, msg)
            docx_path = converted
            delete_docx = True
        else:
            docx_path = filepath

        if cancel.is_set():
            return _finish_pdf(task_id, "cancelled", None, None)

        # Step 2: format the docx (in-place on a temp copy)
        tmp_docx = os.path.join(
            tempfile.gettempdir(),
            f"wf_preview_{task_id}.docx",
        )
        ok, msg, _ = format_docx(docx_path, profile, output_path=tmp_docx)
        if not ok:
            return _finish_pdf(task_id, "error", None, msg)

        if delete_docx:
            try:
                os.remove(docx_path)
            except Exception:
                pass

        if cancel.is_set():
            return _finish_pdf(task_id, "cancelled", None, None)

        # Step 3: return the formatted .docx path — the frontend handles
        # WPS/Word COM → PDF conversion
        return _finish_pdf(task_id, "completed", tmp_docx, None)

    except Exception as exc:
        logger.exception("PDF preview task %s failed", task_id)
        return _finish_pdf(task_id, "error", None, str(exc))


def _finish_pdf(
    task_id: str,
    status: str,
    pdf_path: str | None,
    error: str | None,
) -> None:
    with _pdf_tasks_lock:
        if task_id in _pdf_tasks:
            _pdf_tasks[task_id]["status"] = status
            _pdf_tasks[task_id]["pdf_path"] = pdf_path
            _pdf_tasks[task_id]["error"] = error


def _new_pdf_task_id() -> str:
    import uuid
    return f"pdf_{uuid.uuid4().hex[:12]}"


# ============================================================
# Text preview helpers (Level 1)
# ============================================================


def _resolve_profile(spec: str | dict[str, Any]) -> ProfileConfig:
    """Resolve a profile spec to ProfileConfig.

    Mirrors the logic in ``FormatService._resolve_profile`` so the
    preview endpoint shares the same template-lookup behaviour.
    """
    if isinstance(spec, str) and spec:
        try:
            from backend.services.template_service import template_service
            tmpl = template_service.get_template(spec)
            if tmpl is not None:
                return tmpl.profile
        except (KeyError, FileNotFoundError, OSError):
            pass
        return ProfileConfig()

    if isinstance(spec, dict):
        return ProfileConfig.from_dict(spec)

    return ProfileConfig()
