"""
排版引擎主控 — 编排 docx 格式化、doc→docx 转换、逐文件处理
"""

import os
from pathlib import Path
from typing import Optional, Tuple

try:
    from docx import Document
    HAS_DOCX = True
except ImportError:
    HAS_DOCX = False

try:
    import pythoncom
    import win32com.client
    HAS_COM = True
except ImportError:
    HAS_COM = False

from .data_model import FormatProfile
from .font import apply_run_font, apply_style_font
from .page import apply_page_setup
from .paragraph import apply_paragraph_format
from .heading import apply_heading_style, HEADING_NAMES


def format_docx(filepath: str, profile: FormatProfile,
                output_path: Optional[str] = None) -> Tuple[bool, str]:
    """排版单个 .docx 文件"""
    if output_path is None:
        p = Path(filepath)
        output_path = str(p.parent / f"{p.stem}-R{p.suffix}")

    try:
        doc = Document(filepath)

        # 页面设置
        for section in doc.sections:
            apply_page_setup(section, profile.page)

        # 正文样式（Normal 样式）
        normal = doc.styles["Normal"]
        apply_style_font(normal, profile.body.font_cn, profile.body.font_en,
                         profile.body.font_size, profile.body.font_color,
                         profile.body.font_bold, profile.body.font_italic)
        apply_paragraph_format(normal, profile.paragraph, profile.body.font_size)

        # 标题样式
        heading_names = set(HEADING_NAMES.values())
        for level in range(1, 7):
            hd = profile.headings.get(level)
            if hd is None:
                continue
            apply_heading_style(doc, hd, HEADING_NAMES)

        # 逐段落覆盖（跳过标题段落）
        for para in doc.paragraphs:
            style_name = para.style.name if para.style else ""
            if style_name in heading_names:
                continue
            apply_paragraph_format(para, profile.paragraph, profile.body.font_size)
            for run in para.runs:
                apply_run_font(run, profile.body.font_cn, profile.body.font_en,
                               profile.body.font_size, profile.body.font_color,
                               profile.body.font_bold, profile.body.font_italic)

        doc.save(output_path)
        if not os.path.exists(output_path) or os.path.getsize(output_path) == 0:
            return False, "输出文件写入失败或为空"
        return True, f"排版成功: {os.path.basename(output_path)}"

    except Exception as e:
        return False, f"排版失败 [{os.path.basename(filepath)}]: {e}"


def convert_doc_to_docx(filepath: str) -> Tuple[bool, str, Optional[str]]:
    """将 .doc 转换为 .docx（通过 Word/WPS COM）"""
    if not HAS_COM:
        return False, "缺少 pywin32 库，无法处理 .doc 文件。请安装: pip install pywin32", None

    pythoncom.CoInitialize()
    app = doc = temp_docx = None

    try:
        for pid in ("Word.Application", "WPS.Application",
                     "KWPS.Application", "ET.Application"):
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
        return False, f"转换失败 [{os.path.basename(filepath)}]: {e}", None
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


def process_file(filepath: str, profile: FormatProfile,
                 output_dir: str = "") -> Tuple[bool, str]:
    """处理单个文件 — 根据扩展名分发"""
    ext = Path(filepath).suffix.lower()
    out_parent = Path(output_dir) if output_dir else Path(filepath).parent

    if ext == ".docx":
        out_path = str(out_parent / f"{Path(filepath).stem}-R{ext}")
        return format_docx(filepath, profile, output_path=out_path)

    if ext == ".doc":
        ok, msg, docx_path = convert_doc_to_docx(filepath)
        if not ok or docx_path is None:
            return False, msg

        out_path = str(out_parent / f"{Path(filepath).stem}-R.docx")
        ok2, msg2 = format_docx(docx_path, profile, output_path=out_path)

        try:
            if os.path.exists(docx_path):
                os.remove(docx_path)
        except Exception:
            pass
        return ok2, msg2

    return False, f"不支持的文件格式: {ext}"


def check_dependencies() -> list[str]:
    """返回缺失的依赖列表"""
    missing = []
    if not HAS_DOCX:
        missing.append("python-docx")
    if not HAS_COM:
        missing.append("pywin32")
    return missing
