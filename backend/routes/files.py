"""
/files 端点 — 文件管理
"""

from pathlib import Path
from fastapi import APIRouter
from shared.schemas import FileSelectRequest, FolderRequest, FileDeleteRequest, FilesResponse, OkResponse

router = APIRouter()

# 全局文件列表
_files: list[str] = []


def _get_extensions() -> list[str]:
    """默认支持的文件扩展名"""
    return [".docx", ".doc"]


@router.get("/files", response_model=FilesResponse)
async def get_files():
    """获取当前文件列表"""
    return FilesResponse(files=list(_files), count=len(_files))


@router.post("/files/select", response_model=FilesResponse)
async def select_files(req: FileSelectRequest):
    """添加文件（选择文件）"""
    added = []
    for fp in req.paths:
        p = Path(fp)
        if not p.is_file():
            continue
        fp = str(p)
        if fp not in _files and p.suffix.lower() in _get_extensions():
            _files.append(fp)
            added.append(fp)
    return FilesResponse(files=list(_files), count=len(_files), added=added)


@router.post("/files/folder", response_model=FilesResponse)
async def select_folder(req: FolderRequest):
    """从文件夹批量添加文件"""
    folder = Path(req.folder)
    if not folder.is_dir():
        return FilesResponse(files=list(_files), count=len(_files))
    
    added = []
    exts = _get_extensions()
    for ext in exts:
        for f in folder.rglob(f"*{ext}"):
            fp = str(f)
            if fp not in _files:
                _files.append(fp)
                added.append(fp)
    return FilesResponse(files=list(_files), count=len(_files), added=added)


@router.post("/files/remove", response_model=FilesResponse)
async def remove_files(req: FileDeleteRequest):
    """删除选中文件"""
    for fp in req.paths:
        if fp in _files:
            _files.remove(fp)
    return FilesResponse(files=list(_files), count=len(_files))


@router.delete("/files/all", response_model=OkResponse)
async def clear_files():
    """清空所有文件"""
    _files.clear()
    return OkResponse(ok=True)