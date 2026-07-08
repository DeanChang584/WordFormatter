#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
WordFormatter — PyQt5 入口
启动 Qt 应用程序，加载主窗口。
"""

import sys
from pathlib import Path
from PyQt5.QtWidgets import QApplication
from PyQt5.QtGui import QIcon
from main_window import MainWindow


def main():
    app = QApplication(sys.argv)
    app.setApplicationName("WordFormatter")
    app.setWindowIcon(QIcon(str(Path(__file__).parent / "WordFormatter.png")))
    app.setStyle("Fusion")  # 跨平台一致的现代风格

    window = MainWindow()
    window.show()

    sys.exit(app.exec_())


if __name__ == "__main__":
    main()
