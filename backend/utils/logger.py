"""
Word Formatter — 后端日志模块 (Step 0.3)

按「用途」分文件 + 按「日期」自动归档 + 自动保留策略，三者兼得。

活动/近期日志（logs/ 根目录）：
    app.log       前端 / UI 相关（启动、关闭、主题切换）
    backend.log   API、Service（请求、业务逻辑）
    format.log    排版任务（进度、文件处理）
    error.log     所有异常（ERROR 及以上，跨全部分类汇总）
    {分类}-YYYY-MM-DD.log   午夜滚动后的近期每日文件（保留最近 30 天）

归档（logs/archive/）：
    超过 30 天的每日文件移动到此处，保留最近 90 天，超期自动删除。
        archive/app-2026-05-01.log ...

保留策略（用户无需手动清理）：
    logs/ 根目录的每日文件保留最近 30 天 → 之后移入 archive/
    archive/ 保留最近 90 天 → 之后自动删除

接口：get_logger(name, category="backend") -> 配置好的 INFO 级 Logger。
- 主分类文件收 INFO+；error.log 额外收 ERROR+（无论属于哪个分类）。
- 格式 [时间] [级别] [模块名] 消息内容。

按 architecture.md 第 9 节与 tech-stack.md 第 10 节：禁止 print()，统一 logging。
"""

import os
import re
import shutil
import logging
import logging.handlers
from datetime import date, datetime, timedelta
from pathlib import Path

# ============================================================
# 路径与常量
# ============================================================

# backend/utils/logger.py -> parents[2] == 项目根目录 WordFormatter/
_ROOT = Path(__file__).resolve().parents[2]
LOG_DIR = _ROOT / "logs"
ARCHIVE_DIR = LOG_DIR / "archive"

_LOG_LEVEL = logging.INFO
_LOG_FORMAT = "[%(asctime)s] [%(levelname)s] [%(name)s] %(message)s"
_DATE_FORMAT = "%Y-%m-%d %H:%M:%S"

# 允许的用途分类；error 为跨分类的异常汇总
CATEGORIES = ("app", "backend", "format", "error")
_DEFAULT_CATEGORY = "backend"

# 保留策略
ACTIVE_RETENTION_DAYS = 30   # logs/ 根目录每日文件保留天数，之后移入 archive/
ARCHIVE_RETENTION_DAYS = 90  # archive/ 保留天数，之后删除

# 每日文件名：{分类}-YYYY-MM-DD.log（活动文件 {分类}.log 不含日期，不匹配）
_DAILY_RE = re.compile(
    r"^(?P<cat>app|backend|format|error)-(?P<date>\d{4}-\d{2}-\d{2})\.log$"
)

_formatter = logging.Formatter(fmt=_LOG_FORMAT, datefmt=_DATE_FORMAT)


# ============================================================
# 保留策略：清扫过期日志
# ============================================================


def _parse_date(text: str):
    try:
        return datetime.strptime(text, "%Y-%m-%d").date()
    except ValueError:
        return None


def purge_old_logs(
    active_days: int = ACTIVE_RETENTION_DAYS,
    archive_days: int = ARCHIVE_RETENTION_DAYS,
) -> None:
    """执行两级保留策略（幂等，可重复/定时调用）。

    1) logs/ 根目录中超过 active_days 天的每日文件 → 移入 logs/archive/
    2) logs/archive/ 中超过 archive_days 天的文件 → 删除

    活动文件（app.log 等，不含日期）永不被清理。
    """
    today = date.today()
    ARCHIVE_DIR.mkdir(parents=True, exist_ok=True)

    # 1) 近期每日文件超期 → 归档
    move_cutoff = today - timedelta(days=active_days)
    if LOG_DIR.exists():
        for p in LOG_DIR.glob("*.log"):
            m = _DAILY_RE.match(p.name)
            if not m:
                continue
            d = _parse_date(m.group("date"))
            if d and d < move_cutoff:
                dest = ARCHIVE_DIR / p.name
                try:
                    if dest.exists():
                        dest.unlink()
                    shutil.move(str(p), str(dest))
                except OSError:
                    pass

    # 2) 归档超期 → 删除
    del_cutoff = today - timedelta(days=archive_days)
    for p in ARCHIVE_DIR.glob("*.log"):
        m = _DAILY_RE.match(p.name)
        if not m:
            continue
        d = _parse_date(m.group("date"))
        if d and d < del_cutoff:
            try:
                p.unlink()
            except OSError:
                pass


