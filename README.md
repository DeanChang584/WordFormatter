# Word 文档排版工具

基于 Python 的 Word 文档批量排版工具，支持 `.doc` / `.docx` 双格式，兼容 Microsoft Office 和 WPS。

## 功能

- **页面设置** — 上下左右页边距（mm）
- **正文样式** — 中英文字体、字号、颜色、加粗/斜体
- **段落设置** — 行距（倍行距/固定值/最小值）、首行缩进、左右缩进、段前段后间距、对齐方式
- **标题样式** — 标题 1~6 级独立设置（字体、字号、颜色、对齐、缩进、行距、段前段后）
- **批量处理** — 选择文件或文件夹，一键处理
- **格式预设** — 内置三种预设：公文标准格式、学术论文格式、简约商务报告
- **`.doc` 兼容** — 通过 `win32com` 自动将 `.doc` 转为 `.docx` 后排版
- **安全输出** — 原文件不做修改，输出为 `原文件名-Revise.docx`

## 安装

```bash
pip install python-docx pywin32 PyQt5
```

> 仅处理 `.docx` 时无需 `pywin32`；`.doc` 文件则必须安装。

## 使用

### PyQt5 GUI（推荐）

```bash
python main_window.py
```

### Tkinter GUI（兼容备用）

```bash
python Word_Editor_tkinter.py
```

1. 导入 Word 文件（支持单选、多选、文件夹批量导入）
2. 调节页面 / 段落 / 正文 / 标题参数
3. 点击 **开始排版**

## 项目结构

```
├── models.py               # 数据模型：FormatProfile, PageConfig, BodyConfig 等
├── engine.py               # 排版引擎：docx 格式化、.doc 转换、批量处理
├── main_window.py          # PyQt5 主窗口，加载 ui/main_window.ui
├── worker.py               # QThread 后台工作线程
├── ui/main_window.ui       # Qt Designer 界面布局文件
├── Word_Editor_tkinter.py  # Tkinter 版本（兼容备用）
├── format_presets.json     # 预设格式配置
├── DESIGN.md               # Apple 设计规范参考（UI 灵感来源）
├── Word排版工具.spec       # PyInstaller 打包配置
├── requirements.txt        # 依赖列表
└── README.md
```

## 架构

```
┌─────────────────────────────────────┐
│           main_window.py            │
│   PyQt5 GUI / Qt Designer .ui       │
└──────────┬──────────────────────────┘
           │ signals / slots
┌──────────▼──────────┐   ┌──────────┐
│     worker.py        │──▶│ engine.py │
│  QThread 后台线程    │   │ 排版引擎  │
└─────────────────────┘   └────┬─────┘
                               │ 使用
                        ┌──────▼──────┐
                        │  models.py   │
                        │ 数据模型定义  │
                        └─────────────┘
```

- **models.py** — 纯数据层，定义 `FormatProfile`、`PageConfig`、`BodyConfig`、`ParagraphConfig`、`HeadingStyleConfig`，支持 JSON 序列化/反序列化
- **engine.py** — 核心排版引擎，处理 `.docx` 格式化、`.doc` → `.docx` 转换（COM）、批量处理
- **worker.py** — 继承 `QThread`，通过 Qt signals 向主线程报告进度、状态和结果
- **main_window.py** — PyQt5 主窗口，加载 Qt Designer 生成的 `.ui` 文件，连接信号/槽驱动排版

## 预设格式

| 预设 | 正文字体 | 字号 | 行距 | 适用场景 |
|---|---|---|---|---|
| 公文标准格式 | 仿宋 | 三号(16pt) | 固定 28pt | 政府公文、红头文件 |
| 学术论文格式 | 宋体 | 小四(12pt) | 1.5 倍行距 | 毕业论文、期刊投稿 |
| 简约商务报告 | 微软雅黑 | 五号(10.5pt) | 1.15 倍行距 | 商业报告、企划书 |

## 打包

```bash
pip install pyinstaller
pyinstaller Word排版工具.spec
```

## 技术栈

- `PyQt5` — GUI 框架（推荐），基于 Qt Designer `.ui` 文件
- `python-docx` — `.docx` 读写
- `win32com` — `.doc` 格式转换（调用 Word / WPS COM）
- `tkinter` — GUI 兼容备用
- `PyInstaller` — 打包为独立 exe

## License

MIT
