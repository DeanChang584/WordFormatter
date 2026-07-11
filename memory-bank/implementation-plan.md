# Word Formatter V2.0 — AI 开发者实施计划

> 基于 design-document.md、architecture.md、tech-stack.md、API.md、data model.md 生成
> 每条指令必须按顺序执行，完成验证后才能进入下一步

---

## 阶段 0：项目基础设施

### Step 0.1 — 创建目标目录结构

**指令**：按照 `architecture.md` 第 5 节定义，在项目根目录下创建全部目录。前端目录置于 `frontend/`，后端目录置于 `backend/`。随后立即将现有根目录的 `engine.py`、`models.py`、`worker.py` 迁移到 `backend/formatter/` 下（复制并修改导入路径为 `from backend.formatter.xxx import ...`），根目录旧文件归档或删除。

**验证**：(a) 运行 `tree /F` 确认所有目录存在，预期包含以下目录：`frontend/Views`、`frontend/ViewModels`、`frontend/Services`、`frontend/Models`、`frontend/Controls`、`frontend/Assets`、`frontend/Styles`、`frontend/Resources`、`backend/api`、`backend/services`、`backend/formatter`、`backend/templates`、`backend/history`、`backend/preview`、`backend/config`、`backend/utils`、`shared`、`docs`、`tests`、`logs`、`cache`、`temp`、`output`、`scripts`。

---

### Step 0.2 — 创建共享数据模型文件

**指令**：在 `shared/` 目录下创建以下文件，内容为空或占位：`schemas.py`（DTO 定义）、`constants.py`（字号映射等常量）、`version.py`（版本号 `"2.0"`）。注意 `schemas.py` 使用 `data model.md` 定义的 camelCase 命名，所有字段类型为 Python dataclass。

**验证**：运行 `python -c "from shared import schemas"`，确认无 ImportError。

---

### Step 0.3 — 创建后端日志模块

**指令**：在 `backend/utils/` 下创建 `logger.py`。该模块提供 `get_logger(name, category="backend")` 函数，返回配置好的 `logging.Logger` 实例。采用**按用途分文件 + 按日期自动归档**：

- 活动文件位于 `logs/` 根目录，按用途分为四个：`app.log`（前端/UI）、`backend.log`（API、Service）、`format.log`（排版任务）、`error.log`（所有 ERROR+ 异常，跨分类汇总）。`category` 参数（app/backend/format/error）决定主分类文件；无论哪个分类，级别 ≥ ERROR 的记录都额外写入 `error.log`。
- 按日期归档 + 保留策略：使用 `logging.handlers.TimedRotatingFileHandler`（`when="midnight"`），午夜滚动后在 `logs/` 根目录留下 `{分类}-YYYY-MM-DD.log`（通过自定义 `namer`）。两级保留（`purge_old_logs()`，首次取 logger 时执行一次、每次滚动后再执行一次）：根目录每日文件超过 **30 天**移入 `logs/archive/`；`logs/archive/` 中超过 **90 天**的自动删除。用户无需手动清理。
- 日志级别 INFO，格式 `[时间] [级别] [模块名] 消息内容`。同时输出到控制台便于开发。`get_logger` 对同名调用幂等（不重复挂载 handler）。同时创建 `logs/` 与 `logs/archive/` 目录。

**验证**：运行 Python 脚本，分别用 `category="app"/"backend"/"format"` 调用 `get_logger` 并写 INFO 日志，另写一条 ERROR 日志；检查 `logs/` 下生成对应的 `app.log`/`backend.log`/`format.log`，且 ERROR 同时出现在其分类文件与 `error.log`。手动触发一次 `doRollover()`，确认历史内容归档为 `logs/{分类}-YYYY-MM-DD.log`。构造 45 天前的每日文件与 120 天前的归档文件后调用 `purge_old_logs()`，确认前者被移入 `logs/archive/`、后者被删除、活动文件不受影响。

---

### Step 0.4 — 初始化 Git 并更新 .gitignore

**指令**：确保项目根目录存在 `.gitignore`，包含以下排除项：`__pycache__/`、`*.pyc`、`logs/`、`cache/`、`temp/`、`output/`、`*.zip`、`.vscode/`、`.idea/`、`frontend/bin/`、`frontend/obj/`。如果 `.gitignore` 不存在则创建。

**验证**：运行 `git status`，确认 `logs/`、`cache/`、`temp/`、`output/` 不会出现在 untracked 文件列表中。

---

## 阶段 1：后端核心 — 数据层 + API 框架

### Step 1.1 — 创建后端应用入口

**指令**：创建 `backend/server.py`。内容：初始化 FastAPI 应用，配置 CORS 允许所有来源，注册 `/api/health` 路由。应用标题设为 `"Word Formatter API"`，版本 `"2.0"`。监听端口 8765。同时创建 `backend/api/__init__.py` 和 `backend/api/health.py`。

**验证**：运行 `python -m uvicorn backend.server:app --port 8765`，浏览器或 curl 访问 `http://127.0.0.1:8765/api/health`。预期返回 JSON：`{"success": true, "code": 0, "message": "OK", "data": {"status": "ok", "version": "2.0"}}`。

---

### Step 1.2 — 创建统一响应格式工具

