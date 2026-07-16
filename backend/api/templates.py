"""Template management API endpoints (Step 1.8, refactored Step 3.3).

Implements API.md §8 — 7 template-management endpoints under ``/api/templates``.

All business logic has been extracted to ``backend.services.template_service``.
This module handles only parameter validation and response formatting.
"""

from __future__ import annotations

from typing import Any

from fastapi import APIRouter
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from backend.services.template_service import template_service
from backend.utils.logger import get_logger
from backend.utils.response import ErrorCode, error_response, success_response

logger = get_logger("backend.api.templates", category="backend")

router = APIRouter(prefix="/templates", tags=["templates"])


def _error(code: int, message: str | None = None) -> JSONResponse:
    """Shortcut: build error envelope and return as JSONResponse."""
    body, status = error_response(code, message)
    return JSONResponse(content=body, status_code=status)


# ============================================================
# Request models
# ============================================================


class CreateTemplateRequest(BaseModel):
    name: str
    profile: dict[str, Any] = Field(default_factory=dict)


class UpdateTemplateRequest(BaseModel):
    name: str | None = None
    profile: dict[str, Any] | None = None


class ImportTemplateRequest(BaseModel):
    path: str


class ExportTemplateRequest(BaseModel):
    template_id: str = Field(alias="templateId")
    target_path: str = Field(alias="targetPath")


class SetDefaultRequest(BaseModel):
    template_id: str = Field(alias="templateId")


# ============================================================
# Endpoints
# ============================================================


# 8.1 — Get template list
@router.get("")
async def get_templates() -> dict:
    """Return all templates (summary: id, name, isDefault)."""
    return success_response({"templates": template_service.get_all_templates()})


# 8.2 — Save (create) a new template
@router.post("")
async def create_template(req: CreateTemplateRequest):
    """Create a new template from the supplied name and profile data."""
    result = template_service.create_template(req.name, req.profile or None)
    return success_response(result)


# 8.3 — Update a template
@router.put("/{template_id}")
async def update_template(template_id: str, req: UpdateTemplateRequest):
    """Update a template's name and/or profile."""
    try:
        template_service.update_template(template_id, req.name, req.profile)
    except KeyError:
        return _error(ErrorCode.TEMPLATE_NOT_FOUND)

    return success_response(message="Template updated")


# 8.4 — Delete a template (default template cannot be deleted)
@router.delete("/{template_id}")
async def delete_template(template_id: str):
    """Delete a template. The default template cannot be deleted."""
    try:
        template_service.delete_template(template_id)
    except KeyError:
        return _error(ErrorCode.TEMPLATE_NOT_FOUND)
    except PermissionError:
        return _error(ErrorCode.CONFIG_ERROR, "Cannot delete the default template")

    return success_response(message="Template deleted")


# 8.5 — Import template from JSON file
@router.post("/import")
async def import_template(req: ImportTemplateRequest):
    """Import a template from a JSON file, validating the version field."""
    try:
        result = template_service.import_template(req.path)
    except FileNotFoundError as exc:
        return _error(ErrorCode.FILE_NOT_FOUND, str(exc))
    except ValueError as exc:
        return _error(ErrorCode.TEMPLATE_CORRUPT, str(exc))

    return success_response(result)


# 8.6 — Export template to JSON file
@router.post("/export")
async def export_template(req: ExportTemplateRequest):
    """Export a template to a JSON file at the specified directory."""
    try:
        exported_path = template_service.export_template(req.template_id, req.target_path)
    except KeyError:
        return _error(ErrorCode.TEMPLATE_NOT_FOUND)
    except FileNotFoundError as exc:
        return _error(ErrorCode.OUTPUT_DIR_NOT_FOUND, str(exc))
    except OSError as exc:
        return _error(ErrorCode.UNKNOWN, f"Write failed: {exc}")

    return success_response({"exportedFile": exported_path})


# 8.7 — Set default template
@router.post("/default")
async def set_default(req: SetDefaultRequest):
    """Designate a template as the default."""
    try:
        template_service.set_default(req.template_id)
    except KeyError:
        return _error(ErrorCode.TEMPLATE_NOT_FOUND)

    return success_response(message="Default template set")
