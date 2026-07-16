"""File management service (Step 3.1).

Extracts all business logic from ``backend.api.files`` into a reusable
service class. Handles file list maintenance, add/remove/clear/search,
folder scanning, recent-open records, and pinned directory management.

The API layer (``backend.api.files``) should only validate request
parameters and delegate to this service.
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from backend.utils.logger import get_logger

logger = get_logger("backend.services.file_service", category="backend")

# Allowed file extensions
_ALLOWED_EXTENSIONS = {".doc", ".docx"}

# Maximum recent-open records to keep
_RECENT_MAX = 10


class FileService:
    """In-memory file management service.

    Manages the processing queue of .doc/.docx files, recent-open history,
    and pinned directories. Thread-safe via a single lock (all state mutations
    are quick dict/list operations).
    """

    def __init__(self) -> None:
        # Key: normalised file path, Value: file info dict
        self._files: dict[str, dict[str, Any]] = {}

        # Recent opened files/folders (most recent first, max 10)
        self._recent: list[dict[str, str]] = []

        # Pinned directories (normalised paths)
        self._pinned: list[str] = []

    # ------------------------------------------------------------------
    # Public query methods (read-only)
    # ------------------------------------------------------------------

    def get_all_files(self) -> list[dict[str, Any]]:
        """Return all file info dicts currently in the queue."""
        return list(self._files.values())

    def get_recent(self) -> list[dict[str, str]]:
        """Return recently opened files and folders (most recent first)."""
        return list(self._recent)

    def get_pinned(self) -> list[str]:
        """Return pinned directory paths."""
        return list(self._pinned)

    def search_files(self, keyword: str) -> list[dict[str, Any]]:
        """Filter the current file list by keyword (case-insensitive name/path match).

        Returns all files if keyword is empty.
        """
        keyword = keyword.strip().lower()
        if not keyword:
            return list(self._files.values())

        return [
            info for info in self._files.values()
            if keyword in info["name"].lower() or keyword in info["path"].lower()
        ]

    def file_count(self) -> int:
        """Return number of files in the queue."""
        return len(self._files)

    # ------------------------------------------------------------------
    # Public mutation methods
    # ------------------------------------------------------------------

    def add_files(self, paths: list[str]) -> dict[str, Any]:
        """Add one or more .doc/.docx files by path.

        Returns a dict with keys:
        - added (int): number of files successfully added
        - failures (list[str]): paths that could not be added
        - skipped (int): number of duplicate/already-present files
        """
        added = 0
        failures: list[str] = []
        skipped = 0

        for raw_path in paths:
            info = self._make_file_info(raw_path)
            if info is None:
                normalised = str(Path(raw_path).resolve())
                if normalised in self._files:
                    skipped += 1
                    continue
                failures.append(raw_path)
                continue

            key = info["path"]
            if key in self._files:
                skipped += 1
                continue

            self._files[key] = info
            self._add_recent(raw_path, "file")
            added += 1

        logger.info(
            "add_files: requested=%d, added=%d, skipped=%d, failed=%d",
            len(paths), added, skipped, len(failures),
        )

        return {"added": added, "failures": failures, "skipped": skipped}

    def add_folder(self, folder: str, include_subdir: bool = True) -> dict[str, Any]:
        """Scan a folder for .doc/.docx files and add them.

        Returns a dict with keys:
        - added (int): number of new files added
        - found (int): total files found in the folder

        Raises FileNotFoundError if the folder does not exist.
        """
        folder_path = Path(folder)
        if not folder_path.is_dir():
            raise FileNotFoundError(f"Folder not found: {folder}")

        found = self._scan_folder(folder, include_subdir)
        added = 0
        for fpath in found:
            if fpath in self._files:
                continue
            info = self._make_file_info(fpath)
            if info:
                self._files[fpath] = info
                added += 1

        self._add_recent(folder, "folder")
        logger.info(
            "add_folder: %s (subdir=%s), found=%d, added=%d",
            folder, include_subdir, len(found), added,
        )

        return {"added": added, "found": len(found)}

    def remove_files(self, paths: list[str]) -> int:
        """Remove files from the queue by path.

        Returns the number of files actually removed.
        """
        removed = 0
        for raw_path in paths:
            key = str(Path(raw_path).resolve())
            if key in self._files:
                del self._files[key]
                removed += 1

        logger.info("remove_files: requested=%d, removed=%d", len(paths), removed)
        return removed

    def clear_files(self) -> int:
        """Remove all files from the queue.

        Returns the number of files that were in the queue.
        """
        count = len(self._files)
        self._files.clear()
        logger.info("clear_files: %d files removed", count)
        return count

    def pin_folder(self, folder: str) -> list[str]:
        """Pin a frequently used directory.

        Returns the updated pinned directories list.

        Raises FileNotFoundError if the folder does not exist.
        """
        folder_path = Path(folder)
        if not folder_path.is_dir():
            raise FileNotFoundError(f"Folder not found: {folder}")

        normalised = str(folder_path.resolve())
        if normalised not in self._pinned:
            self._pinned.append(normalised)

        logger.info("pin_folder: %s", normalised)
        return list(self._pinned)

    # ------------------------------------------------------------------
    # Private helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _now_iso() -> str:
        return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    @staticmethod
    def _make_file_info(path: str) -> dict[str, Any] | None:
        """Build a file info dict for an existing .doc/.docx file.

        Returns ``None`` if the path does not exist or is not an allowed type.
        """
        p = Path(path)
        if not p.exists():
            return None
        if p.suffix.lower() not in _ALLOWED_EXTENSIONS:
            return None
        try:
            stat = p.stat()
        except OSError:
            return None
        return {
            "id": uuid.uuid4().hex[:12],
            "name": p.name,
            "path": str(p.resolve()),
            "size": stat.st_size,
            "modifiedTime": datetime.fromtimestamp(
                stat.st_mtime, tz=timezone.utc
            ).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "status": "waiting",
        }

    def _add_recent(self, path: str, entry_type: str) -> None:
        """Insert or move *path* to the front of the recent list."""
        normalised = str(Path(path).resolve())
        self._recent = [r for r in self._recent if r["path"] != normalised]
        self._recent.insert(0, {"path": normalised, "type": entry_type})
        self._recent = self._recent[:_RECENT_MAX]

    @staticmethod
    def _scan_folder(folder: str, include_subdir: bool) -> list[str]:
        """Return sorted list of .doc/.docx paths found in *folder*."""
        results: list[str] = []
        root = Path(folder)
        if not root.is_dir():
            return results
        pattern = "**/*" if include_subdir else "*"
        for entry in root.glob(pattern):
            if entry.is_file() and entry.suffix.lower() in _ALLOWED_EXTENSIONS:
                results.append(str(entry.resolve()))
        results.sort()
        return results


# Module-level singleton — the API layer imports this instance
file_service = FileService()
