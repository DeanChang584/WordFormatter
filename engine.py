#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word Formatter — 排版引擎
提供 docx 格式化、doc→docx 转换、批量处理等核心功能。
"""

import os, tempfile
from pathlib import Path
from typing import Optional, Tuple, Callable

try:
    from docx import Document
    from docx.shared import Pt, Cm, Mm, Inches, RGBColor, Emu
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.enum.section import WD_ORIENT
    from docx.oxml.ns import qn, nsdecls
    from docx.oxml import parse_xml
    HAS_DOCX = True
except ImportError:
    HAS_DOCX = False

try:
    import pythoncom
    import win32com.client
    HAS_COM = True
except ImportError:
    HAS_COM = False

from models import (
    FormatProfile, ParagraphConfig, HeadingStyleConfig,
    FONT_SIZE_MAP, FONT_SIZE_NAMES, font_size_to_name,
)

# ============================================================
# 排版引擎
# ============================================================

ALIGNMENT_MAP = {
    "left": WD_ALIGN_PARAGRAPH.LEFT,
    "center": WD_ALIGN_PARAGRAPH.CENTER,
    "right": WD_ALIGN_PARAGRAPH.RIGHT,
    "justify": WD_ALIGN_PARAGRAPH.JUSTIFY,
    "distribute": WD_ALIGN_PARAGRAPH.DISTRIBUTE,
}


def parse_color_hex(hex_str: str) -> RGBColor:
    hex_str = hex_str.lstrip("#")
    return RGBColor(int(hex_str[0:2], 16), int(hex_str[2:4], 16), int(hex_str[4:6], 16))


def set_run_font(run, font_cn: str, font_en: str, font_size_pt: float,
                 color_hex: str, bold: bool, italic: bool):
    run.font.size = Pt(font_size_pt)
    run.font.bold = bold
    run.font.italic = italic
    run.font.color.rgb = parse_color_hex(color_hex)
    run.font.name = font_en
    rPr = run._element.get_or_add_rPr()
    rFonts = rPr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} />')
        rPr.insert(0, rFonts)
    rFonts.set(qn('w:eastAsia'), font_cn)
    rFonts.set(qn('w:ascii'), font_en)
    rFonts.set(qn('w:hAnsi'), font_en)
    rFonts.set(qn('w:cs'), font_en)


def _convert_indent_to_mm(value: float, unit: str, font_size_pt: float = 12.0) -> float:
    """将缩进值转换为 mm。字符单位按当前字号换算（1字符≈字号pt*0.37mm）"""
    if unit == "字符":
        return value * font_size_pt * 0.37
    return value


def _convert_space_to_pt(value: float, unit: str, font_size_pt: float = 12.0) -> float:
    """将间距值转换为 pt。行单位按当前字号换算（1行≈字号pt*1.2）"""
    if unit == "行":
        return value * font_size_pt * 1.2
    return value


def set_paragraph_format(paragraph, para_config: ParagraphConfig, font_size_pt: float = 12.0):
    pf = paragraph.paragraph_format
    if para_config.line_spacing_mode == "multiple":
        pf.line_spacing = para_config.line_spacing_value
    elif para_config.line_spacing_mode == "fixed":
        pf.line_spacing = Pt(para_config.line_spacing_value)
        from docx.enum.text import WD_LINE_SPACING
        pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    elif para_config.line_spacing_mode == "at_least":
        pf.line_spacing = Pt(para_config.line_spacing_value)
        from docx.enum.text import WD_LINE_SPACING
        pf.line_spacing_rule = WD_LINE_SPACING.AT_LEAST

    first_mm = _convert_indent_to_mm(para_config.first_line_indent, para_config.first_line_indent_unit, font_size_pt)
    if first_mm > 0:
        pf.first_line_indent = Mm(first_mm)
    else:
        pf.first_line_indent = Pt(0)

    left_mm = _convert_indent_to_mm(para_config.left_indent, para_config.left_indent_unit, font_size_pt)
    if left_mm > 0:
        pf.left_indent = Mm(left_mm)
    right_mm = _convert_indent_to_mm(para_config.right_indent, para_config.right_indent_unit, font_size_pt)
    if right_mm > 0:
        pf.right_indent = Mm(right_mm)

    pf.alignment = ALIGNMENT_MAP.get(para_config.alignment, WD_ALIGN_PARAGRAPH.JUSTIFY)
    pf.space_before = Pt(_convert_space_to_pt(para_config.space_before, para_config.space_before_unit, font_size_pt))
    pf.space_after = Pt(_convert_space_to_pt(para_config.space_after, para_config.space_after_unit, font_size_pt))


def set_style_paragraph_format(style, para_config, font_size_pt: float = 12.0):
    pf = style.paragraph_format
    if para_config.line_spacing_mode == "multiple":
        pf.line_spacing = para_config.line_spacing_value
    elif para_config.line_spacing_mode == "fixed":
        pf.line_spacing = Pt(para_config.line_spacing_value)
        from docx.enum.text import WD_LINE_SPACING
        pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY

    if hasattr(para_config, 'first_line_indent') and para_config.first_line_indent > 0:
        mm_val = _convert_indent_to_mm(para_config.first_line_indent, para_config.first_line_indent_unit, font_size_pt)
        pf.first_line_indent = Mm(mm_val)
    else:
        pf.first_line_indent = Pt(0)

    pf.alignment = ALIGNMENT_MAP.get(para_config.alignment, WD_ALIGN_PARAGRAPH.JUSTIFY)

    sb = _convert_space_to_pt(para_config.space_before, para_config.space_before_unit, font_size_pt)
    sa = _convert_space_to_pt(para_config.space_after, para_config.space_after_unit, font_size_pt)
    pf.space_before = Pt(sb)
    pf.space_after = Pt(sa)


def set_style_font(style, font_cn: str, font_en: str, font_size_pt: float,
                   color_hex: str, bold: bool, italic: bool):
    font = style.font
    font.size = Pt(font_size_pt)
    font.bold = bold
    font.italic = italic
    font.color.rgb = parse_color_hex(color_hex)
    font.name = font_en
    rPr = style.element.get_or_add_rPr()
    rFonts = rPr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} />')
        rPr.insert(0, rFonts)
    rFonts.set(qn('w:eastAsia'), font_cn)
    rFonts.set(qn('w:ascii'), font_en)
    rFonts.set(qn('w:hAnsi'), font_en)
    rFonts.set(qn('w:cs'), font_en)


# ---- 纸张大小定义 (宽, 高, 单位 mm) ----
PAPER_SIZES = {
    "A4": (210, 297),
    "Letter": (215.9, 279.4),
    "A5": (148, 210),
}


def format_docx(filepath: str, profile: FormatProfile, progress_callback=None,
                output_path: Optional[str] = None) -> Tuple[bool, str]:
    if output_path is None:
        p = Path(filepath)
        output_path = str(p.parent / f"{p.stem}-Revise{p.suffix}")
    try:
        doc = Document(filepath)
        def _to_mm(val: float, unit: str) -> float:
            return val / 10.0 if unit in ("cm", "厘米") else val
        for section in doc.sections:
            section.top_margin = Mm(_to_mm(profile.page.margin_top, profile.page.margin_top_unit))
            section.bottom_margin = Mm(_to_mm(profile.page.margin_bottom, profile.page.margin_bottom_unit))
            section.left_margin = Mm(_to_mm(profile.page.margin_left, profile.page.margin_left_unit))
            section.right_margin = Mm(_to_mm(profile.page.margin_right, profile.page.margin_right_unit))

            # 页眉页脚边距
            if profile.page.header_margin_unit in ("cm", "厘米"):
                section.header_distance = Cm(profile.page.header_margin / 10.0)
            else:
                section.header_distance = Mm(profile.page.header_margin)
            if profile.page.footer_margin_unit in ("cm", "厘米"):
                section.footer_distance = Cm(profile.page.footer_margin / 10.0)
            else:
                section.footer_distance = Mm(profile.page.footer_margin)

            # 纸张大小
            ps = PAPER_SIZES.get(profile.page.paper_size, PAPER_SIZES["A4"])
            w_mm, h_mm = ps
            section.page_width = Mm(w_mm)
            section.page_height = Mm(h_mm)

            # 文字方向
            if profile.page.text_direction == "横向":
                section.orientation = WD_ORIENT.LANDSCAPE
                # 横向时交换宽高
                section.page_width = Mm(h_mm)
                section.page_height = Mm(w_mm)

        normal_style = doc.styles['Normal']
        set_style_font(normal_style, profile.body.font_cn, profile.body.font_en,
                       profile.body.font_size, profile.body.font_color,
                       profile.body.font_bold, profile.body.font_italic)
        set_style_paragraph_format(normal_style, profile.paragraph, profile.body.font_size)

        heading_style_names = {1: 'Heading 1', 2: 'Heading 2', 3: 'Heading 3',
                               4: 'Heading 4', 5: 'Heading 5', 6: 'Heading 6'}
        for level, style_name in heading_style_names.items():
            hd = profile.headings[level]
            if style_name in [s.name for s in doc.styles]:
                style = doc.styles[style_name]
                set_style_font(style, hd.font_cn, hd.font_en, hd.font_size,
                               hd.font_color, hd.font_bold, hd.font_italic)
                hd_para = ParagraphConfig()
                hd_para.alignment = hd.alignment
                hd_para.space_before = hd.space_before
                hd_para.space_before_unit = hd.space_before_unit
                hd_para.space_after = hd.space_after
                hd_para.space_after_unit = hd.space_after_unit
                hd_para.line_spacing_mode = hd.line_spacing_mode
                hd_para.line_spacing_value = hd.line_spacing_value
                hd_para.first_line_indent = hd.first_line_indent
                hd_para.first_line_indent_unit = hd.first_line_indent_unit
                set_style_paragraph_format(style, hd_para, hd.font_size)

        heading_styles = set(heading_style_names.values())
        for para in doc.paragraphs:
            style_name = para.style.name if para.style else ""
            if style_name in heading_styles:
                continue
            set_paragraph_format(para, profile.paragraph, profile.body.font_size)
            for run in para.runs:
                set_run_font(run, profile.body.font_cn, profile.body.font_en,
                             profile.body.font_size, profile.body.font_color,
                             profile.body.font_bold, profile.body.font_italic)

        doc.save(output_path)
        if not os.path.exists(output_path) or os.path.getsize(output_path) == 0:
            return False, "输出文件写入失败或为空"
        return True, f"排版成功: {os.path.basename(output_path)}"
    except Exception as e:
        return False, f"排版失败 [{os.path.basename(filepath)}]: {str(e)}"


def convert_doc_to_docx(filepath: str, progress_callback=None) -> Tuple[bool, str, Optional[str]]:
    if not HAS_COM:
        return False, "缺少 pywin32 库，无法处理 .doc 文件。请安装: pip install pywin32", None
    pythoncom.CoInitialize()
    app = doc = temp_docx = None
    try:
        for pid in ["Word.Application", "WPS.Application", "KWPS.Application", "ET.Application"]:
            try:
                app = win32com.client.Dispatch(pid)
                break
            except Exception:
                continue
        if app is None:
            return False, "未找到可用的 Word 处理器（MS Word 或 WPS 均未安装）", None
        app.Visible = False
        app.DisplayAlerts = 0
        doc = app.Documents.Open(os.path.abspath(filepath), ReadOnly=True)
        temp_docx = filepath + ".converted.docx"
        doc.SaveAs2(os.path.abspath(temp_docx), FileFormat=12)
        doc.Close()
        return True, f"转换成功: {os.path.basename(filepath)}", temp_docx
    except Exception as e:
        return False, f"转换失败 [{os.path.basename(filepath)}]: {str(e)}", None
    finally:
        if doc is not None:
            try:
                doc.Close(SaveChanges=0)
            except Exception:
                pass
        if app is not None:
            try:
                app.Quit()
            except Exception:
                pass
        pythoncom.CoUninitialize()


def process_file(filepath: str, profile: FormatProfile, progress_callback=None,
                 output_dir: str = "") -> Tuple[bool, str]:
    ext = Path(filepath).suffix.lower()
    out_parent = Path(output_dir) if output_dir else Path(filepath).parent
    if ext == ".docx":
        out_path = str(out_parent / f"{Path(filepath).stem}-Revise{Path(filepath).suffix}")
        return format_docx(filepath, profile, progress_callback, output_path=out_path)
    elif ext == ".doc":
        ok, msg, docx_path = convert_doc_to_docx(filepath, progress_callback)
        if not ok or docx_path is None:
            return False, msg
        out_path = str(out_parent / f"{Path(filepath).stem}-Revise.docx")
        ok2, msg2 = format_docx(docx_path, profile, progress_callback, output_path=out_path)
        try:
            if os.path.exists(docx_path):
                os.remove(docx_path)
        except Exception:
            pass
        return ok2, msg2
    else:
        return False, f"不支持的文件格式: {ext}"


def batch_process(file_paths, profile: FormatProfile, progress_callback=None):
    results = []
    for idx, fp in enumerate(file_paths, 1):
        if progress_callback:
            progress_callback(f"[{idx}/{len(file_paths)}] 处理: {os.path.basename(fp)}")
        ok, msg = process_file(fp, profile)
        results.append((fp, ok, msg))
    return results


def check_dependencies() -> list:
    """返回缺失的依赖列表"""
    missing = []
    if not HAS_DOCX:
        missing.append("python-docx")
    if not HAS_COM:
        missing.append("pywin32")
    return missing