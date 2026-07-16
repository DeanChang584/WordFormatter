"""Application data directory paths (resolved from %LOCALAPPDATA%).

All runtime data (templates, history, settings, logs) lives under
``%LOCALAPPDATA%\\WordFormatter\\`` so that the application works correctly
when installed in read-only locations such as ``Program Files``.

Each module imports the path it needs from this module:
    from backend.utils.app_paths import TEMPLATES_DIR, LOGS_DIR, ...
"""

from pathlib import Path
import os


def _ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


# Base: %LOCALAPPDATA%\WordFormatter
_base = Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "WordFormatter"
APP_DATA_DIR = _ensure_dir(_base)

# Sub-directories
TEMPLATES_DIR = _ensure_dir(_base / "templates")
HISTORY_DIR = _ensure_dir(_base / "history")
LOGS_DIR = _ensure_dir(_base / "logs")

# Settings file
SETTINGS_FILE = _base / "settings.json"
