#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word Formatter — 后台工作线程
使用 threading.Thread 在后台执行排版任务，通过回调报告进度和结果。
"""

import threading
from typing import Callable, Optional

from models import FormatProfile
from engine import process_file


class FormatWorker(threading.Thread):
    """在后台线程中逐个处理文件，通过回调报告进度和结果"""

    def __init__(
        self,
        file_paths: list,
        profile: FormatProfile,
        on_progress: Optional[Callable[[int, int], None]] = None,
        on_status: Optional[Callable[[str], None]] = None,
        on_file_finished: Optional[Callable[[str, bool, str], None]] = None,
        on_all_finished: Optional[Callable[[list], None]] = None,
        on_error: Optional[Callable[[str], None]] = None,
    ):
        super().__init__(daemon=True)
        self.file_paths = file_paths
        self.profile = profile
        self._cancelled = False

        # 回调函数
        self._on_progress = on_progress or (lambda c, t: None)
        self._on_status = on_status or (lambda s: None)
        self._on_file_finished = on_file_finished or (lambda fp, ok, msg: None)
        self._on_all_finished = on_all_finished or (lambda results: None)
        self._on_error = on_error or (lambda msg: None)

    # ── Qt 风格兼容属性（供现有后端 .connect() 使用）──

    class _Signal:
        """模拟 Qt 信号的 .connect() 接口"""
        def __init__(self):
            self._callbacks: list = []
        def connect(self, callback):
            self._callbacks.append(callback)
        def emit(self, *args):
            for cb in self._callbacks:
                cb(*args)

    @property
    def progress_updated(self) -> '_Signal':
        if not hasattr(self, '_sig_progress'):
            self._sig_progress = self._Signal()
            self._sig_progress.connect(self._on_progress)
        return self._sig_progress

    @property
    def status_message(self) -> '_Signal':
        if not hasattr(self, '_sig_status'):
            self._sig_status = self._Signal()
            self._sig_status.connect(self._on_status)
        return self._sig_status

    @property
    def file_finished(self) -> '_Signal':
        if not hasattr(self, '_sig_file_finished'):
            self._sig_file_finished = self._Signal()
            self._sig_file_finished.connect(self._on_file_finished)
        return self._sig_file_finished

    @property
    def all_finished(self) -> '_Signal':
        if not hasattr(self, '_sig_all_finished'):
            self._sig_all_finished = self._Signal()
            self._sig_all_finished.connect(self._on_all_finished)
        return self._sig_all_finished

    @property
    def error_occurred(self) -> '_Signal':
        if not hasattr(self, '_sig_error'):
            self._sig_error = self._Signal()
            self._sig_error.connect(self._on_error)
        return self._sig_error

    def cancel(self):
        """请求取消（当前处理中的文件会继续完成）"""
        self._cancelled = True

    def run(self):
        results = []
        total = len(self.file_paths)

        try:
            for idx, fp in enumerate(self.file_paths, 1):
                if self._cancelled:
                    self.status_message.emit("已取消")
                    break

                self.progress_updated.emit(idx, total)
                msg = f"[{idx}/{total}] 处理: {fp}"
                self.status_message.emit(msg)

                ok, msg = process_file(fp, self.profile, output_dir=self.profile.output_dir)
                results.append((fp, ok, msg))
                self.file_finished.emit(fp, ok, msg)

                self.progress_updated.emit(idx, total)

        except Exception as e:
            import traceback
            self.error_occurred.emit(f"排版过程出错:\n{traceback.format_exc()}")
        finally:
            self.all_finished.emit(results)