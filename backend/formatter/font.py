"""
字体工具 — 设置中英文字体、字号、颜色、字形
"""

from docx.shared import Pt, RGBColor
from docx.oxml.ns import qn, nsdecls
from docx.oxml import parse_xml


def parse_color(hex_str: str) -> RGBColor:
    """#hex → RGBColor"""
    h = hex_str.lstrip("#")
    return RGBColor(int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


def apply_run_font(run, font_cn: str, font_en: str, font_size_pt: float,
                   color_hex: str, bold: bool, italic: bool):
    """设置单个 run 的字体属性（含中英文分离的 XML 级处理）"""
    run.font.size = Pt(font_size_pt)
    run.font.bold = bold
    run.font.italic = italic
    run.font.color.rgb = parse_color(color_hex)
    run.font.name = font_en

    rPr = run._element.get_or_add_rPr()
    rFonts = rPr.find(qn("w:rFonts"))
    if rFonts is None:
        rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} />')
        rPr.insert(0, rFonts)

    rFonts.set(qn("w:eastAsia"), font_cn)
    rFonts.set(qn("w:ascii"), font_en)
    rFonts.set(qn("w:hAnsi"), font_en)
    rFonts.set(qn("w:cs"), font_en)


def apply_style_font(style, font_cn: str, font_en: str, font_size_pt: float,
                     color_hex: str, bold: bool, italic: bool):
    """设置样式层面的字体属性"""
    font = style.font
    font.size = Pt(font_size_pt)
    font.bold = bold
    font.italic = italic
    font.color.rgb = parse_color(color_hex)
    font.name = font_en

    rPr = style.element.get_or_add_rPr()
    rFonts = rPr.find(qn("w:rFonts"))
    if rFonts is None:
        rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} />')
        rPr.insert(0, rFonts)

    rFonts.set(qn("w:eastAsia"), font_cn)
    rFonts.set(qn("w:ascii"), font_en)
    rFonts.set(qn("w:hAnsi"), font_en)
    rFonts.set(qn("w:cs"), font_en)