**指令**：在 `backend/utils/` 下创建 `response.py`。提供两个函数：`success_response(data=None, message="OK")` 返回 `{success: true, code: 0, message, data}`；`error_response(code, message, http_status=400)` 返回对应的错误格式。错误码枚举按 `API.md` 第 4 节定义（1000-1010）。

**验证**：编写一个临时测试路由，分别调用 `success_response` 和 `error_response`，curl 验证返回 JSON 结构与 `API.md` 第 3 节一致。

---

### Step 1.3 — 创建后端配置管理模块

**指令**：创建 `backend/config/` 目录和 `backend/config/manager.py`。该模块负责加载和保存软件全局设置，支持默认值，使用 JSON 文件存储。创建 `backend/config/defaults.py` 存放所有默认值（按 `data model.md` 第 11 节定义）。设置文件路径为 `config/settings.json`。

**验证**：编写临时脚本：调用 `get_setting("theme")` 应返回默认值 `"system"`；调用 `update_settings({"theme": "dark"})` 后再读取应返回 `"dark"`；检查 `config/settings.json` 文件内容正确。然后删除临时脚本。

---

### Step 1.4 — 创建共享 DTO 定义

**指令**：填充 `shared/schemas.py`。按 `data model.md` 定义所有数据对象的 Python dataclass：`FileItem`、`PageConfig`、`HeaderFooterConfig`、`BodyConfig`、`HeadingStyleConfig`、`PictureConfig`、`TableConfig`、`ProfileConfig`、`Template`、`Task`、`TaskResult`、`PreviewResult`、`HistoryRecord`、`Settings`、`LogEntry`。所有字段命名使用 camelCase。每个类添加 `to_dict()` 和 `from_dict()` 方法。后端 API 使用 Pydantic 模型（含 `alias_generator` 将内部 snake_case 字段自动转为 camelCase JSON 输出），前端使用 `JsonNamingPolicy.CamelCase`。

**验证**：运行 Python 脚本：实例化 `ProfileConfig`，调用 `to_dict()` 输出 JSON，确认字段名全为 camelCase。用 `from_dict()` 回读，确认字段值一致。

---

### Step 1.5 — 创建错误码定义

**指令**：在 `shared/constants.py` 中添加 `API.md` 第 4.1 节定义的全部错误码（1000-1010），以及任务状态枚举（idle/preparing/running/saving/completed/failed/cancelled）、文件处理状态枚举（waiting/running/done/error）。同时迁移现有的中文字号映射 `FONT_SIZE_MAP` 到此文件。

**验证**：运行 `python -c "from shared.constants import ErrorCode; print(ErrorCode.FILE_NOT_FOUND)"`，预期输出 `1001`。

---

### Step 1.6 — 创建文件管理 API

**指令**：创建 `backend/api/files.py`。实现 `API.md` 第 6 节定义的全部 8 个端点：获取文件列表、添加文件、添加文件夹（支持 `include_subdir` 参数）、移除文件、清空文件列表、搜索文件（关键词过滤）、获取最近打开记录、固定常用目录。文件列表使用内存存储（后续迁移到 SQLite）。验证请求参数，文件路径不存在时返回错误码 1001。所有响应使用 `response.py` 统一格式。路由前缀 `/api/files`。

**验证**：启动后端，用 curl 逐个测试 8 个端点：1) GET /files 返回空列表 → 2) POST /files/add 添加两个文件 → 3) GET /files 确认包含 2 个文件 → 4) POST /files/add-folder 导入文件夹 → 5) POST /files/remove 移除一个 → 6) POST /files/search 搜索关键词 → 7) GET /files/recent → 8) DELETE /files 清空。每个请求验证响应 HTTP 状态码和 JSON 结构。

---

### Step 1.7 — 创建排版配置 API

**指令**：创建 `backend/api/profile.py`。实现 `API.md` 第 7 节定义的 3 个端点：获取当前配置、更新配置、恢复默认配置。配置数据使用 `shared/schemas.py` 中的 `ProfileConfig` 类型。默认值从 `config/defaults.py` 加载。路由前缀 `/api/profile`。

**验证**：1) GET /profile 返回完整 ProfileConfig JSON（包含 page/header_footer/body/heading/image/table 六个子对象）→ 2) PUT /profile 更新 body.fontCn 为 "仿宋" → 3) GET /profile 确认 fontCn 已变为 "仿宋" → 4) POST /profile/reset → 5) GET /profile 确认恢复为默认值 "宋体"。

---

### Step 1.8 — 创建模板管理 API

**指令**：创建 `backend/api/templates.py`。实现 `API.md` 第 8 节定义的 7 个端点：获取模板列表、保存模板、更新模板、删除模板（默认模板不可删除）、导入模板（从 JSON 文件读取并验证版本号）、导出模板（写入 JSON 文件）、设置为默认模板。模板数据存储到 `config/templates/` 目录下的 JSON 文件。路由前缀 `/api/templates`。

**验证**：1) GET /templates 返回预置模板列表（至少含"默认模板"和"日常写作模板"）→ 2) POST /templates 创建新模板 → 3) GET /templates 确认新模板出现 → 4) PUT /templates/{id} 更新名称 → 5) 尝试 DELETE 默认模板，确认返回拒绝 → 6) POST /templates/export 导出模板，检查生成 JSON 文件 → 7) POST /templates/import 导入任一 JSON，确认成功。

