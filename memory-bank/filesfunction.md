# 文件功能清单

> 记录项目中每个文件的作用，保持与项目实际结构同步

---

## memory-bank/（设计文档）

| 文件 | 作用 |
|---|---|
| `design-document.md` | 产品设计规范：布局、交互、视觉、功能模块定义 |
| `architecture.md` | 软件架构文档：分层设计、模块划分、目录结构、通信数据流 |
| `tech-stack.md` | 技术栈文档：技术选型、命名规范、性能目标、开发流程 |
| `API.md` | API 接口文档：全部端点的请求/响应规范、错误码定义 |
| `data model.md` | 数据模型文档：所有数据对象的字段定义和 JSON schema |
| `implementation-plan.md` | AI 开发者实施计划：36 步分阶段指令 |
| `progress.md` | 实施进度记录：每步完成状态 |
| `filesfunction.md` | 本文件 — 文件功能清单 |

---

## 项目根目录

| 文件 | 作用 |
|---|---|
| `requirements.txt` | Python 依赖声明 |
| `run.bat` | 一键启动脚本（后端 + 前端） |
| `README.md` | 项目介绍和快速开始指南 |
| `.gitignore` | Git 忽略规则 |

---

## backend/（FastAPI 后端）

| 文件 | 作用 |
|---|---|
| `server.py` | 入口：FastAPI 应用初始化、CORS、路由注册 |
| `requirements.txt` | 后端 Python 依赖 |
| `routes/profile.py` | /api/profile：排版配置 CRUD，DTO ↔ FormatProfile 映射 |
| `routes/files.py` | /api/files：文件管理（添加/移除/清空/搜索/固定） |
| `routes/format_tasks.py` | /api/format：排版任务启动/进度/结果（直接 threading） |

### backend/formatter/（排版引擎）

| 文件 | 作用 |
|---|---|
| `__init__.py` | 包标识 |
| `data_model.py` | 数据模型：FormatProfile/PageConfig/BodyConfig/ParagraphConfig/HeadingStyleConfig（dataclass），FONT_SIZE_MAP，PAPER_SIZES |
| `font.py` | 字体工具：apply_run_font（段落级）、apply_style_font（样式级），XML 中英文分离 |
| `page.py` | 页面规则：apply_page_setup（边距/纸张/方向/页眉页脚边距） |
| `paragraph.py` | 段落规则：apply_paragraph_format（行距/缩进/间距/对齐）、apply_first_line_indent（字符/mm） |
| `heading.py` | 标题规则：apply_heading_style（1-6 级），HEADING_NAMES 常量映射 |
| `engine.py` | 排版主控：format_docx（编排全部规则）、convert_doc_to_docx（COM 转换）、process_file（分发）、check_dependencies |
| `image.py` | 图片规则 — 占位，Phase 2+ 实现 |
| `table.py` | 表格规则 — 占位，Phase 2+ 实现 |
| `header_footer.py` | 页眉页脚规则 — 占位，Phase 2+ 实现 |

### backend/utils/（工具函数）

| 文件 | 作用 |
|---|---|
| `__init__.py` | 包标识 |
| `logger.py` | 日志模块：get_logger(name, category) 返回 INFO 级 Logger；按用途分文件 app/backend/format/error.log，error.log 汇总 ERROR+；TimedRotatingFileHandler 午夜滚动为 logs/{分类}-YYYY-MM-DD.log；purge_old_logs() 两级保留（根目录 30 天→archive/，archive 90 天→删除）；格式 [时间] [级别] [模块名] 消息 |

---

## frontend/（WinUI3 前端）

| 文件 | 作用 |
|---|---|
| `App.xaml / App.xaml.cs` | 应用入口 + 后端健康检测 |
| `MainWindow.xaml / MainWindow.xaml.cs` | 主窗口：三栏布局 + 事件处理 + ViewModel 绑定 |
| `WordFormatterUI.csproj` | .NET 9 WinUI3 项目文件 |
| `Services/ApiService.cs` | HTTP 客户端封装 |
| `Services/ThemeService.cs` | 本地主题管理 |
| `ViewModels/` | MVVM ViewModel（FilesViewModel, FormatViewModel, ProfileViewModel） |
| `Pages/` | 页面 XAML（旧架构，待 Phase 7 重构） |

---

## shared/（前后端共享）

| 文件 | 作用 |
|---|---|
| `schemas.py` | Python dataclass DTO（ProfileDTO 等），前后端 JSON 契约 |
| `__init__.py` | 包标识 |

---

> 最后更新：2026-07-12 (Step 0.3 完成)
