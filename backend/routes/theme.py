"""
/theme 端点 — 主题管理
复用 theme.py 的颜色配置。
"""

import sys
from pathlib import Path
_ROOT = Path(__file__).parent.parent.parent
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from fastapi import APIRouter
from shared.schemas import ThemeResponse, ThemeRequest, OkResponse

router = APIRouter()

# 全局主题模式
_mode: str = "system"


@router.get("/theme", response_model=ThemeResponse)
async def get_theme():
    """获取当前主题模式"""
    return ThemeResponse(mode=_mode)


@router.put("/theme", response_model=OkResponse)
async def update_theme(req: ThemeRequest):
    """更新主题模式"""
    global _mode
    if req.mode in ("light", "dark", "system"):
        _mode = req.mode
    return OkResponse(ok=True)