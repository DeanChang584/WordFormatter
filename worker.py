#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word 文档排版工具 — 后台工作线程
使用 QThread 在后台执行排版任务，通过信号与主线程通信。
"""

from PyQt5.QtCore import QThread, pyqtSignal

from models import FormatProfile
from engine import process_file


class FormatWorker(QThread):
    """在后台线程中逐个处理文件，通过信号报告进度和结果"""

    # 信号定义
    progress_updated = pyqtSignal(int, int)          # (current, total)
    status_message = pyqtSignal(str)                  # 当前状态文字
    file_finished = pyqtSignal(str, bool, str)        # (filepath, success, message)
    all_finished = pyqtSignal(list)                    # [(filepath, ok, msg), ...]
    error_occurred = pyqtSignal(str)                   # 错误信息

    def __init__(self, file_paths: list, profile: FormatProfile, parent=None):
        super().__init__(parent)
        self.file_paths = file_paths
        self.profile = profile
        self._cancelled = False

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