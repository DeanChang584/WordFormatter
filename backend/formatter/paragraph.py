"""
段落规则 — 行距（倍数/固定值/最小值）、缩进、间距、对齐

Step 2.4 完整实现。
接受 FormatProfile.body（data_model.BodyConfig）。
heading.py 复用本模块的 ALIGNMENT 字典和 _apply_line_spacing。
"""

from docx.shared import Pt, Mm
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml.ns import qn, nsdecls
from docx.oxml import parse_xml, OxmlElement
from .data_model import BodyConfig, HeadingStyleConfig


ALIGNMENT = {
    "left": WD_ALIGN_PARAGRAPH.LEFT,
    "center": WD_ALIGN_PARAGRAPH.CENTER,
    "right": WD_ALIGN_PARAGRAPH.RIGHT,
    "justify": WD_ALIGN_PARAGRAPH.JUSTIFY,
}

_LINE_SPACING_RULE = {
    "multiple": None,  # python-docx default: assign float → multiple
    "fixed": WD_LINE_SPACING.EXACTLY,
    "at_least": WD_LINE_SPACING.AT_LEAST,
}


def _space_to_twips(value: float, unit: str, font_size_pt: float) -> int:
    """间距单位 → twips

    行单位: 1行 = font_size_pt × 1.2pt = font_size_pt × 24 twips
    pt单位: 1pt = 20 twips
    """
    if unit in ("行", "line"):
        return int(round(value * font_size_pt * 24))
    else:  # pt / 磅
        return int(round(value * 20))


def _write_spacing_xml(paragraph, space_before_twips: int,
                       space_after_twips: int,
                       before_unit: str = "pt",
                       before_value: float = 0,
                       after_unit: str = "pt",
                       after_value: float = 0) -> None:
    """写入 w:spacing 元素。

    w:before/w:after 总是以 twips 写入保证渲染精度。
    若单位为"行"，同时写入 w:beforeLines/w:afterLines（1/100 行），
    使 Word/WPS 对话框显示为"XX 行"而非"XX 磅"。
    """
    pPr = paragraph._element.get_or_add_pPr()
    spacing = pPr.find(qn('w:spacing'))
    if spacing is None:
        spacing = OxmlElement('w:spacing')
        pPr.append(spacing)

    spacing.set(qn('w:before'), str(space_before_twips))
    spacing.set(qn('w:after'), str(space_after_twips))

    # 清除旧值
    for attr in (qn('w:beforeLines'), qn('w:afterLines'),
                 qn('w:beforeAutospacing'), qn('w:afterAutospacing')):
        if attr in spacing.attrib:
            del spacing.attrib[attr]

    # 若单位是"行"，额外写入 beforeLines/afterLines 供 Word/WPS 显示
    if before_unit in ("行", "line"):
        spacing.set(qn('w:beforeLines'), str(int(round(before_value * 100))))
    if after_unit in ("行", "line"):
        spacing.set(qn('w:afterLines'), str(int(round(after_value * 100))))


def _apply_line_spacing(paragraph_format, value: float,
                        mode: str = "multiple") -> None:
    """统一行距设置，支持 multiple / fixed / at_least 三种模式。

    - multiple: value 为倍数（1.0 / 1.5 / 2.0 等），直接赋值
    - fixed: value 为 pt，设为 EXACTLY 模式
    - at_least: value 为 pt，设为 AT_LEAST 模式
    """
    rule = _LINE_SPACING_RULE.get(mode)
    if rule is None:
        # multiple mode — python-docx accepts float directly
        paragraph_format.line_spacing = value
    else:
        # fixed / at_least — value is pt
        paragraph_format.line_spacing = Pt(value)
        paragraph_format.line_spacing_rule = rule


