"""
Word Formatter — FastAPI Backend Server
入口文件：启动 FastAPI 服务，注册所有路由。
"""

import sys
import os
from pathlib import Path

# 确保项目根目录在 sys.path 中
_ROOT = Path(__file__).parent.parent
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from shared.schemas import HealthResponse
from backend.routes import profile, files, format_tasks, theme

# ============================================================
# FastAPI App
# ============================================================

app = FastAPI(
    title="Word Formatter API",
    version="1.0.0",
    description="Word Formatter 后端服务",
)

# CORS（WinUI 3 通过 HTTP 访问）
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# 注册路由
app.include_router(profile.router, prefix="/api", tags=["profile"])
app.include_router(files.router, prefix="/api", tags=["files"])
app.include_router(format_tasks.router, prefix="/api", tags=["format"])
app.include_router(theme.router, prefix="/api", tags=["theme"])


@app.get("/api/health", response_model=HealthResponse)
async def health():
    """健康检查"""
    return HealthResponse(status="ok")


# ============================================================
# 入口
# ============================================================

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("backend.server:app", host="127.0.0.1", port=8765, reload=False)