---

### Step 1.9 — 创建排版任务 API

**指令**：创建 `backend/api/format.py`。实现 `API.md` 第 9 节定义的 4 个端点：启动排版、查询任务状态、取消任务、获取任务结果。任务管理使用内存字典（key 为 task_id）。每个任务启动独立后台线程执行排版。进度更新通过任务状态字典完成，前端轮询获取。路由前缀 `/api/format`。

**验证**：1) POST /format/start 传入两个文件路径和默认 profile → 返回 202 和 task_id → 2) GET /format/status/{task_id} 返回 running/completed → 3) 在任务运行中 POST /format/cancel → 确认状态变为 cancelled → 4) GET /format/result/{task_id} 返回成功/失败统计。预期至少有一个文件处理成功（如果是有效的 .docx 文件）。

---

## 阶段 2：排版引擎重构

### Step 2.1 — 迁移现有排版代码到规则引擎架构

**指令**：将项目根目录的 `engine.py`、`models.py`、`worker.py` 迁移到 `backend/formatter/` 目录下，按以下结构拆分：
- `backend/formatter/engine.py` — 排版主控逻辑（`format_docx`、`convert_doc_to_docx`、`process_file`）
- `backend/formatter/models.py` — 数据模型（`FormatProfile`、`PageConfig` 等旧模型，后续逐步替换为 `shared/schemas.py`）
- `backend/formatter/page.py` — 页面规则（纸张大小、边距、方向、页码）
- `backend/formatter/paragraph.py` — 段落规则（行距、缩进、对齐）
- `backend/formatter/heading.py` — 标题规则（1-6 级独立配置）
- `backend/formatter/image.py` — 图片规则（占位，后续实现）
- `backend/formatter/table.py` — 表格规则（占位，后续实现）
- `backend/formatter/header_footer.py` — 页眉页脚规则（占位，后续实现）
- `backend/formatter/font.py` — 字体工具函数（设置中英文字体、字号、颜色）

原有根目录的 `engine.py`、`models.py`、`worker.py` 迁移后删除。

**验证**：运行现有排版逻辑（如 `process_file` 处理一个 .docx 文件），确认输出文件与原代码行为一致：输出文件名为 `原文件名-R.docx`，文件大小 > 0，用 Word 可正常打开。

---

### Step 2.2 — 对齐排版配置数据结构

**指令**：修改 `backend/formatter/engine.py` 的输入参数，使其接受 `shared/schemas.py` 中定义的 `ProfileConfig` 对象（camelCase 字段），而非旧的 `models.py` 中的对象。在 `engine.py` 中添加转换层：读取 `ProfileConfig` 各字段并映射到实际的排版操作。

**验证**：创建 `ProfileConfig` 实例，设置 `body.fontCn="仿宋"`、`body.fontSize=16`、`body.lineSpacing=1.5`，调用排版函数处理一个 .docx 文件。用 python-docx 检查输出文件的 Normal 样式：预期中文字体为"仿宋"、字号 16pt、行距 1.5 倍。

---

### Step 2.3 — 实现页面设置规则

**指令**：填充 `backend/formatter/page.py`。实现以下规则函数：`apply_page_setup(section, page_config)` 设置纸张大小、方向（portrait/landscape）、上下左右边距，参数来自 `PageConfig`。支持自定义纸张大小时交换宽高。

**验证**：调用排版函数处理一个 .docx 文件，传入 `PageConfig(paperSize="A4", orientation="landscape", marginTop=20, marginBottom=20)`。用 python-docx 检查输出文件的 section：预期页宽 > 页高（横向）、边距均为 20mm。

---

### Step 2.4 — 实现段落与标题排版规则

**指令**：填充 `backend/formatter/paragraph.py` 和 `backend/formatter/heading.py`。段落规则覆盖：行距（倍数/固定值/最小值）、首行缩进（字符/mm）、对齐方式、段前段后间距。标题规则覆盖：1-6 级独立字体/字号/字形/对齐/间距，通过修改 Word 的 Heading 1-6 样式实现。

**验证**：排版测试文件，分别验证：1) 正文段落首行缩进 2 字符 → 2) 正文行距 1.5 倍 → 3) Heading 1 字体为"黑体"、字号 16pt、加粗、居中对齐 → 4) Heading 2 为独立配置。用 python-docx 逐个检查。

---

## 阶段 3：Service 层

### Step 3.1 — 创建文件管理 Service

**指令**：创建 `backend/services/file_service.py`。从 `backend/api/files.py` 中提取业务逻辑到此类：文件列表维护、添加/移除/清空/搜索、文件夹扫描、最近打开记录、固定目录管理。API 路由仅做参数校验和调用 Service，不含业务逻辑。

**验证**：启动后端，通过 API 端到端测试：添加文件 → 搜索 → 移除 → 清空。所有操作与 Step 1.6 的验证结果一致。确认 `backend/api/files.py` 中没有直接操作 `_files` 字典的逻辑。

---

### Step 3.2 — 创建排版任务 Service

**指令**：创建 `backend/services/format_service.py`。从 `backend/api/format.py` 中提取业务逻辑：任务创建、状态管理、进度更新、结果汇总。实现任务队列类，支持 pending → running → completed/failed/cancelled 状态转换。

