#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Theme — 主题配置与切换
基于 DESIGN.md 配色系统，提供浅色/深色/跟随系统三种模式。
"""

import sys
from PyQt6.QtWidgets import QApplication
from PyQt6.QtCore import QSettings

# ============================================================
# 浅色主题（当前 .ui 样式表颜色）
# ============================================================
LIGHT = {
    "canvas": "#F5F5F7",
    "card": "#FFFFFF",
    "ink": "#1D1D1F",
    "ink_muted": "#6E6E73",
    "border": "#E0E0E0",
    "border_subtle": "rgba(0, 0, 0, 0.04)",
    "primary": "#0066CC",
    "primary_hover": "rgba(0, 102, 204, 0.04)",
    "primary_pressed": "rgba(0, 102, 204, 0.08)",
    "list_hover": "#F5F5F7",
    "list_selected": "#E8F0FE",
    "progress_bg": "#E8E8ED",
    "progress_chunk": "#0066CC",
    "tab_active_bg": "#FFFFFF",
    "tab_active_text": "#1A1A1A",
    "tab_active_border": "#E2E8F0",
    "tab_inactive_text": "#6C757D",
    "tab_hover_bg": "#F1F3F5",
    "tab_hover_border": "#E9ECEF",
    "scrollbar_hover": "rgba(0, 0, 0, 0.12)",
    "scrollbar_active": "rgba(0, 0, 0, 0.25)",
    "close_btn_hover": "#E81123",
    "win_btn_hover": "rgba(0, 0, 0, 0.06)",
    "input_hover": "#0066CC",
}

# ============================================================
# 深色主题（基于 DESIGN.md surface-tile 配色）
# ============================================================
DARK = {
    "canvas": "#272729",
    "card": "#2a2a2c",
    "ink": "#FFFFFF",
    "ink_muted": "#CCCCCC",
    "border": "#3E3E3E",
    "border_subtle": "rgba(255, 255, 255, 0.06)",
    "primary": "#2997FF",
    "primary_hover": "rgba(41, 151, 255, 0.10)",
    "primary_pressed": "rgba(41, 151, 255, 0.16)",
    "list_hover": "#333335",
    "list_selected": "#1A3A5C",
    "progress_bg": "#3E3E3E",
    "progress_chunk": "#2997FF",
    "tab_active_bg": "#3E3E3E",
    "tab_active_text": "#FFFFFF",
    "tab_active_border": "#4E4E4E",
    "tab_inactive_text": "#999999",
    "tab_hover_bg": "#353537",
    "tab_hover_border": "#4E4E4E",
    "scrollbar_hover": "rgba(255, 255, 255, 0.12)",
    "scrollbar_active": "rgba(255, 255, 255, 0.25)",
    "close_btn_hover": "#E81123",
    "win_btn_hover": "rgba(255, 255, 255, 0.08)",
    "input_hover": "#2997FF",
}


def detect_system_theme() -> str:
    """检测 Windows 系统主题偏好，返回 'light' 或 'dark'"""
    try:
        import winreg
        key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
        )
        value, _ = winreg.QueryValueEx(key, "AppsUseLightTheme")
        winreg.CloseKey(key)
        return "light" if value == 1 else "dark"
    except Exception:
        return "light"


def generate_stylesheet(c: dict) -> str:
    """根据颜色字典生成完整样式表"""
    return f"""
/* ============================================================
   Word Formatter — {'Dark' if c is DARK else 'Light'} Theme
   ============================================================ */

/* --- 全局默认 --- */
* {{
    font-family: "Segoe UI Variable Text", "Microsoft YaHei UI";
    font-size: 10pt;
    color: {c['ink']};
}}

/* --- Canvas --- */
QMainWindow, QWidget#centralwidget {{
    background-color: {c['canvas']};
    border-radius: 8px;
}}

/* --- 弹出框 --- */
QDialog, QMessageBox, QMenu {{
    background-color: {c['canvas']};
    color: {c['ink']};
}}
QMenu {{
    border: 1px solid {c['border']};
    border-radius: 4px;
    padding: 4px 0px;
}}
QMenu::item {{
    padding: 8px 16px;
    min-width: 128px;
}}
QMenu::item:selected {{
    background-color: {c['list_hover']};
    color: {c['ink']};
}}

/* --- 卡片 --- */
QGroupBox {{
    font-family: "Segoe UI Variable Display", "Microsoft YaHei UI";
    font-size: 10pt;
    font-weight: 600;
    color: {c['ink']};
    background-color: {c['card']};
    border: 1px solid {c['border']};
    border-radius: 18px;
    padding-top: 58px;
    padding-left: 24px;
    padding-right: 24px;
    padding-bottom: 24px;
}}
QGroupBox::title {{
    subcontrol-origin: margin;
    subcontrol-position: top left;
    left: 48px;
    top: 24px;
}}