# ============================================================
# 按天滚动：活动文件 -> logs/{分类}-YYYY-MM-DD.log（留在根目录）
# ============================================================


def _daily_namer(default_name: str) -> str:
    """把 logs/app.log.2026-07-11 改写为 logs/app-2026-07-11.log（仍在根目录）。"""
    _dir, base = os.path.split(default_name)
    category, _sep, date_str = base.partition(".log.")
    return str(LOG_DIR / f"{category}-{date_str}.log")


class _DailyRotatingHandler(logging.handlers.TimedRotatingFileHandler):
    """午夜滚动的文件处理器，滚动后顺带执行一次保留策略清扫。"""

    def doRollover(self) -> None:
        super().doRollover()
        try:
            purge_old_logs()
        except Exception:  # 清扫失败不得影响日志本身
            pass


def _make_daily_handler(category: str, level: int) -> logging.Handler:
    """创建按天滚动的文件处理器，活动文件为 logs/{category}.log。"""
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    handler = _DailyRotatingHandler(
        filename=str(LOG_DIR / f"{category}.log"),
        when="midnight",
        interval=1,
        backupCount=0,  # 自行管理保留，禁用内建删除
        encoding="utf-8",
    )
    handler.suffix = "%Y-%m-%d"
    handler.namer = _daily_namer
    handler.setLevel(level)
    handler.setFormatter(_formatter)
    return handler


# 跨分类共享的异常汇总 handler（ERROR+ -> error.log），懒加载单例
_error_handler: logging.Handler | None = None


def _get_error_handler() -> logging.Handler:
    global _error_handler
    if _error_handler is None:
        _error_handler = _make_daily_handler("error", logging.ERROR)
    return _error_handler


# ============================================================
# 公共接口
# ============================================================

_configured: set[str] = set()
_purged_once = False


def get_logger(name: str, category: str = _DEFAULT_CATEGORY) -> logging.Logger:
    """返回配置好的 logger。

    多次以同一 name 调用是安全的：处理器只挂载一次，不重复输出。
    首次调用时执行一次保留策略清扫（进程内仅一次）。

    Args:
        name: logger 名称，通常为模块名（用于日志中的 [模块名]）。
        category: 用途分类，取值 app / backend / format / error，
            决定写入哪个活动文件。非法值回退为 "backend"。

    Returns:
        INFO 级 Logger，写入 logs/{category}.log；ERROR+ 额外写 logs/error.log。
    """
    global _purged_once

    logger = logging.getLogger(name)

    if name in _configured:
        return logger

    if not _purged_once:
        try:
            purge_old_logs()
        except Exception:
            pass
        _purged_once = True

    if category not in CATEGORIES:
        category = _DEFAULT_CATEGORY

    logger.setLevel(_LOG_LEVEL)
    # 不向 root 传播，避免与 uvicorn 等的 root handler 重复输出
    logger.propagate = False

    # 主分类文件（INFO+）
    logger.addHandler(_make_daily_handler(category, _LOG_LEVEL))

    # 统一异常汇总（ERROR+ -> error.log）；error 分类自身不再重复挂
    if category != "error":
        logger.addHandler(_get_error_handler())

    # 控制台，便于开发期观察
    console_handler = logging.StreamHandler()
    console_handler.setLevel(_LOG_LEVEL)
    console_handler.setFormatter(_formatter)
    logger.addHandler(console_handler)

    _configured.add(name)
    return logger