**验证**：启动后端，通过 API 创建 3 个排版任务，检查每个任务的状态转换是否正确（从 pending 到 completed）。确认取消任务后后续文件不再处理。

---

### Step 3.3 — 创建模板管理 Service

**指令**：创建 `backend/services/template_service.py`。从 `backend/api/templates.py` 中提取业务逻辑：模板增删改查、JSON 文件读写、版本号验证、导入导出。添加默认模板数据（"默认模板"和"日常写作模板"）的初始化逻辑。

**验证**：通过 API 端到端测试全部 7 个模板端点，与 Step 1.8 验证结果一致。确认 `backend/api/templates.py` 只做路由和参数校验。

---

## 阶段 4：历史记录 + 预览

### Step 4.1 — 实现历史记录模块

**指令**：创建 `backend/history/manager.py` 和 `backend/api/history.py`。历史记录持久化到 `config/history/` 下的 JSON 文件（每条记录一个文件）。实现 `API.md` 第 11 节定义的 3 个端点：获取最近任务列表（最近 20 条）、获取任务详情、清空历史。排版任务完成后自动写入历史记录。

**验证**：1) 执行一次排版任务 → 2) GET /history 确认记录出现（含时间、模板名、成功/失败数）→ 3) GET /history/{id} 获取完整详情 → 4) DELETE /history 清空 → 5) GET /history 确认已清空。

---

### Step 4.2 — 实现参数摘要预览（Level 1）

**指令**：创建 `backend/preview/generator.py` 和 `backend/api/preview.py`。实现 `API.md` 第 10 节和 `design-document.md` 第 13 节 Level 1：根据当前 `ProfileConfig` 生成参数摘要文本。格式如："【页面】A4 纵向，上边距 25.4mm，下边距 25.4mm\n【正文】宋体 小四，行距 1.5 倍，首行缩进 2 字符\n..."。

**验证**：POST /preview 传入文件路径和默认 profile → 返回 JSON 中的 `data.preview` 字段为非空字符串，内容包含"页面""正文""标题"三个关键词。

---

## 阶段 5：前端基础搭建

### Step 5.1 — 初始化 WinUI3 项目

**指令**：在 `frontend/` 目录下使用 `dotnet new` 创建 WinUI3 项目（空白应用，打包）。项目名 `WordFormatterUI`。添加 NuGet 包：`CommunityToolkit.Mvvm`（8.x）。配置 `Package.appxmanifest` 和 `.csproj`：目标框架 `net9.0-windows10.0.26100.0`，`WindowsPackageType=None`（非打包应用）。

**验证**：运行 `dotnet build`，确认 0 错误。运行 `dotnet run`，确认显示空白 WinUI3 窗口。

---

### Step 5.2 — 创建 ApiService（HTTP 客户端）

**指令**：在 `frontend/Services/` 下创建 `ApiService.cs`。封装 `HttpClient`，基地址 `http://127.0.0.1:8765/api`。提供对应所有 API 端点的方法：`GetHealthAsync()`、`GetFilesAsync()`、`AddFilesAsync(paths)`、`AddFolderAsync(folder, includeSubdir)`、`RemoveFilesAsync(paths)`、`ClearFilesAsync()`、`SearchFilesAsync(keyword)`、`GetRecentFilesAsync()`、`PinFolderAsync(folder)`、`GetProfileAsync()`、`UpdateProfileAsync(profile)`、`ResetProfileAsync()`、`GetTemplatesAsync()`、`SaveTemplateAsync(...)`、`UpdateTemplateAsync(...)`、`DeleteTemplateAsync(id)`、`ImportTemplateAsync(path)`、`ExportTemplateAsync(id, path)`、`SetDefaultTemplateAsync(id)`、`StartFormatAsync(files, profile, outputDir)`、`GetFormatStatusAsync(taskId)`、`CancelFormatAsync(taskId)`、`GetFormatResultAsync(taskId)`、`GetPreviewAsync(file, profile)`、`GetHistoryAsync()`、`GetHistoryDetailAsync(id)`、`ClearHistoryAsync()`、`GetSettingsAsync()`、`UpdateSettingsAsync(settings)`。使用 `System.Text.Json`，`JsonNamingPolicy.CamelCase` 命名策略（对齐 Q1 决议：JSON 统一 camelCase）。

**验证**：启动后端，在 `MainWindow` 的 Loaded 事件中调用 `GetHealthAsync()`，确认返回 `success: true`。将健康状态显示在临时 TextBlock 上。

---

### Step 5.3 — 创建前端 DTO 类

**指令**：在 `frontend/Models/` 下创建与 `shared/schemas.py` 对应的 C# 类：`FileItem`、`PageConfig`、`HeaderFooterConfig`、`BodyConfig`、`HeadingStyleConfig`、`PictureConfig`、`TableConfig`、`ProfileConfig`、`Template`、`TaskInfo`、`TaskResult`、`PreviewResult`、`HistoryRecord`、`Settings`。使用 `JsonNamingPolicy.CamelCase` 自动将 C# PascalCase 属性映射为 JSON camelCase（与后端 Pydantic alias 保持一致）。添加统一响应包装类 `ApiResponse<T>`（含 `success`、`code`、`message`、`data` 字段）。

**验证**：编写临时单元测试：从 JSON 字符串反序列化为 `ApiResponse<ProfileConfig>`，确认 `data.body.fontCn` 能正确读取。然后删除临时测试。