/* --- 列表 --- */
QListWidget {{
    background-color: {c['card']};
    border: 1px solid {c['border']};
    border-radius: 18px;
    padding: 4px;
    color: {c['ink']};
}}
QListWidget::item {{
    border-radius: 4px;
    padding: 4px 8px;
}}
QListWidget::item:hover {{
    background-color: {c['list_hover']};
}}
QListWidget::item:selected {{
    background-color: {c['list_selected']};
    color: {c['ink']};
}}

/* --- 输入控件 --- */
QLineEdit, QSpinBox, QDoubleSpinBox, QComboBox {{
    background-color: {c['card']};
    color: {c['ink']};
    border: 1px solid {c['border']};
    border-radius: 8px;
    padding: 4px 8px;
    min-width: 136px;
    max-width: 136px;
    min-height: 40px;
    max-height: 40px;
}}
QLineEdit:hover, QSpinBox:hover, QDoubleSpinBox:hover, QComboBox:hover {{
    border: 1px solid {c['input_hover']};
}}
QLineEdit:focus, QSpinBox:focus, QDoubleSpinBox:focus, QComboBox:focus {{
    border: 1px solid {c['input_hover']};
}}

/* --- SpinBox Fluent arrows --- */
QDoubleSpinBox::up-button, QDoubleSpinBox::down-button,
QSpinBox::up-button, QSpinBox::down-button {{
    width: 22px;
    border: none;
    background: transparent;
    border-radius: 4px;
    margin: 2px;
}}
QDoubleSpinBox::up-button:hover, QDoubleSpinBox::down-button:hover,
QSpinBox::up-button:hover, QSpinBox::down-button:hover {{
    background: {c['list_hover']};
}}
QDoubleSpinBox::up-button:pressed, QDoubleSpinBox::down-button:pressed,
QSpinBox::up-button:pressed, QSpinBox::down-button:pressed {{
    background: {c['border']};
}}
QDoubleSpinBox::up-arrow, QSpinBox::up-arrow {{
    image: url(ui/icons/arrow_up.svg);
    width: 14px;
    height: 10px;
}}
QDoubleSpinBox::down-arrow, QSpinBox::down-arrow {{
    image: url(ui/icons/arrow_down.svg);
    width: 14px;
    height: 10px;
}}

/* --- 多控件行 96px --- */
QDoubleSpinBox#spin_body_indent_value, QDoubleSpinBox#spin_heading_indent_value,
QComboBox#combo_body_indent_unit, QComboBox#combo_heading_indent_unit,
QComboBox#combo_space_before_unit, QComboBox#combo_space_after_unit,
QComboBox#combo_heading_sb_unit, QComboBox#combo_heading_sa_unit,
QComboBox#combo_margin_top_unit, QComboBox#combo_margin_bottom_unit,
QComboBox#combo_margin_left_unit, QComboBox#combo_margin_right_unit,
QComboBox#combo_header_margin_unit, QComboBox#combo_footer_margin_unit {{
    min-width: 96px;
    max-width: 96px;
}}

/* --- 下拉框 --- */
QComboBox::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    width: 20px;
    border-left-width: 0px;
}}

/* --- 按钮 --- */
QPushButton {{
    background-color: transparent;
    color: {c['primary']};
    border: 1px solid {c['primary']};
    border-radius: 9999px;
    padding: 8px 16px;
}}
QPushButton:hover {{
    background-color: {c['primary_hover']};
}}
QPushButton:pressed {{
    background-color: {c['primary_pressed']};
}}

/* --- 勾选框 --- */
QCheckBox {{
    color: {c['ink']};
    spacing: 6px;
}}

/* --- 进度条 --- */
QProgressBar {{
    background-color: {c['progress_bg']};
    border: none;
    border-radius: 4px;
    min-height: 6px;
    text-align: center;
    font-size: 10pt;
    font-weight: 400;
    color: {c['ink']};
}}
QProgressBar::chunk {{
    background-color: {c['progress_chunk']};
    border-radius: 4px;
}}

/* --- 右栏 --- */
QScrollArea#right_column {{
    background-color: {c['card']};
    border-radius: 18px;
    border: none;
}}
QScrollArea#right_column > QWidget > QWidget {{
    background-color: transparent;
}}

/* --- 滚动区域 --- */
QAbstractScrollArea {{
    background: transparent;
    border: none;
}}
QAbstractScrollArea > QWidget {{
    background: {c['canvas']};
}}
QAbstractScrollArea > QWidget > QWidget {{
    background: transparent;
}}

