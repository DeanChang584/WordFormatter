"""
标题规则 — 1~6 级标题独立配置，通过修改 Word Heading 样式实现
"""

from .data_model import HeadingStyleConfig, ParagraphConfig
from .font import apply_style_font
from .paragraph import apply_paragraph_format


HEADING_NAMES = {
    1: "Heading 1", 2: "Heading 2", 3: "Heading 3",
    4: "Heading 4", 5: "Heading 5", 6: "Heading 6",
}


def apply_heading_style(doc, config: HeadingStyleConfig, styles_dict: dict[int, str]) -> None:
    """将单级标题配置应用到文档样式"""
    style_name = styles_dict.get(config.level)
    if not style_name or style_name not in [s.name for s in doc.styles]:
        return

    style = doc.styles[style_name]
    apply_style_font(style, config.font_cn, config.font_en, config.font_size,
                     config.font_color, config.font_bold, config.font_italic)

    # 构建临时 ParagraphConfig 以复用 apply_paragraph_format
    para = ParagraphConfig()
    para.alignment = config.alignment
    para.space_before = config.space_before
    para.space_before_unit = config.space_before_unit
    para.space_after = config.space_after
    para.space_after_unit = config.space_after_unit
    para.line_spacing_mode = config.line_spacing_mode
    para.line_spacing_value = config.line_spacing_value
    para.first_line_indent = config.first_line_indent
    para.first_line_indent_unit = config.first_line_indent_unit
    apply_paragraph_format(style, para, config.font_size)