---

## 阶段 6：前端 UI — 主窗口框架

### Step 6.1 — 创建三栏主窗口布局

**指令**：修改 `MainWindow.xaml`，实现 `design-document.md` 第 3 节和 `architecture.md` 第 7 节定义的三栏布局。使用 `Grid` 作为根容器：
- 行 0（高度 Auto）：标题栏区域（32px）
- 行 1（高度 *）：三栏内容区。左栏宽度 200px（导航），中栏宽度按比例（配置区），右栏宽度 360px（文件与排版）。三栏之间使用 `GridSplitter` 可拖拽调整。
- 行 2（高度 Auto）：状态栏（32px）

**验证**：运行应用，确认显示三栏布局窗口。窗口尺寸 1280×800。标题栏和状态栏可见（内容暂为空）。拖动分隔条确认宽度可变。

---

### Step 6.2 — 实现标题栏

**指令**：创建 `frontend/Controls/TitleBar.xaml` 和 `TitleBar.xaml.cs`。按 `design-document.md` 第 4 节：左侧显示应用图标（32×32，使用项目中的图标资源），不显示软件名称；中部显示页面标签（如"Word Formatter"），居中对齐；右侧三个按钮：最小化、最大化/还原、关闭。标题栏高度 32px，背景跟随系统主题。支持窗口拖动（通过标题栏区域拖拽移动整个窗口）。

**验证**：运行应用，确认标题栏显示图标和标签。点击最小化 → 窗口最小化。点击最大化 → 窗口全屏，按钮变为还原图标。点击关闭 → 应用退出。拖动标题栏空白区域 → 窗口移动。

---

### Step 6.3 — 实现左侧导航栏

**指令**：创建 `frontend/Controls/NavBar.xaml` 和 `NavBar.xaml.cs`。按 `design-document.md` 第 5 节：使用 `ListView` 或 `NavigationView`，列出 8 个导航项，每项显示图标和文字。导航项包括：页面设置、正文样式、标题样式、页眉页脚、图片设置、表格设置、高级设置、关于软件。每个导航项带图标（使用 Segoe Fluent Icons 字体）。当前选中项高亮（左侧蓝色指示条 + 背景色变化）。支持键盘快捷键 Ctrl+1~8 切换。窗口缩窄时自动折叠为仅图标模式，hover 展开文字标签。

**验证**：运行应用，确认 8 个导航项可见。点击"正文样式" → 该项高亮。按 Ctrl+3 → 切换到"标题样式"。缩窄窗口宽度至 760px → 导航栏折叠为仅图标。鼠标悬停 → 展开显示文字。

---

### Step 6.4 — 实现状态栏

**指令**：创建 `frontend/Controls/StatusBar.xaml` 和 `StatusBar.xaml.cs`。按 `design-document.md` 第 8 节：高度 32px，左侧显示状态文本，右侧显示文件计数、模板名称、版本号。状态文本随任务状态变化：就绪、扫描中、排版中（含进度）、完成、失败、错误。绑定 ViewModel 的属性。

**验证**：运行应用，确认状态栏显示"Ready | 已加载 0 个文件 | 模板：默认 | v2.0"。通过 ViewModel 修改状态文本为"处理中... (5/10)" → 状态栏实时更新。

---

## 阶段 7：前端 UI — 中间配置区域

### Step 7.1 — 创建基础配置卡片框架

**指令**：创建 `frontend/Controls/ConfigCard.xaml` 和 `ConfigCard.xaml.cs`。可复用的配置卡片控件：包含标题、内容区域（可插入任意子控件）、底部操作按钮区域（"保存为模板""重置为默认值"）。卡片圆角 8px，背景色跟随主题（浅色 #FFFFFF，深色 #2D2D2D）。

**验证**：在 MainWindow 中栏放置一个 `ConfigCard`，标题设为"页面设置"，内容区放一个 TextBlock。确认圆角、背景色、标题正确显示。

---

### Step 7.2 — 创建页面设置配置界面

**指令**：创建 `frontend/Views/PageSettingsView.xaml` 和 `PageSettingsView.xaml.cs`。按 `design-document.md` 第 9.1 节：纸张大小（ComboBox：A4/A3/B5/Letter/Legal/自定义）、页面方向（RadioButtons：纵向/横向）、上下左右边距（NumberBox + 单位 ComboBox：mm/cm/inch）、页码样式（ComboBox：无/顶部居中/底部居中）。所有输入项格式：[标签] [输入框] [单位选择器]。输入框高度 32px。绑定到 `PageSettingsViewModel`。

**验证**：运行应用，点击"页面设置"导航项 → 中栏显示页面设置表单。修改纸张大小为"A3" → 确认 ComboBox 选中项变化。修改上边距为 30mm → 确认 NumberBox 显示 30。切换到其他导航项再切回 → 确认之前的值保留。点击"重置为默认值" → 确认恢复默认参数。

---

### Step 7.3 — 创建正文样式配置界面

