#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Word 文档排版工具 — 数据模型
定义页面、正文、段落、标题样式及其配置序列化。
"""

# ============================================================
# 中文字号映射（从大到小）
# ============================================================
FONT_SIZE_MAP = {
    "初号": 42, "小初": 36, "一号": 26, "小一": 24,
    "二号": 22, "小二": 18, "三号": 16, "小三": 15,
    "四号": 14, "小四": 12, "五号": 10.5, "小五": 9,
}
FONT_SIZE_NAMES = list(FONT_SIZE_MAP.keys())  # 从大到小已排列


def font_size_to_name(size_pt: float) -> str:
    """字号 pt → 中文名反向映射（浮点精度处理）"""
    for name, pt in FONT_SIZE_MAP.items():
        if abs(pt - size_pt) < 0.01:
            return name
    return str(size_pt)


# ============================================================
# 排版配置数据模型
# ============================================================

class PageConfig:
    """页面边距配置（单位：mm）"""
    def __init__(self):
        self.margin_top: float = 25.4
        self.margin_bottom: float = 25.4
        self.margin_left: float = 31.8
        self.margin_right: float = 31.8


class BodyConfig:
    """正文样式配置"""
    def __init__(self):
        self.font_cn: str = "宋体"
        self.font_en: str = "Times New Roman"
        self.font_size: float = 12.0
        self.font_color: str = "#000000"
        self.font_bold: bool = False
        self.font_italic: bool = False


class ParagraphConfig:
    """段落格式配置"""
    def __init__(self):
        self.line_spacing_mode: str = "multiple"       # "multiple" | "fixed" | "at_least"
        self.line_spacing_value: float = 1.5
        self.first_line_indent: float = 7.4             # 默认7.4mm(≈2字符)
        self.first_line_indent_unit: str = "mm"          # "mm" | "字符"
        self.left_indent: float = 0.0
        self.left_indent_unit: str = "字符"
        self.right_indent: float = 0.0
        self.right_indent_unit: str = "字符"
        self.alignment: str = "justify"
        self.space_before: float = 0.0
        self.space_before_unit: str = "行"               # "磅" | "行"
        self.space_after: float = 0.0
        self.space_after_unit: str = "行"


class HeadingStyleConfig:
    """标题样式配置（level 1-6）"""
    def __init__(self, level: int):
        self.level: int = level
        self.font_cn: str = "黑体"
        self.font_en: str = "Times New Roman"
        sizes = {1: 22, 2: 16, 3: 14, 4: 12, 5: 10.5, 6: 10.5}
        self.font_size: float = sizes[level]
        self.font_color: str = "#000000"
        self.font_bold: bool = True
        self.font_italic: bool = False
        self.alignment: str = "left"
        self.space_before: float = 1.0                   # 默认1行
        self.space_before_unit: str = "行"
        self.space_after: float = 1.0                    # 默认1行
        self.space_after_unit: str = "行"
        self.line_spacing_mode: str = "multiple"
        self.line_spacing_value: float = 1.5
        self.first_line_indent: float = 0.0              # 默认0字符
        self.first_line_indent_unit: str = "字符"


class FormatProfile:
    """一份完整的排版配置文件"""
    def __init__(self):
        self.page = PageConfig()
        self.body = BodyConfig()
        self.paragraph = ParagraphConfig()
        self.output_dir: str = ""                         # 自定义输出目录，空=源文件目录
        self.headings: dict[int, HeadingStyleConfig] = {}
        for i in range(1, 7):
            self.headings[i] = HeadingStyleConfig(i)

    def to_dict(self) -> dict:
        return {
            "page": self.page.__dict__.copy(),
            "body": self.body.__dict__.copy(),
            "paragraph": self.paragraph.__dict__.copy(),
            "headings": {str(k): v.__dict__.copy() for k, v in self.headings.items()},
        }

    @classmethod
    def from_dict(cls, d: dict):
        profile = cls()
        if "page" in d:
            for k, v in d["page"].items():
                setattr(profile.page, k, v)
        if "body" in d:
            for k, v in d["body"].items():
                setattr(profile.body, k, v)
        if "paragraph" in d:
            for k, v in d["paragraph"].items():
                setattr(profile.paragraph, k, v)
        if "headings" in d:
            for level_str, hd in d["headings"].items():
                level = int(level_str)
                for k, v in hd.items():
                    if k != "level":
                        setattr(profile.headings[level], k, v)
        return profile