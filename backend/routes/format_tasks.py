"""
/format 端点 — 排版任务管理
"""

import uuid
import threading
from fastapi import APIRouter, HTTPException
from backend.formatter.data_model import FormatProfile
from backend.formatter.engine import process_file
from shared.schemas import (
    FormatStartRequest, FormatStartResponse,
    FormatProgressResponse, FormatResultResponse, FormatResultItem,
)

router = APIRouter()

# 全局任务存储
_tasks: dict[str, dict] = {}


def _dto_to_profile(d) -> FormatProfile:
    """ProfileDTO → FormatProfile"""
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

    def run():
        results = []
        total = len(req.files)
        try:
            for idx, fp in enumerate(req.files, 1):
                _tasks[task_id]["current"] = idx
                _tasks[task_id]["total"] = total
                ok, msg = process_file(fp, profile, output_dir=profile.output_dir)
                results.append((fp, ok, msg))
            _tasks[task_id]["status"] = "finished"
        except Exception as e:
            _tasks[task_id]["status"] = "error"
            results.append(("", False, str(e)))
        _tasks[task_id]["results"] = results

    t = threading.Thread(target=run, daemon=True)
    t.start()

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