**指令**：创建 `frontend/Views/BodyStyleView.xaml` 和 `BodyStyleView.xaml.cs`。按 `design-document.md` 第 9.3 节：中文字体（ComboBox）、西文字体（ComboBox）、字号（ComboBox：五号/小四/四号/小三/三号/小二/二号/一号）、字形（多选：加粗/倾斜/下划线/删除线）、对齐方式（ComboBox：左/中/右/两端）、行距（ComboBox：单倍/1.5倍/2倍/固定值/最小值 + 值输入框）、段间距（段前 NumberBox + 单位、段后 NumberBox + 单位）、缩进（首行缩进 NumberBox + 单位）。绑定到 `BodyStyleViewModel`。

**验证**：导航到"正文样式" → 确认所有控件可见。修改中文字体为"仿宋" → 确认。修改字号为"三号" → 确认。修改行距为"固定值 28pt" → 确认值输入框联动出现。

---

### Step 7.4 — 创建标题样式配置界面

**指令**：创建 `frontend/Views/HeadingStyleView.xaml` 和 `HeadingStyleView.xaml.cs`。按 `design-document.md` 第 9.4 节：顶部 ComboBox 选择标题级别（标题一至标题六）。下方显示该级标题的配置：中文字体、西文字体、字号、字形、对齐方式、段前间距、段后间距、行距。切换级别时保留已修改的未保存状态，并在 ComboBox 旁显示"●"标记。绑定到 `HeadingStyleViewModel`。

**验证**：导航到"标题样式" → 默认显示标题一的配置（黑体 二号 加粗）。切换到标题二 → 显示标题二的默认配置。修改标题一的字号为 18pt → 切换到标题二 → 标题二不变 → 切回标题一 → 字号仍是 18pt。

---

### Step 7.5 — 创建页眉页脚配置界面

**指令**：创建 `frontend/Views/HeaderFooterView.xaml` 和 `HeaderFooterView.xaml.cs`。按 `design-document.md` 第 9.2 节：中文字体、西文字体、字号、字形（加粗/倾斜/下划线）、对齐方式（左/中/右）、页眉距顶部距离（mm）、页脚距底部距离（mm）。绑定到 `HeaderFooterViewModel`。

**验证**：导航到"页眉页脚" → 确认所有控件可见。修改页眉字体为"黑体"、字号 10pt、居中 → 确认。修改页眉边距为 20mm → 确认。

---

### Step 7.6 — 创建图片设置和表格设置界面

**指令**：创建 `frontend/Views/PictureSettingsView.xaml` 和 `frontend/Views/TableSettingsView.xaml`。按 `design-document.md` 第 9.5/9.6 节：图片设置（统一宽度 + 单位、对齐方式、保持纵横比勾选框），表格设置（整体居中勾选框、自动分页勾选框、表头字体/字号、边框样式 ComboBox）。绑定到对应 ViewModel。标记为"（待实现）"，因为排版引擎尚未支持。

**验证**：导航到"图片设置" → 确认控件可见，但功能标记为"即将推出"。同样检查"表格设置"。

---

### Step 7.7 — 创建高级设置和关于界面

**指令**：创建 `frontend/Views/AdvancedSettingsView.xaml` 和 `frontend/Views/AboutView.xaml`。高级设置：界面语言（ComboBox：中文/English）、主题（RadioButtons：浅色/深色/跟随系统，绑定 ThemeService）、自动检查更新（ToggleSwitch）。关于：软件名称、版本号 v2.0、GitHub 链接、版权信息。

**验证**：切换到深色主题 → 确认窗口背景和控件颜色变为深色。打开关于页面 → 确认版本号显示 v2.0、GitHub 链接可点击。

---

## 阶段 8：前端 UI — 右侧工作区域

### Step 8.1 — 创建文件管理卡片

**指令**：在 MainWindow 右栏上部创建文件管理区域。按 `design-document.md` 第 7.1 节：顶部工具栏（添加文件按钮、添加文件夹按钮、搜索框）、文件列表（ListView，显示文件名/路径/大小，支持 Ctrl/Shift 多选）、底部操作区（全选、反选、删除选中、清空列表按钮）、文件计数（"已选 N 个文件"）。支持最近打开下拉菜单（列出最近 10 条记录）和固定目录列表。支持从资源管理器拖拽文件到本区域。

**验证**：点击"添加文件" → 打开文件选择器 → 选择 3 个 .docx 文件 → 列表中显示 3 个文件名。点击"添加文件夹" → 选择目录 → 递归扫描 .doc/.docx。Ctrl+A 全选 → 点击"删除选中" → 列表清空。搜索框输入关键词 → 列表实时过滤。从桌面拖一个 .docx 文件到本区域 → 自动添加到列表。

---

### Step 8.2 — 创建排版控制卡片

**指令**：在 MainWindow 右栏中部创建排版控制区域。按 `design-document.md` 第 7.2 节：输出目录（TextBox + 浏览按钮）、模板选择（ComboBox 列出所有已保存模板）、"开始排版"按钮（强调色，点击后变为"取消任务"）、"预览"按钮（生成参数摘要）、进度条（ProgressBar + 百分比文本 + 当前文件名）、成功/失败计数（"成功 12 | 失败 0"）、"重试失败文件"按钮（任务完成后若有失败项显示）。

**验证**：选择模板 → ComboBox 显示已保存模板。点击"预览" → 状态栏显示参数摘要弹窗。点击"开始排版" → 按钮变为"取消任务"，进度条出现并更新。任务完成后 → 显示成功/失败统计。若有失败文件 → 显示"重试失败文件"按钮并可用。

