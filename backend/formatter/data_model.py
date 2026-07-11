"""
排版配置数据模型
Python 内部使用 snake_case（PEP 8），Phase 1.4 将通过 Pydantic alias_generator
转换为 camelCase JSON 输出（Q1 决议）。
"""

from dataclasses import dataclass, field

# 中文字号映射
FONT_SIZE_MAP = {
    "初号": 42, "小初": 36, "一号": 26, "小一": 24,
    "二号": 22, "小二": 18, "三号": 16, "小三": 15,
    "四号": 14, "小四": 12, "五号": 10.5, "小五": 9,
}
FONT_SIZE_NAMES = list(FONT_SIZE_MAP.keys())


def font_size_to_name(size_pt: float) -> str:
    for name, pt in FONT_SIZE_MAP.items():
        if abs(pt - size_pt) < 0.01:
            return name
    return str(size_pt)


# 纸张大小定义 (宽, 高, 单位 mm)
PAPER_SIZES = {
    "A4": (210, 297),
    "A3": (297, 420),
    "A5": (148, 210),
    "B5": (176, 250),
    "Letter": (215.9, 279.4),
    "Legal": (215.9, 355.6),
}


@dataclass
class PageConfig:
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
    page_number: bool = True


@dataclass
class BodyConfig:
    font_cn: str = "宋体"
    font_en: str = "Times New Roman"
    font_size: float = 12.0
    font_color: str = "#000000"
    font_bold: bool = False
    font_italic: bool = False
    font_underline: bool = False


@dataclass
class ParagraphConfig:
    line_spacing_mode: str = "multiple"
    line_spacing_value: float = 1.5
    special_format: str = "首行"
    indent_value: float = 2.0
    indent_unit: str = "ch"
    first_line_indent: float = 7.4
    first_line_indent_unit: str = "mm"
    left_indent: float = 0.0
    left_indent_unit: str = "字符"
    right_indent: float = 0.0
    right_indent_unit: str = "字符"
    alignment: str = "justify"
    space_before: float = 0.0
    space_before_unit: str = "行"
    space_after: float = 0.0
    space_after_unit: str = "行"


@dataclass
class HeadingStyleConfig:
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


@dataclass
class FormatProfile:
    page: PageConfig = field(default_factory=PageConfig)
    body: BodyConfig = field(default_factory=BodyConfig)
    paragraph: ParagraphConfig = field(default_factory=ParagraphConfig)
    output_dir: str = ""
    headings: dict = field(default_factory=dict)

    def __post_init__(self):
        if not self.headings:
            sizes = {1: 22, 2: 16, 3: 14, 4: 12, 5: 10.5, 6: 10.5}
            for i in range(1, 7):
                h = HeadingStyleConfig(level=i)
                h.font_size = sizes[i]
                self.headings[i] = h
