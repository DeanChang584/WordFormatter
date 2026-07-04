#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word 文档排版工具 — PyQt5 主窗口
加载 Qt Designer 生成的 .ui 文件，连接信号/槽，驱动排版引擎。
"""

import os, sys, json
from pathlib import Path

from PyQt5.QtWidgets import (
    QMainWindow, QFileDialog, QMessageBox, QColorDialog, QApplication,
    QListWidgetItem,
)
from PyQt5.QtCore import Qt, QTimer
from PyQt5.QtWidgets import QGraphicsDropShadowEffect
from PyQt5 import uic

from models import (
    FormatProfile, ParagraphConfig, HeadingStyleConfig,
    PageConfig, BodyConfig, FONT_SIZE_MAP, FONT_SIZE_NAMES, font_size_to_name,
)
from engine import check_dependencies
from worker import FormatWorker


# ---- 常量 ----
if getattr(sys, 'frozen', False):
    _BASE_DIR = Path(sys._MEIPASS)
else:
    _BASE_DIR = Path(__file__).parent

UI_DIR = _BASE_DIR / "ui"
UI_FILE = UI_DIR / "main_window.ui"

FONT_CN_LIST = ["宋体", "仿宋", "黑体", "楷体", "微软雅黑", "等线", "华文楷体", "华文宋体"]
FONT_EN_LIST = ["Times New Roman", "Arial", "Calibri", "Segoe UI", "Georgia", "Courier New"]

LINE_SPACING_MODE_MAP = {
    "倍行距": "multiple",
    "固定值": "fixed",
    "最小值": "at_least",
}
LINE_SPACING_MODE_REVERSE = {v: k for k, v in LINE_SPACING_MODE_MAP.items()}

ALIGNMENT_MAP = {
    "左对齐": "left",
    "居中": "center",
    "右对齐": "right",
    "两端对齐": "justify",
    "分散对齐": "distribute",
}
ALIGNMENT_REVERSE = {v: k for k, v in ALIGNMENT_MAP.items()}

HEADING_LEVEL_MAP = {
    "标题一": 1, "标题二": 2, "标题三": 3,
    "标题四": 4, "标题五": 5, "标题六": 6,
}
HEADING_LEVEL_NAMES = list(HEADING_LEVEL_MAP.keys())


# ============================================================
# 主窗口
# ============================================================

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()

        # 数据
        self.profile = FormatProfile()
        self.file_paths: list = []
        self._worker: FormatWorker = None
        self._current_heading_level: int = 1
        self._body_color: str = "#000000"
        self._heading_color: str = "#000000"

        # 加载 .ui 文件
        if UI_FILE.exists():
            uic.loadUi(str(UI_FILE), self)
        else:
            self._show_ui_missing_error()
            return

        self.setWindowTitle("Word 文档排版工具")
        self.resize(2000, 1500)

        # 初始化控件值
        self._init_defaults()
        # 连接信号
        self._connect_signals()
        # 检查依赖
        QTimer.singleShot(100, self._check_deps)

    # ================================================================
    # 初始化默认值
    # ================================================================

    def _init_defaults(self):
        """将模型默认值加载到 UI 控件"""
        # ---- 页面设置 ----
        self._safe_set(self, "spin_margin_top", self.profile.page.margin_top)
        self._safe_set(self, "spin_margin_bottom", self.profile.page.margin_bottom)
        self._safe_set(self, "spin_margin_left", self.profile.page.margin_left)
        self._safe_set(self, "spin_margin_right", self.profile.page.margin_right)

        # ---- 段落设置 ----
        self._safe_combo(self, "combo_line_spacing_mode",
                         list(LINE_SPACING_MODE_MAP.keys()), "倍行距")
        self._safe_set(self, "spin_line_spacing_value", self.profile.paragraph.line_spacing_value)

        self._safe_set(self, "spin_first_indent", self.profile.paragraph.first_line_indent)
        self._safe_combo(self, "combo_indent_unit", ["mm", "字符"],
                         "mm" if self.profile.paragraph.first_line_indent_unit == "mm" else "字符")

        self._safe_set(self, "spin_left_indent", self.profile.paragraph.left_indent)
        self._safe_set(self, "spin_right_indent", self.profile.paragraph.right_indent)

        self._safe_set(self, "spin_space_before", self.profile.paragraph.space_before)
        self._safe_set(self, "spin_space_after", self.profile.paragraph.space_after)

        self._safe_combo(self, "combo_alignment", list(ALIGNMENT_MAP.keys()), "两端对齐")

        # ---- 正文样式 ----
        self._safe_combo(self, "combo_body_font_cn", FONT_CN_LIST, self.profile.body.font_cn)
        self._safe_combo(self, "combo_body_font_en", FONT_EN_LIST, self.profile.body.font_en)
        self._safe_combo(self, "combo_body_font_size", FONT_SIZE_NAMES, font_size_to_name(self.profile.body.font_size))
        self._safe_set(self, "check_body_bold", self.profile.body.font_bold, is_bool=True)
        self._safe_set(self, "check_body_italic", self.profile.body.font_italic, is_bool=True)
        self._body_color = self.profile.body.font_color

        # ---- 标题样式 ----
        self._safe_combo(self, "combo_heading_level", HEADING_LEVEL_NAMES, "标题一")
        self._load_heading_to_ui(1)

        # ---- 文件过滤 ----
        self._safe_set(self, "check_filter_docx", True, is_bool=True)
        self._safe_set(self, "check_filter_doc", True, is_bool=True)

        # ---- 操作区 ----
        self._safe_set(self, "progress_bar", 0, is_progress=True)
        self._safe_set(self, "label_status", "就绪", is_label=True)
        self._safe_set(self, "label_file_count", "已选: 0 个文件", is_label=True)



    # ---- 安全设值辅助 ----

    def _safe_set(self, obj, attr_name: str, value, *, is_bool=False, is_progress=False, is_label=False):
        """安全设置控件值，控件不存在则静默跳过"""
        widget = getattr(obj, attr_name, None)
        if widget is None:
            return
        try:
            if is_bool:
                widget.setChecked(bool(value))
            elif is_progress:
                widget.setValue(int(value))
            elif is_label:
                widget.setText(str(value))
            else:
                # QDoubleSpinBox / QSpinBox
                widget.setValue(float(value))
        except Exception:
            pass

    def _safe_combo(self, obj, attr_name: str, items: list, current: str):
        """安全设置下拉框选项和当前值"""
        widget = getattr(obj, attr_name, None)
        if widget is None:
            return
        try:
            widget.clear()
            widget.addItems(items)
            idx = widget.findText(current)
            if idx >= 0:
                widget.setCurrentIndex(idx)
            elif items:
                widget.setCurrentIndex(0)
        except Exception:
            pass

    # ================================================================
    # 信号连接
    # ================================================================

    def _connect_signals(self):
        """连接所有控件的信号到槽函数"""

        # ---- 文件操作 ----
        self._connect(self, "btn_select_file", "clicked", self._select_single_file)
        self._connect(self, "btn_select_folder", "clicked", self._select_folder)
        self._connect(self, "btn_clear_files", "clicked", self._clear_files)
        self._connect(self, "btn_remove_selected", "clicked", self._remove_selected_files)

        # ---- 操作按钮 ----
        self._connect(self, "btn_start_format", "clicked", self._start_formatting)
        self._connect(self, "btn_output_dir", "clicked", self._select_output_dir)
        self._connect(self, "btn_preview_config", "clicked", self._preview_config)

        # ---- 颜色按钮 ----
        self._connect(self, "btn_body_color", "clicked", self._pick_body_color)
        self._connect(self, "btn_heading_color", "clicked", self._pick_heading_color)

        # ---- 标题级别切换 ----
        self._connect(self, "combo_heading_level", "currentIndexChanged", self._on_heading_level_change)

        # ---- 行距模式切换 ----
        self._connect(self, "combo_line_spacing_mode", "currentTextChanged", self._on_line_mode_change)

    def _connect(self, obj, attr_name: str, signal_name: str, slot):
        """安全连接信号，控件不存在则跳过"""
        widget = getattr(obj, attr_name, None)
        if widget is None:
            return
        try:
            signal = getattr(widget, signal_name, None)
            if signal is not None:
                signal.connect(slot)
        except Exception:
            pass

    # ================================================================
    # 标题样式 UI ↔ 模型 同步
    # ================================================================

    def _load_heading_to_ui(self, level: int):
        """将指定级别的标题配置显示到 UI 控件"""
        hd = self.profile.headings[level]
        self._safe_combo(self, "combo_heading_font_cn", FONT_CN_LIST, hd.font_cn)
        self._safe_combo(self, "combo_heading_font_en", FONT_EN_LIST, hd.font_en)
        self._safe_combo(self, "combo_heading_font_size", FONT_SIZE_NAMES, font_size_to_name(hd.font_size))
        self._safe_set(self, "check_heading_bold", hd.font_bold, is_bool=True)
        self._safe_set(self, "check_heading_italic", hd.font_italic, is_bool=True)
        self._safe_combo(self, "combo_heading_alignment",
                         list(ALIGNMENT_MAP.keys()),
                         ALIGNMENT_REVERSE.get(hd.alignment, "左对齐"))
        self._safe_set(self, "spin_heading_indent", hd.first_line_indent)
        self._safe_set(self, "spin_heading_space_before", hd.space_before)
        self._safe_set(self, "spin_heading_space_after", hd.space_after)
        self._safe_set(self, "spin_heading_line_spacing", hd.line_spacing_value)
        self._heading_color = hd.font_color

    def _save_ui_to_heading(self, level: int):
        """将当前 UI 控件值保存到指定级别的标题配置"""
        hd = self.profile.headings[level]

        # 字体
        combo = getattr(self, "combo_heading_font_cn", None)
        if combo: hd.font_cn = combo.currentText()
        combo = getattr(self, "combo_heading_font_en", None)
        if combo: hd.font_en = combo.currentText()

        # 字号
        combo = getattr(self, "combo_heading_font_size", None)
        if combo:
            sz_name = combo.currentText()
            hd.font_size = FONT_SIZE_MAP.get(sz_name, 12.0)

        # 加粗/斜体
        cb = getattr(self, "check_heading_bold", None)
        if cb: hd.font_bold = cb.isChecked()
        cb = getattr(self, "check_heading_italic", None)
        if cb: hd.font_italic = cb.isChecked()

        # 对齐
        combo = getattr(self, "combo_heading_alignment", None)
        if combo:
            hd.alignment = ALIGNMENT_MAP.get(combo.currentText(), "left")

        # 数值
        spin = getattr(self, "spin_heading_indent", None)
        if spin: hd.first_line_indent = spin.value()
        spin = getattr(self, "spin_heading_space_before", None)
        if spin: hd.space_before = spin.value()
        spin = getattr(self, "spin_heading_space_after", None)
        if spin: hd.space_after = spin.value()
        spin = getattr(self, "spin_heading_line_spacing", None)
        if spin: hd.line_spacing_value = spin.value()

        # 颜色
        hd.font_color = self._heading_color

    def _on_heading_level_change(self, index: int):
        """标题级别下拉切换"""
        if index < 0:
            return
        # 先保存当前级别
        self._save_ui_to_heading(self._current_heading_level)
        # 切换
        name = HEADING_LEVEL_NAMES[index]
        self._current_heading_level = HEADING_LEVEL_MAP.get(name, 1)
        self._load_heading_to_ui(self._current_heading_level)

    def _on_line_mode_change(self, text: str):
        """行距类型切换时自动调整默认值"""
        spin = getattr(self, "spin_line_spacing_value", None)
        if spin is None:
            return
        if text == "固定值":
            spin.setValue(28.0)
        elif text == "倍行距":
            spin.setValue(1.5)

    # ================================================================
    # 颜色选择
    # ================================================================

    def _pick_body_color(self):
        color = QColorDialog.getColor()
        if color.isValid():
            self._body_color = color.name()
            self._update_color_button("btn_body_color", self._body_color)

    def _pick_heading_color(self):
        color = QColorDialog.getColor()
        if color.isValid():
            self._heading_color = color.name()
            self._update_color_button("btn_heading_color", self._heading_color)

    def _update_color_button(self, attr_name: str, color_hex: str):
        """更新颜色按钮的样式以显示当前颜色"""
        btn = getattr(self, attr_name, None)
        if btn is None:
            return
        try:
            btn.setStyleSheet(
                f"background-color: {color_hex};"
                f"border: 1px solid #999; border-radius: 3px;"
                f"min-width: 28px; min-height: 22px;"
            )
        except Exception:
            pass

    # ================================================================
    # 文件操作
    # ================================================================

    def _select_single_file(self):
        paths, _ = QFileDialog.getOpenFileNames(
            self, "选择 Word 文档", "",
            "Word 文档 (*.docx *.doc);;所有文件 (*.*)"
        )
        if paths:
            added = [p for p in paths if p not in self.file_paths]
            self.file_paths.extend(added)
            self._refresh_file_list()

    def _select_folder(self):
        folder = QFileDialog.getExistingDirectory(self, "选择包含 Word 文档的文件夹")
        if not folder:
            return
        exts = []
        cb = getattr(self, "check_filter_docx", None)
        if cb and cb.isChecked(): exts.append(".docx")
        cb = getattr(self, "check_filter_doc", None)
        if cb and cb.isChecked(): exts.append(".doc")

        new_files = []
        for ext in exts:
            for f in Path(folder).rglob(f"*{ext}"):
                fp = str(f)
                if fp not in self.file_paths:
                    new_files.append(fp)
        if new_files:
            self.file_paths.extend(new_files)
            self._refresh_file_list()
        else:
            QMessageBox.information(self, "提示", "所选文件夹中未找到符合条件的 Word 文档")

    def _clear_files(self):
        self.file_paths.clear()
        self._refresh_file_list()

    def _remove_selected_files(self):
        lst = getattr(self, "list_files", None)
        if lst is None:
            return
        selected = lst.selectedItems()
        if not selected:
            return
        for item in selected:
            fp = item.text()
            if fp in self.file_paths:
                self.file_paths.remove(fp)
        self._refresh_file_list()

    def _refresh_file_list(self):
        """根据过滤条件刷新列表显示"""
        lst = getattr(self, "list_files", None)
        if lst is None:
            return
        exts = []
        cb = getattr(self, "check_filter_docx", None)
        if cb and cb.isChecked(): exts.append(".docx")
        cb = getattr(self, "check_filter_doc", None)
        if cb and cb.isChecked(): exts.append(".doc")

        lst.clear()
        display = [f for f in self.file_paths if Path(f).suffix.lower() in exts]
        for fp in display:
            lst.addItem(fp)

        label = getattr(self, "label_file_count", None)
        if label:
            label.setText(f"已选: {len(display)} 个文件")

    # ================================================================
    # 配置收集
    # ================================================================

    def _collect_profile_from_ui(self) -> FormatProfile:
        """从 UI 控件收集完整排版配置"""
        profile = FormatProfile()

        # 页面设置
        spin = getattr(self, "spin_margin_top", None)
        if spin: profile.page.margin_top = spin.value()
        spin = getattr(self, "spin_margin_bottom", None)
        if spin: profile.page.margin_bottom = spin.value()
        spin = getattr(self, "spin_margin_left", None)
        if spin: profile.page.margin_left = spin.value()
        spin = getattr(self, "spin_margin_right", None)
        if spin: profile.page.margin_right = spin.value()

        # 正文样式
        combo = getattr(self, "combo_body_font_cn", None)
        if combo: profile.body.font_cn = combo.currentText()
        combo = getattr(self, "combo_body_font_en", None)
        if combo: profile.body.font_en = combo.currentText()
        combo = getattr(self, "combo_body_font_size", None)
        if combo:
            sz_name = combo.currentText()
            profile.body.font_size = FONT_SIZE_MAP.get(sz_name, 12.0)
        profile.body.font_color = self._body_color
        cb = getattr(self, "check_body_bold", None)
        if cb: profile.body.font_bold = cb.isChecked()
        cb = getattr(self, "check_body_italic", None)
        if cb: profile.body.font_italic = cb.isChecked()

        # 段落设置
        combo = getattr(self, "combo_line_spacing_mode", None)
        if combo:
            profile.paragraph.line_spacing_mode = LINE_SPACING_MODE_MAP.get(
                combo.currentText(), "multiple"
            )
        spin = getattr(self, "spin_line_spacing_value", None)
        if spin: profile.paragraph.line_spacing_value = spin.value()

        spin = getattr(self, "spin_first_indent", None)
        if spin: profile.paragraph.first_line_indent = spin.value()
        combo = getattr(self, "combo_indent_unit", None)
        if combo: profile.paragraph.first_line_indent_unit = combo.currentText()

        spin = getattr(self, "spin_left_indent", None)
        if spin: profile.paragraph.left_indent = spin.value()
        spin = getattr(self, "spin_right_indent", None)
        if spin: profile.paragraph.right_indent = spin.value()

        spin = getattr(self, "spin_space_before", None)
        if spin: profile.paragraph.space_before = spin.value()
        spin = getattr(self, "spin_space_after", None)
        if spin: profile.paragraph.space_after = spin.value()

        combo = getattr(self, "combo_alignment", None)
        if combo:
            profile.paragraph.alignment = ALIGNMENT_MAP.get(combo.currentText(), "justify")

        # 标题样式（保存当前正在编辑的级别）
        self._save_ui_to_heading(self._current_heading_level)

        return profile

    # ================================================================
    # 操作按钮
    # ================================================================

    def _select_output_dir(self):
        folder = QFileDialog.getExistingDirectory(self, "选择排版后文件的输出目录")
        if folder:
            self.profile.output_dir = folder
            self._safe_set(self, "label_status", f"输出目录: {folder}", is_label=True)

    def _preview_config(self):
        """弹窗显示当前配置的 JSON 预览"""
        profile = self._collect_profile_from_ui()
        d = profile.to_dict()
        text = json.dumps(d, indent=2, ensure_ascii=False)

        from PyQt5.QtWidgets import QDialog, QVBoxLayout, QTextEdit, QDialogButtonBox
        dlg = QDialog(self)
        dlg.setWindowTitle("排版配置预览")
        dlg.resize(500, 500)
        layout = QVBoxLayout(dlg)

        te = QTextEdit()
        te.setReadOnly(True)
        te.setPlainText(text)
        layout.addWidget(te)

        btn_box = QDialogButtonBox(QDialogButtonBox.Ok)
        btn_box.accepted.connect(dlg.accept)
        layout.addWidget(btn_box)

        dlg.exec_()

    def _start_formatting(self):
        """开始排版"""
        if self._worker and self._worker.isRunning():
            QMessageBox.warning(self, "提示", "排版任务正在运行中")
            return

        self.profile = self._collect_profile_from_ui()

        # 过滤文件
        exts = []
        cb = getattr(self, "check_filter_docx", None)
        if cb and cb.isChecked(): exts.append(".docx")
        cb = getattr(self, "check_filter_doc", None)
        if cb and cb.isChecked(): exts.append(".doc")

        files_to_process = [f for f in self.file_paths if Path(f).suffix.lower() in exts]
        if not files_to_process:
            QMessageBox.warning(self, "提示", "请先选择要排版的 Word 文件")
            return

        out_info = (
            f"输出目录: {self.profile.output_dir}"
            if self.profile.output_dir
            else "输出目录: 源文件所在目录"
        )
        msg = (
            f"即将排版 {len(files_to_process)} 个文件。\n\n"
            f"排版后的文件将保存为 原文件名-Revise.docx，原文件不作任何修改。\n{out_info}\n确认继续？"
        )
        reply = QMessageBox.question(
            self, "确认排版", msg,
            QMessageBox.Yes | QMessageBox.No, QMessageBox.No
        )
        if reply != QMessageBox.Yes:
            return

        # 禁用开始按钮
        btn = getattr(self, "btn_start_format", None)
        if btn:
            btn.setEnabled(False)
            btn.setText("排版中...")

        # 启动工作线程
        self._worker = FormatWorker(files_to_process, self.profile)
        self._worker.progress_updated.connect(self._on_progress_updated)
        self._worker.status_message.connect(lambda m: self._safe_set(self, "label_status", m, is_label=True))
        self._worker.error_occurred.connect(self._on_worker_error)
        self._worker.all_finished.connect(self._on_all_finished)
        self._worker.start()

    # ================================================================
    # 后台线程回调（运行于主线程）
    # ================================================================

    def _on_progress_updated(self, current: int, total: int):
        """更新进度条"""
        if total > 0:
            pct = int(current / total * 100)
        else:
            pct = 0
        self._safe_set(self, "progress_bar", pct, is_progress=True)

    def _on_worker_error(self, err_msg: str):
        QMessageBox.critical(self, "错误", err_msg)
        self._safe_set(self, "label_status", "排版出错", is_label=True)
        btn = getattr(self, "btn_start_format", None)
        if btn:
            btn.setEnabled(True)
            btn.setText("▶ 开始排版")

    def _on_all_finished(self, results: list):
        """排版完成"""
        # 恢复按钮
        btn = getattr(self, "btn_start_format", None)
        if btn:
            btn.setEnabled(True)
            btn.setText("▶ 开始排版")

        ok_count = sum(1 for _, ok, _ in results if ok)
        total = len(results)
        summary = f"排版完成！成功: {ok_count}, 失败: {total - ok_count}"
        self._safe_set(self, "label_status", summary, is_label=True)
        self._safe_set(self, "progress_bar", 100, is_progress=True)

        # 显示结果窗口
        self._show_result_dialog(results, summary)

    def _show_result_dialog(self, results: list, summary: str):
        """排版结果对话框"""
        from PyQt5.QtWidgets import QDialog, QVBoxLayout, QTextEdit, QLabel, QDialogButtonBox

        dlg = QDialog(self)
        dlg.setWindowTitle("排版结果")
        dlg.resize(600, 400)

        layout = QVBoxLayout(dlg)

        lbl = QLabel(summary)
        lbl.setStyleSheet("font-size: 14px; font-weight: bold;")
        layout.addWidget(lbl)

        te = QTextEdit()
        te.setReadOnly(True)
        lines = []
        for fp, ok, msg in results:
            icon = "✓" if ok else "✗"
            lines.append(f"[{icon}] {msg}")
            if not ok:
                lines.append(f"     文件: {fp}")
        te.setPlainText("\n".join(lines))
        layout.addWidget(te)

        btn_box = QDialogButtonBox(QDialogButtonBox.Ok)
        btn_box.accepted.connect(dlg.accept)
        layout.addWidget(btn_box)

        dlg.exec_()

    # ================================================================
    # 依赖检查 & 错误提示
    # ================================================================

    def _check_deps(self):
        """检查依赖库"""
        missing = check_dependencies()
        if missing:
            msg = f"缺少依赖库: {', '.join(missing)}\n\n请运行: pip install {' '.join(missing)}"
            QMessageBox.warning(self, "依赖缺失", msg)

    def _show_ui_missing_error(self):
        """.ui 文件不存在时显示错误提示"""
        err = QMessageBox(self)
        err.setIcon(QMessageBox.Critical)
        err.setWindowTitle("界面文件缺失")
        err.setText(
            f"未找到 UI 界面文件:\n{UI_FILE}\n\n"
            f"请使用 Qt Designer 创建界面文件后保存到 ui/main_window.ui\n"
            f"控件命名规范请参考代码注释。"
        )
        err.exec_()