---

### Step 8.3 — 创建结果与历史卡片

**指令**：在 MainWindow 右栏下部创建结果与历史区域。按 `design-document.md` 第 7.3 节：任务完成后自动展开结果摘要（"任务完成 | 成功 N | 失败 M | 耗时 Xs"）、"打开输出文件夹"按钮（调用 `System.Diagnostics.Process.Start` 打开资源管理器）、"查看失败文件"按钮（展开失败文件列表含错误详情）、历史记录入口（点击展开最近 20 条记录，每条显示时间/模板/成功/失败，可点击"重新执行"复用配置）。

**验证**：执行排版任务 → 结果区显示摘要。点击"打开输出文件夹" → 资源管理器弹出。点击"查看失败文件" → 展开列表显示错误原因。展开历史记录 → 确认最近任务出现在列表中。

---

## 阶段 9：MVVM 绑定与状态管理

### Step 9.1 — 创建主窗口 ViewModel

**指令**：创建 `frontend/ViewModels/MainViewModel.cs`。使用 `CommunityToolkit.Mvvm` 的 `ObservableObject` 基类和 `[ObservableProperty]` 属性。包含以下属性：`CurrentNavIndex`（当前选中的导航项索引 0-7）、`StatusText`（状态栏文本）、`FileCount`（文件数量）、`TemplateName`（当前模板名）、`IsFormatting`（是否正在排版）、`ProgressPercent`（进度百分比）、`ProgressText`（当前文件显示文本）、`OkCount`（成功数）、`FailCount`（失败数）。包含以下 Command：切换导航项、开始排版、取消排版、预览。

**验证**：运行应用，在 MainWindow 构造函数中创建 `MainViewModel` 实例并设为 DataContext。修改 `StatusText` → 确认状态栏文本实时更新。修改 `IsFormatting` 为 true → 确认排版按钮变为"取消任务"。

---

### Step 9.2 — 创建各页面独立 ViewModel

**指令**：创建以下 ViewModel 文件，均使用 `CommunityToolkit.Mvvm`：
- `PageSettingsViewModel.cs` — 页面设置所有参数属性 + `ResetCommand` + `SaveAsTemplateCommand`
- `BodyStyleViewModel.cs` — 正文样式所有参数属性 + `ResetCommand`
- `HeadingStyleViewModel.cs` — 标题样式所有参数属性 + 当前级别切换 + `ResetCommand`
- `HeaderFooterViewModel.cs` — 页眉页脚所有参数属性 + `ResetCommand`
- `PictureSettingsViewModel.cs` — 图片设置属性
- `TableSettingsViewModel.cs` — 表格设置属性
- `FilesViewModel.cs` — 文件列表、搜索关键词、最近打开、固定目录 + `AddFilesCommand`、`AddFolderCommand`、`RemoveSelectedCommand`、`ClearAllCommand`、`SearchCommand`
- `FormatViewModel.cs` — 输出目录、选中的模板、任务状态、进度、结果 + `StartCommand`、`CancelCommand`、`PreviewCommand`、`RetryFailedCommand`
- `HistoryViewModel.cs` — 历史记录列表 + `LoadCommand`、`ClearCommand`、`ReuseCommand`
- `SettingsViewModel.cs` — 语言、主题、自动更新 + `SaveCommand`

**验证**：每个 ViewModel 编译通过。在各自的 View 中绑定 DataContext，修改属性值 → 确认 UI 实时更新。

---

### Step 9.3 — 绑定配置区域 ViewModel 与 API

**指令**：在 `MainViewModel` 中维护一个 `ProfileConfig` 实例作为全局排版配置。当用户在任一配置页面修改参数时，实时同步到该实例（通过各子 ViewModel 的 PropertyChanged 事件）。点击"开始排版"时，将 `ProfileConfig` 序列化后发送到后端 API。排版完成后不自动清空配置。

**验证**：在正文样式页面修改字体为"楷体"，切换到页面设置页面，修改纸张为 A3，切换回正文样式 → 字体仍是"楷体"。点击"开始排版" → 查看后端日志/抓包确认请求 body 中包含 `body.fontCn="楷体"` 和 `page.paperSize="A3"`。

---

### Step 9.4 — 实现配置修改标记

**指令**：在每个子 ViewModel 中添加 `IsDirty` 属性。当任何参数被修改时设为 true。切换到其他导航项时，若当前项 `IsDirty` 为 true，弹出提示"是否保存对【模块名】的修改？"，选项为"保存""放弃""取消"。选择"保存" → 更新全局 `ProfileConfig` 并清除标记；"放弃" → 恢复默认值；"取消" → 留在当前页面。

**验证**：修改正文样式字体 → 导航切换到"页面设置" → 弹出提示对话框。点击"保存" → 切换到"页面设置"。再次切回"正文样式" → 检查值已保存。

---

## 阶段 10：集成测试与联调

### Step 10.1 — 后端全接口集成测试

**指令**：在 `tests/` 目录下创建 `test_api.py`。使用 `pytest` + `httpx.AsyncClient`。测试覆盖全部 30+ 个 API 端点，按 `API.md` 验证每个端点的请求/响应格式。包括：正常路径（200/201/202）、错误路径（400/404）、边界条件（空文件列表、无效模板 ID）。

