"""
/format 端点 — 排版任务管理
复用 worker.py 的 FormatWorker，通过后台线程执行排版。
"""

import sys
import uuid
import threading
from pathlib import Path
_ROOT = Path(__file__).parent.parent.parent
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from fastapi import APIRouter, HTTPException
from models import FormatProfile
from worker import FormatWorker
from shared.schemas import (
    FormatStartRequest, FormatStartResponse,
    FormatProgressResponse, FormatResultResponse, FormatResultItem,
)

router = APIRouter()

# 全局任务存储
_tasks: dict[str, dict] = {}


def _dto_to_profile(d) -> FormatProfile:
    """ProfileDTO → FormatProfile（复用 profile.py 的逻辑）"""
    from backend.routes.profile import _dto_to_profile as _convert
    return _convert(d)


@router.post("/format/start", response_model=FormatStartResponse)
async def start_format(req: FormatStartRequest):
    """启动排版任务"""
    if not req.files:
        raise HTTPException(400, "请选择排版文件")
    
    profile = _dto_to_profile(req.profile) if req.profile else FormatProfile()
    if req.output_dir:
        profile.output_dir = req.output_dir
    
    task_id = str(uuid.uuid4())[:8]
    _tasks[task_id] = {
        "current": 0,
        "total": len(req.files),
        "status": "running",
        "results": [],
    }
    
    worker = FormatWorker(req.files, profile)
    
    def on_progress(current, total):
        _tasks[task_id]["current"] = current
        _tasks[task_id]["total"] = total
    
    def on_finished(results):
        _tasks[task_id]["status"] = "finished"
        _tasks[task_id]["results"] = results
    
    def on_error(msg):
        _tasks[task_id]["status"] = "error"
        _tasks[task_id]["results"] = [("", False, msg)]
    
    worker.progress_updated.connect(on_progress)
    worker.all_finished.connect(on_finished)
    worker.error_occurred.connect(on_error)
    worker.start()
    
    return FormatStartResponse(task_id=task_id)


@router.get("/format/{task_id}/progress", response_model=FormatProgressResponse)
async def get_progress(task_id: str):
    """查询排版进度"""
    task = _tasks.get(task_id)
    if not task:
        raise HTTPException(404, "任务不存在")
    return FormatProgressResponse(
        task_id=task_id,
        current=task["current"],
        total=task["total"],
        status=task["status"],
    )


@router.get("/format/{task_id}/result", response_model=FormatResultResponse)
async def get_result(task_id: str):
    """获取排版结果"""
    task = _tasks.get(task_id)
    if not task:
        raise HTTPException(404, "任务不存在")
    
    items = []
    ok_count = 0
    for fp, ok, msg in task.get("results", []):
        items.append(FormatResultItem(file_path=fp, success=ok, message=msg))
        if ok:
            ok_count += 1
    
    return FormatResultResponse(
        task_id=task_id,
        results=items,
        ok_count=ok_count,
        fail_count=len(items) - ok_count,
    )