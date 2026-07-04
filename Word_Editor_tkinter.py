#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word 文档排版工具
支持 .doc / .docx 双格式，兼容 MSOffice 和 WPS。
基于 python-docx + win32com + tkinter 实现。
"""

import os, sys, tempfile, threading, traceback, json
from pathlib import Path
from typing import Optional, List, Tuple

import tkinter as tk
from tkinter import ttk, filedialog, messagebox, colorchooser

try:
    from docx import Document
    from docx.shared import Pt, Cm, Mm, Inches, RGBColor, Emu
    from docx.enum.text import WD_ALIGN_PARAGRAPH
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


# ============================================================
# 中文字号映射（从大到小）
# ============================================================
FONT_SIZE_MAP = {
    "初号": 42, "小初": 36, "一号": 26, "小一": 24,
    "二号": 22, "小二": 18, "三号": 16, "小三": 15,
    "四号": 14, "小四": 12, "五号": 10.5, "小五": 9,
}
FONT_SIZE_NAMES = list(FONT_SIZE_MAP.keys())  # 从大到小已排列

# 字号→中文名反向映射（浮点精度处理）
def font_size_to_name(size_pt: float) -> str:
    for name, pt in FONT_SIZE_MAP.items():
        if abs(pt - size_pt) < 0.01:
            return name
    return str(size_pt)


# ============================================================
# 排版配置数据模型
# ============================================================

class PageConfig:
    def __init__(self):
        self.margin_top: float = 25.4
        self.margin_bottom: float = 25.4
        self.margin_left: float = 31.8
        self.margin_right: float = 31.8


class BodyConfig:
    def __init__(self):
        self.font_cn: str = "宋体"
        self.font_en: str = "Times New Roman"
        self.font_size: float = 12.0
        self.font_color: str = "#000000"
        self.font_bold: bool = False
        self.font_italic: bool = False


class ParagraphConfig:
    def __init__(self):
        self.line_spacing_mode: str = "multiple"
        self.line_spacing_value: float = 1.5
        self.first_line_indent: float = 7.4          # 默认7.4mm(≈2字符)
        self.first_line_indent_unit: str = "mm"       # "mm" 或 "字符"
        self.left_indent: float = 0.0
        self.left_indent_unit: str = "字符"
        self.right_indent: float = 0.0
        self.right_indent_unit: str = "字符"
        self.alignment: str = "justify"
        self.space_before: float = 0.0
        self.space_before_unit: str = "行"            # "磅" 或 "行"
        self.space_after: float = 0.0
        self.space_after_unit: str = "行"


class HeadingStyleConfig:
    def __init__(self, level: int):
        self.level: int = level
        self.font_cn: str = "黑体"
        self.font_en: str = "Times New Roman"
        sizes = {1: 22, 2: 16, 3: 14, 4: 12, 5: 10.5, 6: 10.5}
        self.font_size: float = sizes[level]
        self.font_color: str = "#000000"
        self.font_bold: bool = True
        self.font_italic: bool = False
        self.alignment: str = "left"
        self.space_before: float = 1.0                # 默认1行
        self.space_before_unit: str = "行"
        self.space_after: float = 1.0                 # 默认1行
        self.space_after_unit: str = "行"
        self.line_spacing_mode: str = "multiple"
        self.line_spacing_value: float = 1.5
        self.first_line_indent: float = 0.0           # 默认0字符
        self.first_line_indent_unit: str = "字符"


class FormatProfile:
    def __init__(self):
        self.page = PageConfig()
        self.body = BodyConfig()
        self.paragraph = ParagraphConfig()
        self.output_dir: str = ""                      # 自定义输出目录，空=源文件目录
        self.headings: dict[int, HeadingStyleConfig] = {}
        for i in range(1, 7):
            self.headings[i] = HeadingStyleConfig(i)

    def to_dict(self) -> dict:
        return {
            "page": self.page.__dict__.copy(),
            "body": self.body.__dict__.copy(),
            "paragraph": self.paragraph.__dict__.copy(),
            "headings": {str(k): v.__dict__.copy() for k, v in self.headings.items()}
        }

    @classmethod
    def from_dict(cls, d: dict):
        profile = cls()
        if "page" in d:
            for k, v in d["page"].items(): setattr(profile.page, k, v)
        if "body" in d:
            for k, v in d["body"].items(): setattr(profile.body, k, v)
        if "paragraph" in d:
            for k, v in d["paragraph"].items(): setattr(profile.paragraph, k, v)
        if "headings" in d:
            for level_str, hd in d["headings"].items():
                level = int(level_str)
                for k, v in hd.items():
                    if k != "level": setattr(profile.headings[level], k, v)
        return profile


# ============================================================
# 排版引擎
# ============================================================

ALIGNMENT_MAP = {
    "left": WD_ALIGN_PARAGRAPH.LEFT, "center": WD_ALIGN_PARAGRAPH.CENTER,
    "right": WD_ALIGN_PARAGRAPH.RIGHT, "justify": WD_ALIGN_PARAGRAPH.JUSTIFY,
    "distribute": WD_ALIGN_PARAGRAPH.DISTRIBUTE,
}


def parse_color_hex(hex_str: str) -> RGBColor:
    hex_str = hex_str.lstrip("#")
    return RGBColor(int(hex_str[0:2], 16), int(hex_str[2:4], 16), int(hex_str[4:6], 16))


def set_run_font(run, font_cn: str, font_en: str, font_size_pt: float,
                 color_hex: str, bold: bool, italic: bool):
    run.font.size = Pt(font_size_pt)
    run.font.bold = bold; run.font.italic = italic
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
    if left_mm > 0: pf.left_indent = Mm(left_mm)
    right_mm = _convert_indent_to_mm(para_config.right_indent, para_config.right_indent_unit, font_size_pt)
    if right_mm > 0: pf.right_indent = Mm(right_mm)

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
    font.bold = bold; font.italic = italic
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


def format_docx(filepath: str, profile: FormatProfile, progress_callback=None,
                output_path: Optional[str] = None) -> Tuple[bool, str]:
    if output_path is None:
        p = Path(filepath)
        output_path = str(p.parent / f"{p.stem}-Revise{p.suffix}")
    try:
        doc = Document(filepath)
        for section in doc.sections:
            section.top_margin = Mm(profile.page.margin_top)
            section.bottom_margin = Mm(profile.page.margin_bottom)
            section.left_margin = Mm(profile.page.margin_left)
            section.right_margin = Mm(profile.page.margin_right)

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
            try: app = win32com.client.Dispatch(pid); break
            except Exception: continue
        if app is None:
            return False, "未找到可用的 Word 处理器（MS Word 或 WPS 均未安装）", None
        app.Visible = False; app.DisplayAlerts = 0
        doc = app.Documents.Open(os.path.abspath(filepath), ReadOnly=True)
        temp_docx = filepath + ".converted.docx"
        doc.SaveAs2(os.path.abspath(temp_docx), FileFormat=12)
        doc.Close()
        return True, f"转换成功: {os.path.basename(filepath)}", temp_docx
    except Exception as e:
        return False, f"转换失败 [{os.path.basename(filepath)}]: {str(e)}", None
    finally:
        if doc is not None:
            try: doc.Close(SaveChanges=0)
            except Exception: pass
        if app is not None:
            try: app.Quit()
            except Exception: pass
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
        if not ok or docx_path is None: return False, msg
        out_path = str(out_parent / f"{Path(filepath).stem}-Revise.docx")
        ok2, msg2 = format_docx(docx_path, profile, progress_callback, output_path=out_path)
        try:
            if os.path.exists(docx_path): os.remove(docx_path)
        except Exception: pass
        return ok2, msg2
    else:
        return False, f"不支持的文件格式: {ext}"


def batch_process(file_paths, profile, progress_callback=None):
    results = []
    for idx, fp in enumerate(file_paths, 1):
        if progress_callback: progress_callback(f"[{idx}/{len(file_paths)}] 处理: {os.path.basename(fp)}")
        ok, msg = process_file(fp, profile)
        results.append((fp, ok, msg))
    return results


# ============================================================
# Tkinter GUI — 三栏布局
# ============================================================

class WordFormatterApp:
    """主 GUI 应用程序"""

    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Word 文档排版工具")
        self.root.geometry("960x720")
        self.root.minsize(960, 720)
        self.root.resizable(True, True)

        style = ttk.Style()
        # Section 标题
        style.configure('TLabelframe.Label', font=('Segoe UI', 18, 'bold'), foreground='#2E3440')
        # 输入控件现代样式
        style.configure('TEntry', padding=4, fieldbackground='#FFFFFF', bordercolor='#CCCCCC')
        style.configure('TSpinbox', padding=4, fieldbackground='#FFFFFF', bordercolor='#CCCCCC')
        style.configure('TCombobox', padding=4)
        style.configure('TButton', padding=(10, 4))
        # 主按钮 (开始排版)
        style.configure('Action.TButton', font=('Segoe UI', 12, 'bold'),
                        background='#5E81AC', foreground='black', padding=(12, 6))
        style.map('Action.TButton',
                  background=[('active', '#4C6A8E'), ('pressed', '#3B5470'),
                             ('disabled', '#8FAEC8')],
                  foreground=[('active', 'white'), ('disabled', '#E0E0E0')])

        self.profile = FormatProfile()
        self.file_paths: List[str] = []
        self.is_running = False
        self._color_vars: dict = {}

        self._build_ui()
        self._check_dependencies()

    def _check_dependencies(self):
        missing = []
        if not HAS_DOCX: missing.append("python-docx")
        if not HAS_COM: missing.append("pywin32")
        if missing:
            msg = f"缺少依赖库: {', '.join(missing)}\n\n请运行: pip install {' '.join(missing)}"
            messagebox.showwarning("依赖缺失", msg)

    # ========== UI 构建 ==========

    def _build_ui(self):
        title_bar = ttk.Frame(self.root)
        title_bar.pack(fill=tk.X)
        ttk.Label(title_bar, text="Word 文档排版工具",
                  font=("Segoe UI", 18, "bold")).pack(side=tk.LEFT, padx=(20, 0), pady=(10, 6))

        bottom_bar = ttk.Frame(self.root)
        bottom_bar.pack(fill=tk.X, side=tk.BOTTOM)
        self.status_var = tk.StringVar(value="就绪")
        ttk.Label(bottom_bar, textvariable=self.status_var,
                  relief=tk.SUNKEN, anchor=tk.W, padding=(8, 4),
                  font=("Segoe UI", 10)).pack(fill=tk.X)

        self.paned = ttk.PanedWindow(self.root, orient=tk.HORIZONTAL)
        self.paned.pack(fill=tk.BOTH, expand=True, padx=(4, 4), pady=(0, 4))

        # 左栏（不可滚动，固定宽度220px）
        self.left_frame = ttk.Frame(self.paned, width=220)
        self.paned.add(self.left_frame, weight=0)

        # 中栏（不可滚动，固定宽度220px）
        self.mid_frame = ttk.Frame(self.paned, width=220)
        self.paned.add(self.mid_frame, weight=0)

        # 右栏（固定宽度300px，获取剩余空间）
        self.right_frame = ttk.Frame(self.paned, width=300, padding=(0, 0, 12, 0))
        self.paned.add(self.right_frame, weight=1)

        self._build_page_section()
        self._build_paragraph_section()
        self._build_body_section()
        self._build_heading_section()
        self._build_file_section()
        self._build_action_section()

    def _make_section_frame(self, parent, title: str) -> ttk.LabelFrame:
        frame = ttk.LabelFrame(parent, text=title, padding=(16, 10))
        right_pad = 12 if parent is self.right_frame else 4
        frame.pack(fill=tk.X, padx=(12, right_pad), pady=(0, 8))
        return frame

    def _make_labeled_entry(self, parent, label: str, default: str,
                            width: int = 10, unit: str = "",
                            var_type=tk.StringVar, decimals: int = 2) -> tk.StringVar:
        var = var_type()
        row_frame = ttk.Frame(parent)
        row_frame.pack(fill=tk.X, pady=1)
        ttk.Label(row_frame, text=label, font=("Segoe UI", 11), width=10, anchor="w").pack(side=tk.LEFT, padx=(0, 8))
        inc = 0.25 if unit == "行" else 1.0
        fmt = "%.1f" if unit == "mm" else ("%.2f" if unit == "行" else f"%.{decimals}f")
        spin = ttk.Spinbox(row_frame, textvariable=var, width=width,
                           from_=0, to=999, increment=inc, format=fmt)
        spin.pack(side=tk.LEFT)
        var.set(default)
        if unit:
            self._unit_label = ttk.Label(row_frame, text=unit, font=("Segoe UI", 10))
            self._unit_label.pack(side=tk.LEFT, padx=(8, 0))
        return var

    def _make_labeled_combo(self, parent, label: str, values: list,
                            default: str, width: int = 12) -> ttk.Combobox:
        if label:
            # 独立下拉框：创建新行
            row_frame = ttk.Frame(parent)
            row_frame.pack(fill=tk.X, pady=1)
            ttk.Label(row_frame, text=label, font=("Segoe UI", 11), width=10, anchor="w").pack(side=tk.LEFT, padx=(0, 8))
            combo = ttk.Combobox(row_frame, values=values, width=width, state="readonly")
            combo.pack(side=tk.LEFT)
        else:
            # 单位选择器：内联到同一个 row_frame（数值框右侧，间距8px）
            target = parent.winfo_children()[-1]  # 上一个 _make_labeled_entry 创建的 row_frame
            combo = ttk.Combobox(target, values=values, width=width, state="readonly")
            combo.pack(side=tk.LEFT, padx=(8, 0))
        combo.set(default)
        return combo

    def _make_color_button(self, parent, label: str, default: str) -> tk.StringVar:
        row_frame = ttk.Frame(parent)
        row_frame.pack(fill=tk.X, pady=1)
        ttk.Label(row_frame, text=label, font=("Segoe UI", 11), width=10, anchor="e").pack(side=tk.LEFT, padx=(0, 4))
        color_var = tk.StringVar(value=default)
        btn_frame = ttk.Frame(row_frame); btn_frame.pack(side=tk.LEFT)
        cp = tk.Canvas(btn_frame, width=24, height=20, bg=default, highlightthickness=1, highlightbackground="#cccccc")
        cp.pack(side=tk.LEFT, padx=(0, 6))
        def pick():
            c = colorchooser.askcolor(color=color_var.get(), title="选择颜色")
            if c and c[1]: color_var.set(c[1]); cp.configure(bg=c[1])
        cp.bind("<Button-1>", lambda e: pick())
        ttk.Button(btn_frame, text="选择", width=5, command=pick).pack(side=tk.LEFT)
        return color_var

    def _make_checkbox(self, parent, label: str, default: bool) -> tk.BooleanVar:
        var = tk.BooleanVar(value=default)
        ttk.Checkbutton(parent, text=label, variable=var).pack(side=tk.LEFT, padx=(0, 16), pady=2)
        return var

    def _make_inline_row(self, parent) -> ttk.Frame:
        row = ttk.Frame(parent); row.pack(fill=tk.X, pady=1); return row

    # ========== 页面设置 ==========
    def _build_page_section(self):
        frame = self._make_section_frame(self.left_frame, "页面设置 (mm)")
        r1 = self._make_inline_row(frame)
        self._page_margin_top_var = self._make_labeled_entry(r1, "上边距", "25.4", width=10, unit="mm")
        r2 = self._make_inline_row(frame)
        self._page_margin_bottom_var = self._make_labeled_entry(r2, "下边距", "25.4", width=10, unit="mm")
        r3 = self._make_inline_row(frame)
        self._page_margin_left_var = self._make_labeled_entry(r3, "左边距", "31.8", width=10, unit="mm")
        r4 = self._make_inline_row(frame)
        self._page_margin_right_var = self._make_labeled_entry(r4, "右边距", "31.8", width=10, unit="mm")

    # ========== 段落设置 ==========
    def _build_paragraph_section(self):
        frame = self._make_section_frame(self.left_frame, "段落设置")

        r1 = self._make_inline_row(frame)
        self.para_line_mode_var = self._make_labeled_combo(r1, "行距类型",
            ["倍行距", "固定值", "最小值"], "倍行距", width=10)
        self.para_line_value_var = self._make_labeled_entry(r1, "行距值", "1.5", width=8, unit="行")
        # 行距类型切换回调
        def on_line_mode_change(event=None):
            mode = self.para_line_mode_var.get()
            # 需要更新单位标签和默认值——由于标签已创建，这里简化处理：仅更新变量值
            if mode == "固定值":
                self.para_line_value_var.set("28")
            elif mode == "倍行距":
                self.para_line_value_var.set("1.5")
        self.para_line_mode_var.bind("<<ComboboxSelected>>", on_line_mode_change)

        r2 = self._make_inline_row(frame)
        self.para_first_indent_var = self._make_labeled_entry(r2, "首行缩进", "7.4", width=6, unit="mm")
        self.para_first_indent_unit_var = self._make_labeled_combo(r2, "", ["mm", "字符"], "mm", width=5)

        r3 = self._make_inline_row(frame)
        self.para_left_indent_var = self._make_labeled_entry(r3, "左缩进", "0", width=6)
        self.para_left_indent_unit_var = self._make_labeled_combo(r3, "", ["mm", "字符"], "字符", width=5)

        r4 = self._make_inline_row(frame)
        self.para_right_indent_var = self._make_labeled_entry(r4, "右缩进", "0", width=6)
        self.para_right_indent_unit_var = self._make_labeled_combo(r4, "", ["mm", "字符"], "字符", width=5)

        r5 = self._make_inline_row(frame)
        self.para_space_before_var = self._make_labeled_entry(r5, "段前间距", "0.0", width=6)
        self.para_space_before_unit_var = self._make_labeled_combo(r5, "", ["磅", "行"], "行", width=5)

        r6 = self._make_inline_row(frame)
        self.para_space_after_var = self._make_labeled_entry(r6, "段后间距", "0.0", width=6)
        self.para_space_after_unit_var = self._make_labeled_combo(r6, "", ["磅", "行"], "行", width=5)

        r7 = self._make_inline_row(frame)
        self.para_alignment_var = self._make_labeled_combo(r7, "对齐方式",
            ["左对齐", "居中", "右对齐", "两端对齐", "分散对齐"], "两端对齐", width=10)

    # ========== 正文样式 ==========
    def _build_body_section(self):
        frame = self._make_section_frame(self.mid_frame, "正文样式")
        r1 = self._make_inline_row(frame)
        self.body_font_cn_var = self._make_labeled_combo(r1, "中文字体",
            ["宋体", "仿宋", "黑体", "楷体", "微软雅黑", "等线", "华文楷体", "华文宋体"], "宋体", width=14)
        r2 = self._make_inline_row(frame)
        self.body_font_en_var = self._make_labeled_combo(r2, "英文字体",
            ["Times New Roman", "Arial", "Calibri", "Segoe UI", "Georgia", "Courier New"],
            "Times New Roman", width=14)
        r3 = self._make_inline_row(frame)
        self.body_font_size_var = self._make_labeled_combo(r3, "字号", FONT_SIZE_NAMES, "小四", width=6)
        r4 = self._make_inline_row(frame)
        self.body_font_color_var = self._make_color_button(r4, "颜色", "#000000")
        r5 = self._make_inline_row(frame)
        self.body_bold_var = self._make_checkbox(r5, "加粗", False)
        self.body_italic_var = self._make_checkbox(r5, "斜体", False)

    # ========== 标题样式 ==========
    def _build_heading_section(self):
        self.heading_frame = ttk.LabelFrame(self.mid_frame, text="标题样式", padding=(16, 10))
        self.heading_frame.pack(fill=tk.X, padx=(12, 4), pady=(0, 8))

        top_row = ttk.Frame(self.heading_frame); top_row.pack(fill=tk.X)
        ttk.Label(top_row, text="当前:", font=("Segoe UI", 10)).pack(side=tk.LEFT)
        self.heading_level_var = tk.StringVar(value="标题一")
        hlv = ttk.Combobox(top_row, textvariable=self.heading_level_var,
                           values=["标题一","标题二","标题三","标题四","标题五","标题六"],
                           width=8, state="readonly")
        hlv.pack(side=tk.LEFT, padx=(4, 0))
        hlv.bind("<<ComboboxSelected>>", self._on_heading_level_change)

        self.heading_content = ttk.Frame(self.heading_frame)
        hc = self.heading_content

        r1 = self._make_inline_row(hc)
        self.heading_font_cn_var = self._make_labeled_combo(r1, "中文字体",
            ["黑体","宋体","仿宋","楷体","微软雅黑","等线"], "黑体", width=12)

        r2 = self._make_inline_row(hc)
        self.heading_font_en_var = self._make_labeled_combo(r2, "英文字体",
            ["Times New Roman","Arial","Calibri","Segoe UI"], "Times New Roman", width=12)

        r3 = self._make_inline_row(hc)
        self.heading_font_size_var = self._make_labeled_combo(r3, "字号", FONT_SIZE_NAMES, "二号", width=6)

        r4 = self._make_inline_row(hc)
        self.heading_font_color_var = self._make_color_button(r4, "颜色", "#000000")

        r5 = self._make_inline_row(hc)
        self.heading_bold_var = self._make_checkbox(r5, "加粗", True)
        self.heading_italic_var = self._make_checkbox(r5, "斜体", False)

        r6 = self._make_inline_row(hc)
        self.heading_alignment_var = self._make_labeled_combo(r6, "对齐",
            ["左对齐","居中","右对齐","两端对齐"], "左对齐", width=8)

        r7 = self._make_inline_row(hc)
        self.heading_indent_var = self._make_labeled_entry(r7, "首行缩进", "0", width=5)
        self.heading_indent_unit_var = self._make_labeled_combo(r7, "", ["mm","字符"], "字符", width=5)

        r8 = self._make_inline_row(hc)
        self.heading_space_before_var = self._make_labeled_entry(r8, "段前", "1", width=5)
        self.heading_space_before_unit_var = self._make_labeled_combo(r8, "", ["磅","行"], "行", width=5)

        r9 = self._make_inline_row(hc)
        self.heading_space_after_var = self._make_labeled_entry(r9, "段后", "1", width=5)
        self.heading_space_after_unit_var = self._make_labeled_combo(r9, "", ["磅","行"], "行", width=5)

        r10 = self._make_inline_row(hc)
        self.heading_line_spacing_var = self._make_labeled_entry(r10, "行距", "1.5", width=5, unit="行")

        self.heading_content.pack(fill=tk.X, pady=(4, 0))
        self._current_heading_level = 1

    def _on_heading_level_change(self, event=None):
        self._save_ui_to_heading(self._current_heading_level)
        lm = {"标题一":1,"标题二":2,"标题三":3,"标题四":4,"标题五":5,"标题六":6}
        self._current_heading_level = lm.get(self.heading_level_var.get(), 1)
        self._load_heading_to_ui(self._current_heading_level)

    def _load_heading_to_ui(self, level: int):
        hd = self.profile.headings[level]
        self.heading_font_cn_var.set(hd.font_cn)
        self.heading_font_en_var.set(hd.font_en)
        self.heading_font_size_var.set(font_size_to_name(hd.font_size))
        self.heading_font_color_var.set(hd.font_color)
        self.heading_bold_var.set(hd.font_bold)
        self.heading_italic_var.set(hd.font_italic)
        a = {"left":"左对齐","center":"居中","right":"右对齐","justify":"两端对齐","distribute":"分散对齐"}
        self.heading_alignment_var.set(a.get(hd.alignment, "左对齐"))
        self.heading_indent_var.set(str(hd.first_line_indent))
        self.heading_indent_unit_var.set(hd.first_line_indent_unit)
        self.heading_space_before_var.set(str(hd.space_before))
        self.heading_space_before_unit_var.set(hd.space_before_unit)
        self.heading_space_after_var.set(str(hd.space_after))
        self.heading_space_after_unit_var.set(hd.space_after_unit)
        self.heading_line_spacing_var.set(str(hd.line_spacing_value))

    def _save_ui_to_heading(self, level: int):
        hd = self.profile.headings[level]
        hd.font_cn = self.heading_font_cn_var.get()
        hd.font_en = self.heading_font_en_var.get()
        sz_name = self.heading_font_size_var.get()
        hd.font_size = FONT_SIZE_MAP.get(sz_name, 12.0)
        hd.font_color = self.heading_font_color_var.get()
        hd.font_bold = self.heading_bold_var.get()
        hd.font_italic = self.heading_italic_var.get()
        ah = {"左对齐":"left","居中":"center","右对齐":"right","两端对齐":"justify","分散对齐":"distribute"}
        hd.alignment = ah.get(self.heading_alignment_var.get(), "left")
        try: hd.first_line_indent = float(self.heading_indent_var.get())
        except ValueError: pass
        hd.first_line_indent_unit = self.heading_indent_unit_var.get()
        try: hd.space_before = float(self.heading_space_before_var.get())
        except ValueError: pass
        hd.space_before_unit = self.heading_space_before_unit_var.get()
        try: hd.space_after = float(self.heading_space_after_var.get())
        except ValueError: pass
        hd.space_after_unit = self.heading_space_after_unit_var.get()
        try: hd.line_spacing_value = float(self.heading_line_spacing_var.get())
        except ValueError: pass

    # ========== 文件选择 ==========
    def _build_file_section(self):
        frame = self._make_section_frame(self.right_frame, "文件选择")
        btn_line = ttk.Frame(frame); btn_line.pack(fill=tk.X, pady=(0, 6))
        ttk.Button(btn_line, text="📄 选择文件", command=self._select_single_file, width=12).pack(side=tk.LEFT, padx=(0, 6))
        ttk.Button(btn_line, text="📁 选择文件夹", command=self._select_folder, width=12).pack(side=tk.LEFT, padx=(0, 6))
        ttk.Button(btn_line, text="🗑 清空内容", command=self._clear_files, width=12).pack(side=tk.LEFT)

        filter_line = ttk.Frame(frame); filter_line.pack(fill=tk.X, pady=(0, 6))
        ttk.Label(filter_line, text="格式:", font=("Segoe UI", 10)).pack(side=tk.LEFT)
        self.filter_docx_var = tk.BooleanVar(value=True)
        self.filter_doc_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(filter_line, text=".docx", variable=self.filter_docx_var,
                        command=self._refresh_file_list_display).pack(side=tk.LEFT, padx=(6, 12))
        ttk.Checkbutton(filter_line, text=".doc", variable=self.filter_doc_var,
                        command=self._refresh_file_list_display).pack(side=tk.LEFT)

        list_frame = ttk.Frame(frame); list_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        self.file_listbox = tk.Listbox(list_frame, height=20, selectmode=tk.EXTENDED,
                                       font=("Consolas", 10), activestyle="none")
        sb = ttk.Scrollbar(list_frame, orient=tk.VERTICAL, command=self.file_listbox.yview)
        self.file_listbox.configure(yscrollcommand=sb.set)
        self.file_listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        sb.pack(side=tk.RIGHT, fill=tk.Y)

        bot_line = ttk.Frame(frame); bot_line.pack(fill=tk.X)
        self.file_count_var = tk.StringVar(value="已选: 0 个文件")
        ttk.Label(bot_line, textvariable=self.file_count_var, font=("Segoe UI", 10)).pack(side=tk.LEFT)
        ttk.Button(bot_line, text="移除选中", command=self._remove_selected_files, width=10).pack(side=tk.RIGHT)

    # ========== 操作 ==========
    def _build_action_section(self):
        frame = ttk.Frame(self.right_frame); frame.pack(fill=tk.X, padx=(12, 4), pady=(4, 0))
        self.progress_var = tk.DoubleVar(value=0)
        self.progress_bar = ttk.Progressbar(frame, variable=self.progress_var, maximum=100, mode="determinate")
        self.progress_bar.pack(fill=tk.X, pady=(0, 8))
        btn_line = ttk.Frame(frame); btn_line.pack(fill=tk.X)
        self.run_btn = ttk.Button(btn_line, text="▶ 开始排版", command=self._start_formatting, width=10)
        self.run_btn.pack(side=tk.LEFT, padx=(0, 8))
        ttk.Button(btn_line, text="📂 输出目录", command=self._open_output_dir, width=10).pack(side=tk.LEFT, padx=(0, 8))
        ttk.Button(btn_line, text="📋 预览配置", command=self._preview_config, width=10).pack(side=tk.LEFT)

    # ========== 文件操作 ==========
    def _select_single_file(self):
        paths = filedialog.askopenfilenames(title="选择 Word 文档",
                                            filetypes=[("Word 文档","*.docx *.doc"),("所有文件","*.*")])
        if paths:
            new = [p for p in paths if p not in self.file_paths]
            self.file_paths.extend(new); self._refresh_file_list_display()

    def _select_folder(self):
        folder = filedialog.askdirectory(title="选择包含 Word 文档的文件夹")
        if folder:
            exts = []
            if self.filter_docx_var.get(): exts.append(".docx")
            if self.filter_doc_var.get(): exts.append(".doc")
            new_files = []
            for ext in exts:
                for f in Path(folder).rglob(f"*{ext}"):
                    fp = str(f)
                    if fp not in self.file_paths: new_files.append(fp)
            if new_files:
                self.file_paths.extend(new_files); self._refresh_file_list_display()
            else:
                messagebox.showinfo("提示", "所选文件夹中未找到符合条件的 Word 文档")

    def _clear_files(self): self.file_paths.clear(); self._refresh_file_list_display()

    def _remove_selected_files(self):
        sel = self.file_listbox.curselection()
        if not sel: return
        for idx in sorted(sel, reverse=True):
            if idx < len(self.file_paths): del self.file_paths[idx]
        self._refresh_file_list_display()

    def _refresh_file_list_display(self):
        self.file_listbox.delete(0, tk.END)
        exts = []
        if self.filter_docx_var.get(): exts.append(".docx")
        if self.filter_doc_var.get(): exts.append(".doc")
        display = [f for f in self.file_paths if Path(f).suffix.lower() in exts]
        for fp in display: self.file_listbox.insert(tk.END, fp)
        self.file_count_var.set(f"已选: {len(display)} 个文件")

    # ========== 配置操作 ==========
    def _collect_profile_from_ui(self) -> FormatProfile:
        profile = FormatProfile()
        try: profile.page.margin_top = float(self._page_margin_top_var.get())
        except ValueError: pass
        try: profile.page.margin_bottom = float(self._page_margin_bottom_var.get())
        except ValueError: pass
        try: profile.page.margin_left = float(self._page_margin_left_var.get())
        except ValueError: pass
        try: profile.page.margin_right = float(self._page_margin_right_var.get())
        except ValueError: pass

        profile.body.font_cn = self.body_font_cn_var.get()
        profile.body.font_en = self.body_font_en_var.get()
        sz_name = self.body_font_size_var.get()
        profile.body.font_size = FONT_SIZE_MAP.get(sz_name, 12.0)
        profile.body.font_color = self.body_font_color_var.get()
        profile.body.font_bold = self.body_bold_var.get()
        profile.body.font_italic = self.body_italic_var.get()

        lm = {"倍行距":"multiple", "固定值":"fixed", "最小值":"at_least"}
        profile.paragraph.line_spacing_mode = lm.get(self.para_line_mode_var.get(), "multiple")
        try: profile.paragraph.line_spacing_value = float(self.para_line_value_var.get())
        except ValueError: pass
        try: profile.paragraph.first_line_indent = float(self.para_first_indent_var.get())
        except ValueError: pass
        profile.paragraph.first_line_indent_unit = self.para_first_indent_unit_var.get()
        try: profile.paragraph.left_indent = float(self.para_left_indent_var.get())
        except ValueError: pass
        profile.paragraph.left_indent_unit = self.para_left_indent_unit_var.get()
        try: profile.paragraph.right_indent = float(self.para_right_indent_var.get())
        except ValueError: pass
        profile.paragraph.right_indent_unit = self.para_right_indent_unit_var.get()
        try: profile.paragraph.space_before = float(self.para_space_before_var.get())
        except ValueError: pass
        profile.paragraph.space_before_unit = self.para_space_before_unit_var.get()
        try: profile.paragraph.space_after = float(self.para_space_after_var.get())
        except ValueError: pass
        profile.paragraph.space_after_unit = self.para_space_after_unit_var.get()
        am = {"左对齐":"left","居中":"center","右对齐":"right","两端对齐":"justify","分散对齐":"distribute"}
        profile.paragraph.alignment = am.get(self.para_alignment_var.get(), "justify")

        self._save_ui_to_heading(self._current_heading_level)
        return profile

    def _select_output_dir(self):
        folder = filedialog.askdirectory(title="选择排版后文件的输出目录")
        if folder:
            self.profile.output_dir = folder
            self.status_var.set(f"输出目录: {folder}")

    def _open_output_dir(self):
        """打开输出目录，方便用户查看处理结果"""
        if self.profile.output_dir:
            target = self.profile.output_dir
        elif self.file_paths:
            target = str(Path(self.file_paths[0]).parent)
        else:
            messagebox.showinfo("提示", "请先选择 Word 文件，或设置输出路径")
            return
        os.startfile(target)

    def _preview_config(self):
        profile = self._collect_profile_from_ui()
        d = profile.to_dict()
        preview_win = tk.Toplevel(self.root)
        preview_win.title("排版配置预览"); preview_win.geometry("500x500")
        text = tk.Text(preview_win, wrap=tk.WORD, font=("Consolas", 10))
        text.pack(fill=tk.BOTH, expand=True, padx=12, pady=12)
        text.insert(tk.END, json.dumps(d, indent=2, ensure_ascii=False))
        text.configure(state=tk.DISABLED)

    # ========== 排版执行 ==========
    def _start_formatting(self):
        if self.is_running:
            messagebox.showwarning("提示", "排版任务正在运行中"); return
        self.profile = self._collect_profile_from_ui()
        exts = []
        if self.filter_docx_var.get(): exts.append(".docx")
        if self.filter_doc_var.get(): exts.append(".doc")
        files_to_process = [f for f in self.file_paths if Path(f).suffix.lower() in exts]
        if not files_to_process:
            messagebox.showwarning("提示", "请先选择要排版的 Word 文件"); return
        out_info = f"输出目录: {self.profile.output_dir}" if self.profile.output_dir else "输出目录: 源文件所在目录"
        msg = f"即将排版 {len(files_to_process)} 个文件。\n\n排版后的文件将保存为 原文件名-Revise.docx，原文件不作任何修改。\n{out_info}\n确认继续？"
        if not messagebox.askyesno("确认排版", msg): return
        self.is_running = True
        self.run_btn.configure(state=tk.DISABLED, text="排版中...")
        self.progress_var.set(0); self.status_var.set("正在准备...")
        thread = threading.Thread(target=self._run_formatting_thread, args=(files_to_process,), daemon=True)
        thread.start()

    def _run_formatting_thread(self, file_paths):
        total = len(file_paths); results = []
        try:
            for idx, fp in enumerate(file_paths, 1):
                pct = (idx-1)/total*100
                self.root.after(0, lambda p=pct: self.progress_var.set(p))
                self.root.after(0, lambda m=f"[{idx}/{total}] 处理: {os.path.basename(fp)}": self.status_var.set(m))
                ok, msg = process_file(fp, self.profile, output_dir=self.profile.output_dir)
                results.append((fp, ok, msg))
                pct = idx/total*100
                self.root.after(0, lambda p=pct: self.progress_var.set(p))
            ok_count = sum(1 for _, ok, _ in results if ok)
            summary = f"排版完成！成功: {ok_count}, 失败: {total-ok_count}"
            self.root.after(0, lambda: self._on_formatting_done(results, summary))
        except Exception as e:
            self.root.after(0, lambda: self._on_formatting_error(str(e)))

    def _on_formatting_done(self, results, summary):
        self.is_running = False
        self.run_btn.configure(state=tk.NORMAL, text="▶ 开始排版")
        self.status_var.set(summary)
        result_win = tk.Toplevel(self.root)
        result_win.title("排版结果"); result_win.geometry("600x400")
        ttk.Label(result_win, text=summary, font=("Segoe UI", 12, "bold")).pack(pady=(12,8))
        tf = ttk.Frame(result_win); tf.pack(fill=tk.BOTH, expand=True, padx=12, pady=(0,12))
        rt = tk.Text(tf, wrap=tk.WORD, font=("Consolas", 10))
        sb = ttk.Scrollbar(tf, orient=tk.VERTICAL, command=rt.yview)
        rt.configure(yscrollcommand=sb.set)
        rt.pack(side=tk.LEFT, fill=tk.BOTH, expand=True); sb.pack(side=tk.RIGHT, fill=tk.Y)
        for fp, ok, msg in results:
            st = "✓" if ok else "✗"
            rt.insert(tk.END, f"[{st}] {msg}\n")
            if not ok: rt.insert(tk.END, f"     文件: {fp}\n")
        rt.configure(state=tk.DISABLED)

    def _on_formatting_error(self, err_msg):
        self.is_running = False
        self.run_btn.configure(state=tk.NORMAL, text="▶ 开始排版")
        self.status_var.set("排版出错")
        messagebox.showerror("错误", f"排版过程中发生错误:\n{err_msg}")


# ============================================================
# 入口
# ============================================================

def main():
    root = tk.Tk()
    try: ttk.Style().theme_use("vista")
    except Exception: pass
    sw = root.winfo_screenwidth(); sh = root.winfo_screenheight()
    x = (sw-960)//2; y = (sh-720)//2
    root.geometry(f"960x720+{x}+{y}")
    app = WordFormatterApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()