#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word Formatter — PyQt5 入口
启动 Qt 应用程序，加载主窗口。
"""

import sys
from PyQt5.QtWidgets import QApplication
from main_window import MainWindow


def main():
    app = QApplication(sys.argv)
    app.setApplicationName("Word Formatter")
    app.setStyle("Fusion")  # 跨平台一致的现代风格

    window = MainWindow()
    window.show()

    sys.exit(app.exec_())


if __name__ == "__main__":
    main()