/* --- 滚动条 --- */
QScrollBar:vertical {{
    background-color: transparent;
    width: 4px;
    margin: 0;
}}
QScrollBar::handle:vertical {{
    background-color: transparent;
    border-radius: 2px;
    min-height: 20px;
}}
QScrollArea:hover QScrollBar::handle:vertical {{
    background-color: {c['scrollbar_hover']};
}}
QScrollBar::handle:vertical:hover {{
    background-color: {c['scrollbar_active']};
}}
QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {{
    height: 0px;
}}

/* --- 标题栏 --- */
QWidget#title_bar {{
    background-color: transparent;
    border: none;
    border-radius: 0px;
    min-height: 80px;
    max-height: 80px;
}}
QWidget#title_bar QPushButton#card_tab {{
    background-color: transparent;
    color: {c['tab_inactive_text']};
    border: none;
    border-radius: 8px;
    padding: 0px 16px;
    margin-top: 16px;
    font-size: 10pt;
    font-weight: 400;
    text-align: center;
}}
QWidget#title_bar QPushButton#card_tab:hover {{
    background-color: {c['tab_hover_bg']};
    border: 1px solid {c['tab_hover_border']};
}}
QWidget#title_bar QPushButton#card_tab_active {{
    background-color: {c['tab_active_bg']};
    color: {c['tab_active_text']};
    border: 1px solid {c['tab_active_border']};
    border-radius: 8px;
    padding: 0px 16px;
    margin-top: 16px;
    font-size: 10pt;
    font-weight: 400;
    text-align: center;
}}
QWidget#title_bar QPushButton#card_tab_active:hover {{
    background-color: {c['tab_active_bg']};
}}
QWidget#title_bar QPushButton#card_add {{
    background-color: transparent;
    color: {c['tab_inactive_text']};
    border: none;
    border-radius: 8px;
    font-size: 14pt;
    font-weight: 400;
}}
QWidget#title_bar QPushButton#card_add:hover {{
    background-color: {c['tab_hover_bg']};
    border: 1px solid {c['tab_hover_border']};
}}
QWidget#title_bar QPushButton#settings_btn {{
    background-color: transparent;
    color: {c['tab_inactive_text']};
    border: none;
    border-radius: 0px;
    font-size: 14pt;
}}
QWidget#title_bar QPushButton#settings_btn:hover {{
    background-color: {c['win_btn_hover']};
}}
QWidget#title_bar QWidget#win_controls QPushButton {{
    background-color: transparent;
    border: none;
    border-radius: 0px;
}}
QWidget#title_bar QWidget#win_controls QPushButton:hover {{
    background-color: {c['win_btn_hover']};
}}
QWidget#title_bar QWidget#win_controls QPushButton#window_close_btn:hover {{
    background-color: {c['close_btn_hover']};
}}

/* --- 状态栏 --- */
QLabel#label_status {{
    background-color: {c['card']};
    border: 1px solid {c['border']};
    border-radius: 18px;
    font-family: "Segoe UI Variable Text", "Microsoft YaHei UI";
    font-size: 10pt;
    font-weight: 400;
    color: {c['ink_muted']};
}}

/* --- 页面标题 --- */
QLabel#label_title {{
    font-family: "Segoe UI Variable Display", "Microsoft YaHei UI";
    font-size: 18pt;
    font-weight: 600;
    color: {c['ink']};
    background-color: transparent;
}}

/* --- 分隔条 --- */
QSplitter::handle {{
    background-color: transparent;
}}

/* --- 左中栏容器 --- */
QWidget#left_column, QWidget#middle_column {{
    background-color: transparent;
}}
"""


# ============================================================
# 主题切换
# ============================================================

def apply_theme(app, mode: str):
    """
    应用主题。
    mode: 'light', 'dark', 'system'
    返回实际应用的模式 ('light' 或 'dark')
    """
    if mode == "system":
        actual = detect_system_theme()
    else:
        actual = mode

    colors = DARK if actual == "dark" else LIGHT
    stylesheet = generate_stylesheet(colors)
    app.setStyleSheet(stylesheet)
    return actual


def load_theme_preference() -> str:
    """从 QSettings 读取主题偏好，默认 'light'"""
    settings = QSettings("WordFormatter", "WordFormatter")
    # 首次运行强制为浅色
    if not settings.contains("theme_mode"):
        return "system"
    return settings.value("theme_mode", "system", type=str)


def apply_dark_title_bar(widget):
    """将窗口标题栏设为深色模式（Windows 10/11）"""
    try:
        import ctypes
        hwnd = int(widget.winId())
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        ctypes.windll.dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
            ctypes.byref(ctypes.c_int(1)),
            ctypes.sizeof(ctypes.c_int)
        )
    except Exception:
        pass


def save_theme_preference(mode: str):
    """保存主题偏好到 QSettings"""
    settings = QSettings("WordFormatter", "WordFormatter")
    settings.setValue("theme_mode", mode)