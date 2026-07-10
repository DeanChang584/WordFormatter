#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FramelessWindow — 无边框窗口基类
使用 Qt6 官方 API（startSystemMove / startSystemResize）实现窗口行为。
不依赖 nativeEvent、ctypes 或任何 Windows 消息解析。
"""

from PyQt6.QtWidgets import QMainWindow, QWidget
from PyQt6.QtCore import Qt, QPoint
from PyQt6.QtGui import QMouseEvent, QCursor

from title_bar import TitleBar


class _ResizeGrip(QWidget):
    """窗口边缘缩放区域（透明覆盖层）"""

    def __init__(self, position: str, parent=None):
        super().__init__(parent)
        self._position = position
        self._margin = 8
        self._set_cursor()
        self._update_geometry()

    def _set_cursor(self):
        pos = self._position
        if pos in ("top_left", "bottom_right"):
            self.setCursor(Qt.CursorShape.SizeFDiagCursor)
        elif pos in ("top_right", "bottom_left"):
            self.setCursor(Qt.CursorShape.SizeBDiagCursor)
        elif pos in ("left", "right"):
            self.setCursor(Qt.CursorShape.SizeHorCursor)
        elif pos in ("top", "bottom"):
            self.setCursor(Qt.CursorShape.SizeVerCursor)

    def _update_geometry(self):
        """根据位置和父窗口大小更新自身几何"""
        if not self.parentWidget():
            return
        pw, ph = self.parentWidget().width(), self.parentWidget().height()
        m = self._margin
        pos = self._position

        if pos == "left":
            self.setGeometry(0, m, m, ph - 2 * m)
        elif pos == "right":
            self.setGeometry(pw - m, m, m, ph - 2 * m)
        elif pos == "top":
            self.setGeometry(m, 0, pw - 2 * m, m)
        elif pos == "bottom":
            self.setGeometry(m, ph - m, pw - 2 * m, m)
        elif pos == "top_left":
            self.setGeometry(0, 0, m, m)
        elif pos == "top_right":
            self.setGeometry(pw - m, 0, m, m)
        elif pos == "bottom_left":
            self.setGeometry(0, ph - m, m, m)
        elif pos == "bottom_right":
            self.setGeometry(pw - m, ph - m, m, m)

    def _get_edges(self):
        pos = self._position
        edges = Qt.Edge(0)
        if "left" in pos:
            edges |= Qt.Edge.LeftEdge
        if "right" in pos:
            edges |= Qt.Edge.RightEdge
        if "top" in pos:
            edges |= Qt.Edge.TopEdge
        if "bottom" in pos:
            edges |= Qt.Edge.BottomEdge
        return edges

    def mousePressEvent(self, event: QMouseEvent):
        if event.button() == Qt.MouseButton.LeftButton:
            window = self.window()
            if window and window.windowHandle():
                window.windowHandle().startSystemResize(self._get_edges())
            event.accept()
        else:
            super().mousePressEvent(event)


class FramelessWindow(QMainWindow):
    """无边框窗口基类，使用 Qt6 原生 API 实现拖动和缩放"""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint)
        self._title_bar: TitleBar | None = None
        self._resize_grips: list[_ResizeGrip] = []

    def _build_title_bar(self):
        """构建标题栏并插入到布局顶部。子类应调用此方法。"""
        self._title_bar = TitleBar(self)
        self._title_bar.minimize_requested.connect(self.showMinimized)
        self._title_bar.maximize_requested.connect(self._toggle_maximize)
        self._title_bar.close_requested.connect(self.close)

    def _install_resize_grips(self):
        """在窗口四边和四角安装缩放区域"""
        positions = [
            "left", "right", "top", "bottom",
            "top_left", "top_right", "bottom_left", "bottom_right",
        ]
        for pos in positions:
            grip = _ResizeGrip(pos, self)
            self._resize_grips.append(grip)
            grip.show()

    def resizeEvent(self, event):
        """窗口大小变化时更新缩放区域位置"""
        super().resizeEvent(event)
        for grip in self._resize_grips:
            grip._update_geometry()

    def _toggle_maximize(self):
        if self.isMaximized():
            self.showNormal()
        else:
            self.showMaximized()