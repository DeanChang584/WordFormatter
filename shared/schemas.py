"""
DTO（数据传输对象）— 前后端通信 JSON 契约
定义所有 API 端点的请求/响应数据结构。
基于 models.py 的 FormatProfile，添加 API 特有的字段。
"""

from dataclasses import dataclass, field, asdict
from typing import Optional


# ============================================================
# 页面配置
# ============================================================

@dataclass
class PageDTO:
    margin_top: float = 25.4
    margin_bottom: float = 25.4
    margin_left: float = 31.8
    margin_right: float = 31.8
    text_direction: str = "纵向"
    paper_size: str = "A4"
    margin_top_unit: str = "mm"
    margin_bottom_unit: str = "mm"
    margin_left_unit: str = "mm"
    margin_right_unit: str = "mm"
    header_margin: float = 15.0
    header_margin_unit: str = "mm"
    footer_margin: float = 17.5
    footer_margin_unit: str = "mm"
    section_mode: str = "全文排版"


# ============================================================
# 正文样式
# ============================================================

@dataclass
class BodyDTO:
    font_cn: str = "宋体"
    font_en: str = "Times New Roman"
    font_size: float = 12.0
    font_color: str = "#000000"
    font_bold: bool = False
    font_italic: bool = False


# ============================================================
# 段落格式
# ============================================================

@dataclass
class ParagraphDTO:
    line_spacing_mode: str = "multiple"
    line_spacing_value: float = 1.5
    special_format: str = "首行"
    indent_value: float = 2.0
    indent_unit: str = "ch"
    first_line_indent: float = 7.4
    first_line_indent_unit: str = "mm"
    alignment: str = "justify"
    space_before: float = 0.0
    space_before_unit: str = "行"
    space_after: float = 0.0
    space_after_unit: str = "行"


# ============================================================
# 标题样式
# ============================================================

@dataclass
class HeadingDTO:
    level: int = 1
    font_cn: str = "黑体"
    font_en: str = "Times New Roman"
    font_size: float = 22.0
    font_color: str = "#000000"
    font_bold: bool = True
    font_italic: bool = False
    alignment: str = "left"
    special_format: str = "首行"
    indent_value: float = 0.0
    indent_unit: str = "ch"
    space_before: float = 1.0
    space_before_unit: str = "行"
    space_after: float = 1.0
    space_after_unit: str = "行"
    line_spacing_mode: str = "multiple"
    line_spacing_value: float = 1.5
    first_line_indent: float = 0.0
    first_line_indent_unit: str = "字符"


# ============================================================
# 排版配置（完整 DTO）
# ============================================================

@dataclass
class ProfileDTO:
    page: PageDTO = field(default_factory=PageDTO)
    body: BodyDTO = field(default_factory=BodyDTO)
    paragraph: ParagraphDTO = field(default_factory=ParagraphDTO)
    headings: dict[str, HeadingDTO] = field(default_factory=lambda: {
        str(i): HeadingDTO(level=i) for i in range(1, 7)
    })
    output_dir: str = ""


# ============================================================
# 文件管理 DTO
# ============================================================

@dataclass
class FileSelectRequest:
    paths: list[str] = field(default_factory=list)


@dataclass
class FolderRequest:
    folder: str = ""


@dataclass
class FileDeleteRequest:
    paths: list[str] = field(default_factory=list)


@dataclass
class FilesResponse:
    files: list[str] = field(default_factory=list)
    count: int = 0
    added: list[str] = field(default_factory=list)


# ============================================================
# 排版任务 DTO
# ============================================================

@dataclass
class FormatStartRequest:
    files: list[str] = field(default_factory=list)
    profile: Optional[ProfileDTO] = None
    output_dir: str = ""


@dataclass
class FormatStartResponse:
    task_id: str = ""


@dataclass
class FormatProgressResponse:
    task_id: str = ""
    current: int = 0
    total: int = 0
    status: str = "idle"


@dataclass
class FormatResultItem:
    file_path: str = ""
    success: bool = False
    message: str = ""


@dataclass
class FormatResultResponse:
    task_id: str = ""
    results: list[FormatResultItem] = field(default_factory=list)
    ok_count: int = 0
    fail_count: int = 0


# ============================================================
# 主题 DTO
# ============================================================

@dataclass
class ThemeResponse:
    mode: str = "system"


@dataclass
class ThemeRequest:
    mode: str = "system"


# ============================================================
# 通用响应
# ============================================================

@dataclass
class HealthResponse:
    status: str = "ok"


@dataclass
class OkResponse:
    ok: bool = True
    detail: str = ""