"""
/profile 端点 — 排版配置 CRUD
提供当前排版配置的读取和更新。
"""

from fastapi import APIRouter
from backend.formatter.data_model import FormatProfile, FONT_SIZE_MAP
from shared.schemas import ProfileDTO, PageDTO, BodyDTO, ParagraphDTO, HeadingDTO, OkResponse

router = APIRouter()

# 全局配置实例（内存中，重启重置）
_profile = FormatProfile()


def _profile_to_dto(p: FormatProfile) -> ProfileDTO:
    """models.FormatProfile → shared.schemas.ProfileDTO (explicit mapping)"""
    page = PageDTO(
        margin_top=p.page.margin_top,
        margin_bottom=p.page.margin_bottom,
        margin_left=p.page.margin_left,
        margin_right=p.page.margin_right,
        text_direction=p.page.text_direction,
        paper_size=p.page.paper_size,
        margin_top_unit=p.page.margin_top_unit,
        margin_bottom_unit=p.page.margin_bottom_unit,
        margin_left_unit=p.page.margin_left_unit,
        margin_right_unit=p.page.margin_right_unit,
        header_margin=p.page.header_margin,
        header_margin_unit=p.page.header_margin_unit,
        footer_margin=p.page.footer_margin,
        footer_margin_unit=p.page.footer_margin_unit,
        section_mode=p.page.section_mode,
    )
    body = BodyDTO(
        font_cn=p.body.font_cn,
        font_en=p.body.font_en,
        font_size=p.body.font_size,
        font_color=p.body.font_color,
        font_bold=p.body.font_bold,
        font_italic=p.body.font_italic,
    )
    para = ParagraphDTO(
        line_spacing_mode=p.paragraph.line_spacing_mode,
        line_spacing_value=p.paragraph.line_spacing_value,
        special_format=p.paragraph.special_format,
        indent_value=p.paragraph.indent_value,
        indent_unit=p.paragraph.indent_unit,
        first_line_indent=p.paragraph.first_line_indent,
        first_line_indent_unit=p.paragraph.first_line_indent_unit,
        alignment=p.paragraph.alignment,
        space_before=p.paragraph.space_before,
        space_before_unit=p.paragraph.space_before_unit,
        space_after=p.paragraph.space_after,
        space_after_unit=p.paragraph.space_after_unit,
    )
    headings = {}
    for level, hd in p.headings.items():
        headings[str(level)] = HeadingDTO(
            level=hd.level,
            font_cn=hd.font_cn,
            font_en=hd.font_en,
            font_size=hd.font_size,
            font_color=hd.font_color,
            font_bold=hd.font_bold,
            font_italic=hd.font_italic,
            alignment=hd.alignment,
            special_format=hd.special_format,
            indent_value=hd.indent_value,
            indent_unit=hd.indent_unit,
            space_before=hd.space_before,
            space_before_unit=hd.space_before_unit,
            space_after=hd.space_after,
            space_after_unit=hd.space_after_unit,
            line_spacing_mode=hd.line_spacing_mode,
            line_spacing_value=hd.line_spacing_value,
            first_line_indent=hd.first_line_indent,
            first_line_indent_unit=hd.first_line_indent_unit,
        )
    return ProfileDTO(page=page, body=body, paragraph=para, headings=headings, output_dir=p.output_dir)


def _dto_to_profile(d: ProfileDTO) -> FormatProfile:
    """shared.schemas.ProfileDTO → models.FormatProfile (explicit mapping)"""
    p = FormatProfile()

    # Page
    pg = d.page
    p.page.margin_top = pg.margin_top
    p.page.margin_bottom = pg.margin_bottom
    p.page.margin_left = pg.margin_left
    p.page.margin_right = pg.margin_right
    p.page.text_direction = pg.text_direction
    p.page.paper_size = pg.paper_size
    p.page.margin_top_unit = pg.margin_top_unit
    p.page.margin_bottom_unit = pg.margin_bottom_unit
    p.page.margin_left_unit = pg.margin_left_unit
    p.page.margin_right_unit = pg.margin_right_unit
    p.page.header_margin = pg.header_margin
    p.page.header_margin_unit = pg.header_margin_unit
    p.page.footer_margin = pg.footer_margin
    p.page.footer_margin_unit = pg.footer_margin_unit
    p.page.section_mode = pg.section_mode

    # Body
    bd = d.body
    p.body.font_cn = bd.font_cn
    p.body.font_en = bd.font_en
    p.body.font_size = bd.font_size
    p.body.font_color = bd.font_color
    p.body.font_bold = bd.font_bold
    p.body.font_italic = bd.font_italic

    # Paragraph
    pa = d.paragraph
    p.paragraph.line_spacing_mode = pa.line_spacing_mode
    p.paragraph.line_spacing_value = pa.line_spacing_value
    p.paragraph.special_format = pa.special_format
    p.paragraph.indent_value = pa.indent_value
    p.paragraph.indent_unit = pa.indent_unit
    p.paragraph.first_line_indent = pa.first_line_indent
    p.paragraph.first_line_indent_unit = pa.first_line_indent_unit
    p.paragraph.alignment = pa.alignment
    p.paragraph.space_before = pa.space_before
    p.paragraph.space_before_unit = pa.space_before_unit
    p.paragraph.space_after = pa.space_after
    p.paragraph.space_after_unit = pa.space_after_unit

    # Headings
    for level_str, hd in d.headings.items():
        level = int(level_str)
        if level in p.headings:
            h = p.headings[level]
            h.font_cn = hd.font_cn
            h.font_en = hd.font_en
            h.font_size = hd.font_size
            h.font_color = hd.font_color
            h.font_bold = hd.font_bold
            h.font_italic = hd.font_italic
            h.alignment = hd.alignment
            h.space_before = hd.space_before
            h.space_before_unit = hd.space_before_unit
            h.space_after = hd.space_after
            h.space_after_unit = hd.space_after_unit
            h.line_spacing_mode = hd.line_spacing_mode
            h.line_spacing_value = hd.line_spacing_value
            h.first_line_indent = hd.first_line_indent
            h.first_line_indent_unit = hd.first_line_indent_unit

    p.output_dir = d.output_dir
    return p


@router.get("/profile", response_model=ProfileDTO)
async def get_profile():
    """获取当前排版配置"""
    return _profile_to_dto(_profile)


@router.put("/profile", response_model=OkResponse)
async def update_profile(dto: ProfileDTO):
    """更新排版配置"""
    global _profile
    _profile = _dto_to_profile(dto)
    return OkResponse(ok=True)


@router.get("/profile/font-sizes")
async def get_font_sizes():
    """获取字号映射表"""
    return {"sizes": FONT_SIZE_MAP, "names": list(FONT_SIZE_MAP.keys())}