def apply_first_line_indent(paragraph, value: float, unit: str,
                            font_size_pt: float = 12.0) -> None:
    """首行缩进 — 同时写入 w:firstLine（twips，WPS 渲染用）和 w:firstLineChars（1/100字符，Word UI 显示用）。

    仅有 w:firstLine（twips）时，Word/WPS 对话框只能显示为"0.85厘米"，
    无法识别"2字符"。同时写入 w:firstLineChars（=value×100）后，
    Word/WPS 能正确显示为"2字符"。

    支持单位: 字符 / cm / pt
    """
    pPr = paragraph._element.get_or_add_pPr()
    ind = pPr.find(qn("w:ind"))
    if ind is None:
        ind = parse_xml(f'<w:ind {nsdecls("w")} />')
        pPr.append(ind)

    # 清除旧值（必须使用 qn() 匹配命名空间限定键名）
    for attr in (qn("w:firstLine"), qn("w:firstLineChars"),
                 qn("w:hanging"), qn("w:hangingChars")):
        if attr in ind.attrib:
            del ind.attrib[attr]

    if unit in ("字符", "ch"):
        # twips = pt × 20 — w:firstLine 控制实际渲染，兼容 WPS
        twips = max(0, int(round(value * font_size_pt * 20)))
        ind.set(qn("w:firstLine"), str(twips))
        # firstLineChars = value × 100 — 供 Word UI 显示正确的字符数
        ind.set(qn("w:firstLineChars"), str(int(round(value * 100))))
    elif unit in ("pt", "磅"):
        # pt/磅 → twips (1pt = 20twips)
        twips = max(0, int(round(value * 20)))
        ind.set(qn("w:firstLine"), str(twips))
        ind.set(qn("w:firstLineChars"), "0")
    elif unit in ("mm", "毫米"):
        paragraph.paragraph_format.first_line_indent = Mm(value)
        ind.set(qn("w:firstLineChars"), "0")
    elif unit in ("cm", "厘米"):
        paragraph.paragraph_format.first_line_indent = Mm(value * 10)
        ind.set(qn("w:firstLineChars"), "0")
    else:
        paragraph.paragraph_format.first_line_indent = Pt(0)
        ind.set(qn("w:firstLine"), "0")
        ind.set(qn("w:firstLineChars"), "0")


def apply_hanging_indent(paragraph, value: float, unit: str,
                         font_size_pt: float = 12.0) -> None:
    """悬挂缩进 — 同时写入 w:hanging（twips，WPS 渲染用）和 w:hangingChars（1/100字符，Word UI 显示用）。

    与首行缩进类似，但写入 ``w:hanging`` 属性而非 ``w:firstLine``。

    支持单位: 字符 / cm / pt
    """
    pPr = paragraph._element.get_or_add_pPr()
    ind = pPr.find(qn("w:ind"))
    if ind is None:
        ind = parse_xml(f'<w:ind {nsdecls("w")} />')
        pPr.append(ind)

    # 清除旧值（必须使用 qn() 匹配命名空间限定键名）
    for attr in (qn("w:firstLine"), qn("w:firstLineChars"),
                 qn("w:hanging"), qn("w:hangingChars")):
        if attr in ind.attrib:
            del ind.attrib[attr]

    if unit in ("字符", "ch"):
        twips = max(0, int(round(value * font_size_pt * 20)))
        ind.set(qn("w:hanging"), str(twips))
        # hangingChars = value × 100 — 供 Word UI 显示正确的字符数
        ind.set(qn("w:hangingChars"), str(int(round(value * 100))))
    elif unit in ("pt", "磅"):
        twips = max(0, int(round(value * 20)))
        ind.set(qn("w:hanging"), str(twips))
        ind.set(qn("w:hangingChars"), "0")
    elif unit in ("mm", "毫米"):
        paragraph.paragraph_format.first_line_indent = Mm(-value)
        ind.set(qn("w:hangingChars"), "0")
    elif unit in ("cm", "厘米"):
        paragraph.paragraph_format.first_line_indent = Mm(-value * 10)
        ind.set(qn("w:hangingChars"), "0")
    else:
        ind.set(qn("w:hanging"), "0")
        ind.set(qn("w:hangingChars"), "0")


