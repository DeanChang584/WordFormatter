"""
Shared constants for Word Formatter.

Font-size name/pt mappings and paper-size definitions used across the
backend formatter engine and API layer. Error-code and status enums are
added later in Step 1.5.
"""

# ============================================================
# 中文字号 <-> 磅值映射
# ============================================================

FONT_SIZE_MAP = {
    "初号": 42, "小初": 36, "一号": 26, "小一": 24,
    "二号": 22, "小二": 18, "三号": 16, "小三": 15,
    "四号": 14, "小四": 12, "五号": 10.5, "小五": 9,
}

FONT_SIZE_NAMES = list(FONT_SIZE_MAP.keys())


def font_size_to_name(size_pt: float) -> str:
    """Return the Chinese font-size name for a pt value, or the pt as string."""
    for name, pt in FONT_SIZE_MAP.items():
        if abs(pt - size_pt) < 0.01:
            return name
    return str(size_pt)


# ============================================================
# 纸张大小定义 (宽, 高, 单位 mm)
# ============================================================

PAPER_SIZES = {
    "A4": (210, 297),
    "A3": (297, 420),
    "A5": (148, 210),
    "B5": (176, 250),
    "Letter": (215.9, 279.4),
    "Legal": (215.9, 355.6),
}
