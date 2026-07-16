"""
标题规则 — 1~6 级标题独立配置，通过修改 Word Heading 样式实现

接受 FormatProfile.headings[level]（data_model.HeadingStyleConfig）。
复用 font.apply_style_font 和 paragraph.ALIGNMENT / apply_heading_paragraph_format。
"""

from .data_model import HeadingStyleConfig
from .font import apply_style_font
from .paragraph import apply_heading_paragraph_format


HEADING_NAMES = {
    1: "Heading 1", 2: "Heading 2", 3: "Heading 3",
    4: "Heading 4", 5: "Heading 5", 6: "Heading 6",
}


def apply_heading_style(doc, config: HeadingStyleConfig) -> None:
    """将单级标题配置应用到文档对应的 Heading 样式"""
    style_name = HEADING_NAMES.get(config.level)
    if not style_name or style_name not in [s.name for s in doc.styles]:
        return

    style = doc.styles[style_name]

    # 字体
    apply_style_font(style, config.font_cn, config.font_en, config.font_size,
                     config.font_color, config.font_bold, config.font_italic)

    # 段落格式（行距、对齐、缩进、间距）
    apply_heading_paragraph_format(style, config)