def _clear_paragraph_indent(paragraph) -> None:
    """显式写入零值缩进，清除从 Normal 样式继承的缩进。"""
    pPr = paragraph._element.get_or_add_pPr()
    ind = pPr.find(qn("w:ind"))
    if ind is not None:
        pPr.remove(ind)
    ind = parse_xml(f'<w:ind {nsdecls("w")} w:firstLine="0" w:firstLineChars="0" w:hanging="0" w:hangingChars="0" w:left="0" w:right="0"/>')
    pPr.append(ind)


def apply_paragraph_format(paragraph, config: BodyConfig,
                          skip_line_spacing: bool = False) -> None:
    """将正文段落格式应用到单个段落或样式。"""
    pf = paragraph.paragraph_format

    # 行距（文档网格启用时跳过，让 docGrid 接管）
    if not skip_line_spacing:
        _apply_line_spacing(pf, config.line_spacing, config.line_spacing_mode)

    # 缩进（首行/悬挂/无缩进 — 无缩进时显式清零，避免继承泄露）
    if config.indent_type in ("字符", "ch", "firstLine"):
        apply_first_line_indent(paragraph, config.indent_value,
                                config.indent_unit, config.font_size)
    elif config.indent_type == "hanging":
        apply_hanging_indent(paragraph, config.indent_value,
                             config.indent_unit, config.font_size)
    else:
        _clear_paragraph_indent(paragraph)

    # 对齐
    pf.alignment = ALIGNMENT.get(config.alignment, WD_ALIGN_PARAGRAPH.JUSTIFY)

    # 段间距 — 直接写入 XML w:before / w:after（twips）
    # 不再使用 python-docx paragraph_format.space_before/after，
    # 因为 python-docx 的 API 在 Word 中存在兼容性问题。
    before_twips = _space_to_twips(config.space_before,
                                   config.space_before_unit, config.font_size)
    after_twips = _space_to_twips(config.space_after,
                                  config.space_after_unit, config.font_size)
    _write_spacing_xml(paragraph, before_twips, after_twips,
                       before_unit=config.space_before_unit,
                       before_value=config.space_before,
                       after_unit=config.space_after_unit,
                       after_value=config.space_after)


def apply_heading_paragraph_format(paragraph, config: HeadingStyleConfig) -> None:
    """将标题段落格式应用到样式（独立于正文段落格式）。

    段间距直接写入 XML w:before / w:after（twips），
    避免使用 w:beforeLines / w:afterLines 在 Word 样式级别不生效的问题。
    """
    pf = paragraph.paragraph_format

    # 行距（multiple / fixed / at_least）
    _apply_line_spacing(pf, config.line_spacing, config.line_spacing_mode)

    # 缩进（首行/悬挂/无缩进 — 无缩进时显式清零，避免继承泄露）
    if config.indent_type in ("字符", "ch", "firstLine"):
        apply_first_line_indent(paragraph, config.indent_value,
                                config.indent_unit, config.font_size)
    elif config.indent_type == "hanging":
        apply_hanging_indent(paragraph, config.indent_value,
                             config.indent_unit, config.font_size)
    else:
        _clear_paragraph_indent(paragraph)

    # 对齐
    pf.alignment = ALIGNMENT.get(config.alignment, WD_ALIGN_PARAGRAPH.JUSTIFY)

    # 段间距 — 直接写入 XML w:before / w:after（twips）
    # 不再使用 w:beforeLines / w:afterLines，因为 Word 样式级别的
    # beforeLines/afterLines 经常被忽略，导致段间距不生效。
    before_twips = _space_to_twips(config.space_before,
                                   config.space_before_unit, config.font_size)
    after_twips = _space_to_twips(config.space_after,
                                  config.space_after_unit, config.font_size)
    _write_spacing_xml(paragraph, before_twips, after_twips,
                       before_unit=config.space_before_unit,
                       before_value=config.space_before,
                       after_unit=config.space_after_unit,
                       after_value=config.space_after)