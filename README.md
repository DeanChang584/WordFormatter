# Word Formatter

基于 Python 的 Word 文档批量排版工具，支持 `.doc` / `.docx` 双格式，兼容 Microsoft Office 和 WPS。原项目名称为 Word 文档排版工具，现更名为 Word Formatter。

## 功能

- **页面设置** — 上下左右页边距（mm）
- **正文样式** — 中英文字体、字号、颜色、加粗/斜体
- **段落设置** — 行距（倍行距/固定值/最小值）、首行缩进、左右缩进、段前段后间距、对齐方式
- **标题样式** — 标题 1~6 级独立设置（字体、字号、颜色、对齐、缩进、行距、段前段后）
- **批量处理** — 选择文件或文件夹，一键处理
- **`.doc` 兼容** — 通过 `win32com` 自动将 `.doc` 转为 `.docx` 后排版
- **安全输出** — 原文件不做修改，输出为 `原文件名-Revise.docx`

## 安装与运行

### 环境要求

- **操作系统**：Windows 10 / 11（`.doc` 转换依赖 COM 组件，仅限 Windows）
- **Python**：3.8 及以上
- **Office**（可选）：Microsoft Word 或 WPS（仅处理 `.doc` 文件时需要）

### 1. 克隆仓库

```bash
git clone git@github.com:DeanChang584/Word_Editor.git
cd Word_Editor
```

### 2. 创建虚拟环境（推荐）

```bash
python -m venv venv

# Windows
venv\Scripts\activate

# 激活后终端提示符前会出现 (venv) 标识
```

### 3. 安装依赖

**完整安装（支持 .doc + .docx）：**

```bash
pip install -r requirements.txt
```

**最小安装（仅 .docx）：**

```bash
pip install python-docx PyQt5
```

> `pywin32` 仅用于将 `.doc` 转为 `.docx`，需要本机安装 Microsoft Word 或 WPS。无需处理 `.doc` 文件可跳过。

### 4. 运行

**PyQt5 版本（推荐）：**

```bash
python main_window.py
```

**Tkinter 版本（兼容备用，无需 PyQt5）：**

```bash
python Word_Editor_tkinter.py
```

### 5. 操作步骤

1. 点击 **选择文件** 或 **选择文件夹** 导入 Word 文档
2. 调节左侧面板参数：页面边距、段落格式、正文字体、标题样式
3. 可选：从预设下拉框中选择一键套用格式
4. 点击 **开始排版**，等待进度条完成
5. 排版结果保存为 `原文件名-Revise.docx`，原文件不受影响

### 常见问题

| 问题 | 解决方案 |
|---|---|
| `ImportError: No module named 'PyQt5'` | 运行 `pip install PyQt5` |
| `.doc` 文件提示转换失败 | 确认已安装 `pywin32` 且本机装有 Word 或 WPS |
| `No module named 'docx'` | 运行 `pip install python-docx` |
| PyQt5 GUI 中文乱码 | 确认系统区域设置为中文（中国） |

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
