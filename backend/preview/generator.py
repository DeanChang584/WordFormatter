"""Preview generator — Level 1: parameter summary (Step 4.2).

Reads a ``ProfileConfig`` and produces a human-readable Chinese summary
of all formatting settings, grouped by section.  No file I/O or Word
rendering — pure text generation.

Example output::

    【页面】A4 纵向，上边距 25.4mm，下边距 25.4mm，左边距 31.7mm，右边距 31.7mm，页码：显示
    【正文】宋体 / Times New Roman 小四(12pt)，加粗，两端对齐，行距 1.5 倍，首行缩进 2 字符
    【标题一】黑体 / Arial 三号(16pt)，加粗，左对齐，行距 1.5 倍，段前 12pt，段后 6pt
    【标题二】...
    ...
    【页眉页脚】宋体 / Times New Roman 五号(10.5pt)，居中，页眉距顶 15.0mm，页脚距底 15.0mm
    【图片】宽度 12.0cm，居中，保持纵横比
    【表格】居中，自动分页，表头：黑体 / Arial 五号(10pt)，全部框线
"""

from __future__ import annotations

from typing import Any

from backend.utils.logger import get_logger
from shared.constants import font_size_to_name
from shared.schemas import ProfileConfig

logger = get_logger("backend.preview.generator", category="backend")

# Human-readable maps
_ALIGN_CN = {
    "left": "左对齐",
    "center": "居中",
    "right": "右对齐",
    "justify": "两端对齐",
}
_STYLE_CN = {
    "normal": "",
    "bold": "加粗",
    "italic": "倾斜",
}
_ORIENTATION_CN = {
    "portrait": "纵向",
    "landscape": "横向",
}
_LINE_MODE_CN = {
    "multiple": "倍",
    "fixed": "pt(固定值)",
    "at_least": "pt(最小值)",
}
_BORDER_CN = {
    "none": "无边框",
    "all": "全部框线",
    "horizontal": "仅横向框线",
}
_INDENT_CN = {
    "firstLine": "首行缩进",
    "none": "无缩进",
}


def _fmt_style(style: str) -> str:
    return _STYLE_CN.get(style, style)


def _fmt_align(align: str) -> str:
    return _ALIGN_CN.get(align, align)


def _fmt_size(pt: float) -> str:
    """Return e.g. '小四(12pt)' or '14pt'."""
    name = font_size_to_name(pt)
    if name == str(pt):
        return f"{pt}pt"
    return f"{name}({pt}pt)"


def _fmt_spacing(mode: str, value: float) -> str:
    """Return e.g. '1.5 倍' or '28pt(固定值)'."""
    suffix = _LINE_MODE_CN.get(mode, mode)
    if mode == "multiple":
        return f"{value:g} {suffix}"
    return f"{value:g}{suffix}"


def _fmt_indent(indent_type: str, indent_value: float) -> str:
    if indent_type == "none" or indent_value == 0:
        return "无缩进"
    label = _INDENT_CN.get(indent_type, indent_type)
    return f"{label} {indent_value:g} 字符"


def generate_preview(profile: ProfileConfig) -> str:
    """Generate a human-readable summary of *profile*.

    Returns a multi-line string with sections delimited by ``\\n``.
    """
    sections: list[str] = []

    # --- Page ---
    page = profile.page
    orient = _ORIENTATION_CN.get(page.orientation, page.orientation)
    paper = page.paper_size
    if paper == "custom":
        paper = f"自定义 {page.custom_width}×{page.custom_height}mm"
    pg_num = "显示" if page.page_number else "不显示"
    sections.append(
        f"【页面】{paper} {orient}，"
        f"上边距 {page.margin_top}mm，下边距 {page.margin_bottom}mm，"
        f"左边距 {page.margin_left}mm，右边距 {page.margin_right}mm，"
        f"页码：{pg_num}"
    )

    # --- Body ---
    body = profile.body
    style_parts = []
    s = _fmt_style(body.font_style)
    if s:
        style_parts.append(s)
    style_str = "，".join(style_parts) if style_parts else ""
    indent_str = _fmt_indent(body.indent_type, body.indent_value)
    spacing_str = _fmt_spacing(body.line_spacing_mode, body.line_spacing)
    body_line = (
        f"【正文】{body.font_cn} / {body.font_en} "
        f"{_fmt_size(body.font_size)}"
    )
    if style_str:
        body_line += f"，{style_str}"
    body_line += f"，{_fmt_align(body.alignment)}，行距 {spacing_str}，{indent_str}"
    sections.append(body_line)

    # --- Headings (1–6) ---
    HEADING_NAMES = {1: "一", 2: "二", 3: "三", 4: "四", 5: "五", 6: "六"}
    for level in range(1, 7):
        h = profile.heading.get(str(level))
        if h is None:
            continue
        parts: list[str] = []
        s = _fmt_style(h.font_style)
        if s:
            parts.append(s)
        parts.append(_fmt_align(h.alignment))
        parts.append(f"行距 {_fmt_spacing(h.line_spacing_mode, h.line_spacing)}")
        before_unit = getattr(h, "space_before_unit", "行")
        after_unit = getattr(h, "space_after_unit", "行")
        parts.append(f"段前 {h.space_before:g}{before_unit}，段后 {h.space_after:g}{after_unit}")
        extras = "，".join(parts)
        sections.append(
            f"【标题{HEADING_NAMES[level]}】"
            f"{h.font_cn} / {h.font_en} {_fmt_size(h.font_size)}，{extras}"
        )

    # --- Header / Footer ---
    hf = profile.header_footer
    hf_style = _fmt_style(hf.font_style)
    hf_extra = f"，{hf_style}" if hf_style else ""
    sections.append(
        f"【页眉页脚】{hf.font_cn} / {hf.font_en} "
        f"{_fmt_size(hf.font_size)}{hf_extra}，{_fmt_align(hf.alignment)}，"
        f"页眉距顶 {hf.header_distance}mm，页脚距底 {hf.footer_distance}mm"
    )

    # --- Picture ---
    pic = profile.picture
    ratio = "保持" if pic.keep_ratio else "不保持"
    sections.append(
        f"【图片】宽度 {pic.width:g}{pic.width_unit}，"
        f"{_fmt_align(pic.alignment)}，{ratio}纵横比"
    )

    # --- Table ---
    tbl = profile.table
    align_cn = {"center": "居中", "left": "左对齐", "right": "右对齐"}
    center = align_cn.get(tbl.table_alignment, tbl.table_alignment)
    split = "允许" if tbl.auto_split else "不允许"
    border = _BORDER_CN.get(tbl.border_style, tbl.border_style)
    sections.append(
        f"【表格】{center}，{split}跨页断行，"
        f"表头：{tbl.header_font_cn} / {tbl.header_font_en} "
        f"{_fmt_size(tbl.header_size)}，{border}"
    )

    return "\n".join(sections)
