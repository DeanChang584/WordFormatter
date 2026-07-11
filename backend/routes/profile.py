"""
/profile 端点 — 排版配置 CRUD
复用 models.py 的 FormatProfile，通过 DTO 转换。
"""

import sys
from pathlib import Path
_ROOT = Path(__file__).parent.parent.parent
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from fastapi import APIRouter
from models import FormatProfile, FONT_SIZE_MAP
from shared.schemas import ProfileDTO, PageDTO, BodyDTO, ParagraphDTO, HeadingDTO, OkResponse

router = APIRouter()

# 全局配置实例（内存中，重启重置）
_profile = FormatProfile()


def _profile_to_dto(p: FormatProfile) -> ProfileDTO:
    """models.FormatProfile → shared.schemas.ProfileDTO"""
    dto = ProfileDTO()
    dto.page = PageDTO(**{k: getattr(p.page, k) for k in PageDTO.__dataclass_fields__})
    dto.body = BodyDTO(**{k: getattr(p.body, k) for k in BodyDTO.__dataclass_fields__})
    dto.paragraph = ParagraphDTO(**{k: getattr(p.paragraph, k) for k in ParagraphDTO.__dataclass_fields__})
    dto.headings = {}
    for level, hd in p.headings.items():
        d = {k: getattr(hd, k) for k in HeadingDTO.__dataclass_fields__}
        dto.headings[str(level)] = HeadingDTO(**d)
    dto.output_dir = p.output_dir
    return dto


def _dto_to_profile(d: ProfileDTO) -> FormatProfile:
    """shared.schemas.ProfileDTO → models.FormatProfile"""
    p = FormatProfile()
    for k in PageDTO.__dataclass_fields__:
        if hasattr(d.page, k):
            setattr(p.page, k, getattr(d.page, k))
    for k in BodyDTO.__dataclass_fields__:
        if hasattr(d.body, k):
            setattr(p.body, k, getattr(d.body, k))
    for k in ParagraphDTO.__dataclass_fields__:
        if hasattr(d.paragraph, k):
            setattr(p.paragraph, k, getattr(d.paragraph, k))
    for level_str, hd_dto in d.headings.items():
        level = int(level_str)
        if level in p.headings:
            for k in HeadingDTO.__dataclass_fields__:
                if k != "level" and hasattr(hd_dto, k):
                    setattr(p.headings[level], k, getattr(hd_dto, k))
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