**验证**：启动后端，运行 `pytest tests/test_api.py -v`。预期所有测试通过（至少每个端点一个 happy path 测试通过）。失败测试不超过端点数 的 20%（新端点允许临时跳过，用 `@pytest.mark.skip` 标记）。

---

### Step 10.2 — 前端后端联调

**指令**：启动后端，启动前端。逐项验证完整用户流程：
1. 启动应用 → 状态栏显示"就绪"
2. 添加文件 → 文件列表更新
3. 修改页面设置 → 参数保存到 Profile
4. 修改正文样式 → 切换导航项 → 确认提示框 → 保存
5. 选择模板 → 配置区域同步更新
6. 点击"预览" → 参数摘要显示
7. 点击"开始排版" → 进度条更新 → 任务完成 → 结果显示
8. 查看历史记录 → 复用配置
9. 切换到深色主题 → 界面整体变色
10. 关闭应用 → 重启 → 确认主题/语言设置保留

**验证**：以上 10 个流程全部通过，无崩溃、无明显 UI 卡顿、无未捕获异常。

---

### Step 10.3 — 性能验证

**指令**：按 `design-document.md` 第 15 节验证性能指标。准备 100 个 .docx 测试文件（每个 1 页正文）。测试场景：1) 冷启动时间（双击 run.bat 到窗口出现的秒数，预期 < 2s）；2) 批量排版 100 个文件的总耗时（预期 < 10s）；3) 排版过程中 UI 响应性（能否切换导航项、滚动文件列表）；4) 配置面板切换延迟（预期 < 100ms）。

**验证**：用秒表记录启动时间、排版时间。排版过程中连续点击导航项 → 确认 UI 不卡顿。打开任务管理器查看内存占用 → 排版 100 文件后内存增长不超过 200MB，任务完成后内存回落。

---

## 阶段 11：打包与发布准备

### Step 11.1 — 创建 PyInstaller 打包配置

**指令**：创建 `scripts/build_backend.spec`。使用 PyInstaller 打包后端为独立 exe（`WordFormatterBackend.exe`）。包含所有 Python 依赖、`backend/` 目录、`shared/` 目录、`config/` 目录。排除 `tests/`、`logs/`、`cache/`。输出目录 `dist/backend/`。

**验证**：运行 `pyinstaller scripts/build_backend.spec` → 在 `dist/backend/` 下生成 exe。双击 exe → 启动后端服务 → curl `http://127.0.0.1:8765/api/health` 返回 200。

---

### Step 11.2 — 创建 dotnet publish 配置

**指令**：配置 `frontend/WordFormatterUI.csproj` 的 Publish 设置：输出类型 WinExe，自包含发布（`SelfContained=true`），目标运行时 win-x64。输出目录 `dist/frontend/`。

**验证**：运行 `dotnet publish frontend/WordFormatterUI.csproj -c Release -r win-x64 --self-contained -o dist/frontend/` → 在 `dist/frontend/` 下生成 `WordFormatterUI.exe`。双击 exe → 确认窗口正常打开。

---

### Step 11.3 — 创建一键启动脚本

**指令**：更新 `run.bat`。先启动 `WordFormatterBackend.exe`（后台模式），等待端口就绪（轮询 health 端点），再启动 `WordFormatterUI.exe`。增加进程清理：启动前 kill 残留的旧进程。

**验证**：双击 `run.bat` → 后端和前端均启动 → 前端窗口出现并连接后端成功（状态栏显示"就绪"）。关闭前端窗口 → 后端进程也退出。

---

## 附录 A：关键决议（Q1-Q6 最终回答）

以下决议已融入本计划正文，此处集中列出供快速参考：

| # | 问题 | 最终决议 |
|---|---|---|
| Q1 | JSON 命名 | **camelCase**。后端 Python 内部用 snake_case，通过 Pydantic `alias_generator` 转换输出。前端用 `JsonNamingPolicy.CamelCase`。 |
| Q2 | 前端目录 | **`frontend/`**，不重命名为 `ui/`。与 `backend/` 形成直观对照。 |
| Q3 | 旧代码处理 | **Phase 0 立即迁移**。Step 0.1 创建目录后马上把 `engine.py`/`models.py`/`worker.py` 移入 `backend/formatter/`，不保留旧导入。 |
| Q4 | API 路径 | **用新路径，去 `/v1` 前缀**。如 `/api/files/add`、`/api/format/start`。桌面软件前后端强绑定，版本前缀无收益。 |
| Q5 | 预置模板 | **"默认模板"** 和 **"日常写作模板"**。AI 预设中规中矩参数（宋体 12pt、1.5 倍行距、A4），文件名为 `default.json` 和 `daily_writing.json`。 |
| Q6 | 开发 vs 发布 | 开发阶段（Phase 0-10）：`python -m uvicorn` + `dotnet run`。发布阶段（Phase 11）：PyInstaller exe + dotnet publish，前端负责拉起后端。 |

## 附录 B：文档间剩余差异

以下差异已在 Q1-Q6 中解决，无需额外裁决：

| 项 | 说明 |
|---|---|
| 导航项数量 | design-document.md 8 项（含"关于"），architecture.md 7 项。采用 8 项。 |
| 状态栏高度 | 统一 32px |
| 配置存储 | `config/settings.json`，无冲突 |

**总步骤数**：36 步，分 11 个阶段。
