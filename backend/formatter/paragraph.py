"""
段落规则 — 行距、缩进、间距、对齐
"""

from docx.shared import Pt, Mm
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml.ns import qn, nsdecls
from docx.oxml import parse_xml
from .data_model import ParagraphConfig


ALIGNMENT = {
    "left": WD_ALIGN_PARAGRAPH.LEFT,
    "center": WD_ALIGN_PARAGRAPH.CENTER,
    "right": WD_ALIGN_PARAGRAPH.RIGHT,
    "justify": WD_ALIGN_PARAGRAPH.JUSTIFY,
}


def _indent_to_mm(value: float, unit: str, font_size_pt: float = 12.0) -> float:
    if unit in ("字符", "ch"):
        return value * font_size_pt * 0.37
    return value


def _space_to_pt(value: float, unit: str, font_size_pt: float = 12.0) -> float:
    if unit == "行":
        return value * font_size_pt * 1.2
    return value


def apply_first_line_indent(paragraph, value: float, unit: str,
                            font_size_pt: float = 12.0) -> None:
    """首行缩进 — 字符单位写入 XML firstLineChars"""
    pPr = paragraph._element.get_or_add_pPr()
    ind = pPr.find(qn("w:ind"))
    if ind is None:
        ind = parse_xml(f'<w:ind {nsdecls("w")} />')
        pPr.append(ind)

    for attr in ("w:firstLine", "w:firstLineChars"):
        if attr in ind.attrib:
            del ind.attrib[attr]

    if unit in ("字符", "ch"):
        val = max(0, int(round(value * 100)))
        ind.set(qn("w:firstLineChars"), str(val))
        paragraph.paragraph_format.first_line_indent = Pt(0)
    else:
        mm = _indent_to_mm(value, unit, font_size_pt)
        if mm > 0:
            paragraph.paragraph_format.first_line_indent = Mm(mm)
        else:
            paragraph.paragraph_format.first_line_indent = Pt(0)
            ind.set(qn("w:firstLine"), "0")


def apply_paragraph_format(paragraph, config: ParagraphConfig,
                           font_size_pt: float = 12.0) -> None:
    """将段落格式应用到单个段落"""
    pf = paragraph.paragraph_format

    if config.line_spacing_mode == "multiple":
        pf.line_spacing = config.line_spacing_value
    elif config.line_spacing_mode == "fixed":
        pf.line_spacing = Pt(config.line_spacing_value)
        pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    elif config.line_spacing_mode == "at_least":
        pf.line_spacing = Pt(config.line_spacing_value)
        pf.line_spacing_rule = WD_LINE_SPACING.AT_LEAST

    apply_first_line_indent(paragraph, config.first_line_indent,
                            config.first_line_indent_unit, font_size_pt)

    left_mm = _indent_to_mm(config.left_indent, config.left_indent_unit, font_size_pt)
    if left_mm > 0:
        pf.left_indent = Mm(left_mm)
    right_mm = _indent_to_mm(config.right_indent, config.right_indent_unit, font_size_pt)
    if right_mm > 0:
        pf.right_indent = Mm(right_mm)

    pf.alignment = ALIGNMENT.get(config.alignment, WD_ALIGN_PARAGRAPH.JUSTIFY)
    pf.space_before = Pt(_space_to_pt(config.space_before, config.space_before_unit, font_size_pt))
    pf.space_after = Pt(_space_to_pt(config.space_after, config.space_after_unit, font_size_pt))
