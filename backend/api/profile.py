"""Formatting profile API endpoints (Step 1.7).

Implements API.md §7 — 3 endpoints for reading, updating, and resetting
the current formatting profile under ``/api/profile``.

The profile is held in memory as a ``ProfileConfig`` instance (from
``shared.schemas``).  Default values come from ``ProfileConfig()`` which
matches data model.md §4.  The profile is *not* persisted here — that
is the responsibility of the template system (Step 1.8) and the config
manager (Step 1.3).

All responses use the unified envelope from ``backend.utils.response``.
"""

from __future__ import annotations

from typing import Any

from fastapi import APIRouter
from pydantic import BaseModel

from backend.utils.logger import get_logger
from backend.utils.response import success_response
from shared.schemas import ProfileConfig

logger = get_logger("backend.api.profile", category="backend")

router = APIRouter(prefix="/profile", tags=["profile"])

# ============================================================
# In-memory current profile (seeded with defaults)
# ============================================================

_current_profile: ProfileConfig = ProfileConfig()


# ============================================================
# Request models
# ============================================================


class UpdateProfileRequest(BaseModel):
    """Partial profile update — only supplied sub-objects are overwritten."""

    profile: dict[str, Any]


# ============================================================
# Endpoints
# ============================================================


# 7.1 — Get current profile
@router.get("")
async def get_profile() -> dict:
    """Return the full current formatting profile."""
    return success_response({"profile": _current_profile.to_dict()})


# 7.2 — Update current profile (partial merge)
@router.put("")
async def update_profile(req: UpdateProfileRequest) -> dict:
    """Merge the supplied profile fields into the current profile.

    Only the sub-objects present in the request body are updated;
    omitted sub-objects retain their current values.
    """
    global _current_profile

    current_dict = _current_profile.to_dict()

    # Deep-merge: for each top-level key in the request (page, body, heading …)
    # update only the fields that are provided.
    for section_key, section_value in req.profile.items():
        if section_key in current_dict and isinstance(section_value, dict):
            if isinstance(current_dict[section_key], dict):
                current_dict[section_key].update(section_value)
            else:
                current_dict[section_key] = section_value
        else:
            current_dict[section_key] = section_value

    _current_profile = ProfileConfig.from_dict(current_dict)
    logger.info("Profile updated: sections=%s", list(req.profile.keys()))

    return success_response(message="Profile updated")


# 7.3 — Reset profile to defaults
@router.post("/reset")
async def reset_profile() -> dict:
    """Restore the formatting profile to factory defaults."""
    global _current_profile
    _current_profile = ProfileConfig()
    logger.info("Profile reset to defaults")
    return success_response(message="Profile reset to default")
