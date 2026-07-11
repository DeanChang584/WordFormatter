"""
页面规则 — 纸张大小、方向、边距、页眉页脚边距
"""

from docx.shared import Mm, Cm
from docx.enum.section import WD_ORIENT
from .data_model import PageConfig, PAPER_SIZES


def _to_mm(value: float, unit: str) -> float:
    if unit in ("cm", "厘米"):
        return value / 10.0
    return value


def apply_page_setup(section, config: PageConfig) -> None:
    """将页面配置应用到文档 section"""
    section.top_margin = Mm(_to_mm(config.margin_top, config.margin_top_unit))
    section.bottom_margin = Mm(_to_mm(config.margin_bottom, config.margin_bottom_unit))
    section.left_margin = Mm(_to_mm(config.margin_left, config.margin_left_unit))
    section.right_margin = Mm(_to_mm(config.margin_right, config.margin_right_unit))

    # 页眉页脚边距
    if config.header_margin_unit in ("cm", "厘米"):
        section.header_distance = Cm(config.header_margin / 10.0)
    else:
        section.header_distance = Mm(config.header_margin)
    if config.footer_margin_unit in ("cm", "厘米"):
        section.footer_distance = Cm(config.footer_margin / 10.0)
    else:
        section.footer_distance = Mm(config.footer_margin)

    # 纸张大小
    ps = PAPER_SIZES.get(config.paper_size, PAPER_SIZES["A4"])
    w_mm, h_mm = ps

    if config.text_direction == "横向":
        section.orientation = WD_ORIENT.LANDSCAPE
        section.page_width = Mm(h_mm)
        section.page_height = Mm(w_mm)
    else:
        section.page_width = Mm(w_mm)
        section.page_height = Mm(h_mm)
