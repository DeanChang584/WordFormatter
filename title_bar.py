#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
TitleBar — 自定义标题栏组件
使用 Qt6 官方 API 实现窗口拖动、最大化/还原、关闭等操作。
"""

from PyQt6.QtWidgets import (
    QWidget, QHBoxLayout, QLabel, QPushButton, QSizePolicy, QMenu,
)
from PyQt6.QtCore import Qt, QPoint, pyqtSignal, QEvent
from PyQt6.QtGui import QIcon, QPixmap, QPainter, QPen, QColor, QMouseEvent


class _CloseButton(QPushButton):
    """关闭按钮，hover 时切换为白色图标"""

    def __init__(self, gray_icon: QIcon, white_icon: QIcon, parent=None):
        super().__init__(parent)
        self._gray = gray_icon
        self._white = white_icon
        self.setIcon(gray_icon)

    def enterEvent(self, event: QEvent):
        self.setIcon(self._white)
        super().enterEvent(event)

    def leaveEvent(self, event: QEvent):
        self.setIcon(self._gray)
        super().leaveEvent(event)


class TitleBar(QWidget):
    """自定义无边框标题栏"""

    # 信号
    minimize_requested = pyqtSignal()
    maximize_requested = pyqtSignal()
    close_requested = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setObjectName("title_bar")
        self.setFixedHeight(80)
        self._drag_pos = QPoint()
        self._setup_ui()

    def _setup_ui(self):
        layout = QHBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(8)

        # 标签页按钮
        self._tab_buttons = []
        for text in ("通用", "模板", "更多"):
            btn = QPushButton(text)
            btn.setFixedSize(160, 64)
            self._tab_buttons.append(btn)
            layout.addWidget(btn)

        # 弹簧
        layout.addStretch()

        # 窗口控制按钮
        win_controls = QWidget()
        win_controls.setObjectName("win_controls")
        win_layout = QHBoxLayout(win_controls)
        win_layout.setContentsMargins(0, 0, 0, 0)
        win_layout.setSpacing(0)

        # 最小化
        min_btn = QPushButton()
        min_btn.setIcon(QIcon(self._make_icon("min")))
        min_btn.setIconSize(QPixmap(24, 24).size())
        min_btn.setObjectName("window_min_btn")
        min_btn.setFixedSize(72, 72)
        min_btn.clicked.connect(self.minimize_requested.emit)
        win_layout.addWidget(min_btn)

        # 最大化
        self._max_btn = QPushButton()
        self._max_btn.setIcon(QIcon(self._make_icon("max")))
        self._max_btn.setIconSize(QPixmap(24, 24).size())
        self._max_btn.setObjectName("window_max_btn")
        self._max_btn.setFixedSize(72, 72)
        self._max_btn.clicked.connect(self.maximize_requested.emit)
        win_layout.addWidget(self._max_btn)

        # 关闭（灰色 + 白色两套图标，hover 自动切换）
        close_icon_gray = QIcon(self._make_icon("close", "#6C757D"))
        close_icon_white = QIcon(self._make_icon("close", "#FFFFFF"))
        close_btn = _CloseButton(close_icon_gray, close_icon_white)
        close_btn.setIconSize(QPixmap(24, 24).size())
        close_btn.setObjectName("window_close_btn")
        close_btn.setFixedSize(72, 72)
        close_btn.clicked.connect(self.close_requested.emit)
        win_layout.addWidget(close_btn)

        layout.addWidget(win_controls)

    # ---- 图标绘制 ----

    @staticmethod
    def _make_icon(icon_type: str, color: str = "#6C757D") -> QPixmap:
        pm = QPixmap(24, 24)
        pm.fill(Qt.GlobalColor.transparent)
        p = QPainter(pm)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        pen = QPen(QColor(color), 2.5)
        pen.setCapStyle(Qt.PenCapStyle.RoundCap)
        pen.setJoinStyle(Qt.PenJoinStyle.RoundJoin)
        p.setPen(pen)
        if icon_type == "min":
            p.drawLine(1, 12, 23, 12)
        elif icon_type == "max":
            p.drawRect(2, 2, 20, 20)
        elif icon_type == "close":
            p.drawLine(1, 1, 23, 23)
            p.drawLine(23, 1, 1, 23)
        p.end()
        return pm

    # ---- 标题栏点击事件（窗口拖动 + 双击最大化）----

    def mousePressEvent(self, event: QMouseEvent):
        if event.button() == Qt.MouseButton.LeftButton:
            window = self.window()
            if window and window.windowHandle():
                window.windowHandle().startSystemMove()
            event.accept()
        else:
            super().mousePressEvent(event)

    def mouseDoubleClickEvent(self, event: QMouseEvent):
        if event.button() == Qt.MouseButton.LeftButton:
            self.maximize_requested.emit()
            event.accept()
        else:
            super().mouseDoubleClickEvent(event)