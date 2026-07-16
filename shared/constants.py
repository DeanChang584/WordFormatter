"""
Word Formatter — Shared constants (Step 0.2 / 1.5)

Centralised definitions used across the formatter engine, API layer,
and (future) frontend code:

  - FONT_SIZE_MAP / FONT_SIZE_NAMES / font_size_to_name  — 中文字号 ↔ pt
  - PAPER_SIZES                                          — 纸张宽高 (mm)
  - ErrorCode                                            — 业务错误码 (API.md §4.1)
  - TaskState                                            — 任务状态枚举 (API.md §9.2)
  - FileProcessingStatus                                 — 文件处理状态枚举 (data model.md §3)
"""

# ============================================================
# 中文字号 <-> 磅值映射
# ============================================================

FONT_SIZE_MAP: dict[str, float] = {
    "初号": 42,
    "小初": 36,
    "一号": 26,
    "小一": 24,
    "二号": 22,
    "小二": 18,
    "三号": 16,
    "小三": 15,
    "四号": 14,
    "小四": 12,
    "五号": 10.5,
    "小五": 9,
}

FONT_SIZE_NAMES: list[str] = list(FONT_SIZE_MAP.keys())


def font_size_to_name(size_pt: float) -> str:
    """Return the Chinese font-size name for a pt value, or the pt as string."""
    for name, pt in FONT_SIZE_MAP.items():
        if abs(pt - size_pt) < 0.01:
            return name
    return str(size_pt)


# ============================================================
# 纸张大小定义 (宽, 高, 单位 mm)
# ============================================================

PAPER_SIZES: dict[str, tuple[float, float]] = {
    "A4": (210, 297),
    "A3": (297, 420),
    "A5": (148, 210),
    "B5": (176, 250),
    "Letter": (215.9, 279.4),
    "Legal": (215.9, 355.6),
}


# ============================================================
# 业务错误码  (API.md §4.1)
# ============================================================


class ErrorCode:
    """Numeric business error codes carried in the ``code`` field of every
    API response envelope.

    ``SUCCESS (0)``  — operation completed normally.
    ``1000–1010``    — defined in API.md §4.1.
    """

    SUCCESS = 0
    UNKNOWN = 1000
    FILE_NOT_FOUND = 1001
    FILE_UNREADABLE = 1002
    TEMPLATE_NOT_FOUND = 1003
    TEMPLATE_CORRUPT = 1004
    CONFIG_ERROR = 1005
    PARAM_ERROR = 1006
    TASK_NOT_FOUND = 1007
    TASK_CANCELLED = 1008
    OUTPUT_DIR_NOT_FOUND = 1009
    FORMAT_FAILED = 1010


# ============================================================
# 任务状态枚举  (API.md §9.2)
# ============================================================


class TaskState:
    """Lifecycle states of a batch-formatting task.

    Transitions:  idle → preparing → running → saving → completed
                                                        ↘ failed
                                    ↘ cancelled (user)
    """

    IDLE = "idle"
    PREPARING = "preparing"
    RUNNING = "running"
    SAVING = "saving"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"

    # Convenience set for validation
    ALL = {IDLE, PREPARING, RUNNING, SAVING, COMPLETED, FAILED, CANCELLED}


# ============================================================
# 文件处理状态枚举  (data model.md §3)
# ============================================================


class FileProcessingStatus:
    """Per-file processing status within a task."""

    WAITING = "waiting"
    RUNNING = "running"
    DONE = "done"
    ERROR = "error"

    ALL = {WAITING, RUNNING, DONE, ERROR}
