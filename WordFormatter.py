#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
WordFormatter — PyQt6 入口
启动 Qt 应用程序，加载主窗口。
"""

import sys
import os

# ==== PyQt6 高 DPI 缩放修复 ====
# 在 QApplication 创建前设置环境变量，禁用 Qt6 分数缩放
# 恢复与 Qt5 一致的界面大小
os.environ["QT_ENABLE_HIGHDPI_SCALING"] = "0"
os.environ["QT_SCALE_FACTOR"] = "1"

from pathlib import Path
from PyQt6.QtWidgets import QApplication
from PyQt6.QtGui import QFont, QIcon
from main_window import MainWindow


def main():
    app = QApplication(sys.argv)
    app.setApplicationName("Word Formatter")
    app.setWindowIcon(QIcon(str(Path(__file__).parent / "WordFormatter.ico")))

    # 全局字体统一设置：与 Qt5 默认一致的 9pt
    font = QFont()
    font.setFamily("Microsoft YaHei")
    font.setPointSize(9)
    app.setFont(font)

    app.setStyle("Fusion")  # 跨平台一致的现代风格

    window = MainWindow()
    window.show()

    sys.exit(app.exec())


if __name__ == "__main__":
    main()