# 实施进度

> 对照 implementation-plan.md 记录每个步骤的完成状态

## 阶段 0：项目基础设施

- [x] Step 0.1 — 创建目标目录结构（2026-07-12）
  - 创建全部 24 个子目录（frontend/ 下 8 个、backend/ 下 8 个、根级 8 个）
  - 迁移 engine/models/worker 到 backend/formatter/ 并重写为规则引擎架构
  - data_model.py 新建（dataclass，snake_case，含 FONT_SIZE_MAP 和 PAPER_SIZES）
  - font.py 新建（apply_run_font / apply_style_font）
  - page.py 新建（apply_page_setup）
  - paragraph.py 新建（apply_paragraph_format / apply_first_line_indent）
  - heading.py 新建（apply_heading_style + HEADING_NAMES 常量）
  - engine.py 新建（format_docx / convert_doc_to_docx / process_file / check_dependencies）
  - image.py / table.py / header_footer.py 占位文件
  - 更新 backend/routes/profile.py 和 format_tasks.py 的 import 路径
  - format_tasks.py 重写为直接 threading（移除 FormatWorker 依赖）
  - 删除根目录旧 engine.py / models.py / worker.py
  - 验证：import OK, health 200, process_file 输出正确
- [x] Step 0.2 — 创建共享数据模型文件（2026-07-12）
  - shared/schemas.py 已存在（占位 DTO，snake_case；完整 camelCase 重写延后到 Step 1.4）
  - shared/constants.py 新建（FONT_SIZE_MAP / FONT_SIZE_NAMES / font_size_to_name / PAPER_SIZES）
  - shared/version.py 新建（VERSION = "2.0"）
  - 验证：from shared import schemas / constants / version 均无 ImportError
- [x] Step 0.3 — 创建后端日志模块（2026-07-12）
  - backend/utils/__init__.py 新建（包标识）
  - backend/utils/logger.py 新建：get_logger(name, category="backend") 返回 INFO 级 Logger
  - 按用途分文件：app.log / backend.log / format.log / error.log（error.log 跨分类汇总 ERROR+）
  - 按日期滚动：TimedRotatingFileHandler（when=midnight）+ 自定义 namer，午夜生成 logs/{分类}-YYYY-MM-DD.log（留根目录）
  - 两级保留 purge_old_logs()：logs/ 根目录每日文件保留 30 天→移入 logs/archive/；archive/ 保留 90 天→自动删除；首次取 logger + 每次滚动后各清扫一次；用户无需手动清理
  - 格式 [时间] [级别] [模块名] 消息内容；file + console 双通道；propagate=False；get_logger 幂等
  - 决议：采用「按用途分文件 + 30/90 天两级保留」替代原 YYYY-MM-DD 单文件方案（已同步 architecture.md §9、design-document.md 十六之二、本计划 Step 0.3）
  - 验证：三分类各写 INFO + 一条 ERROR → 对应文件生成，ERROR 同进 format.log 与 error.log；doRollover → logs/app-YYYY-MM-DD.log；构造 45 天/120 天旧文件 → 分别被移入 archive/、删除，活动文件不受影响；幂等 handler 数=3
- [x] Step 0.4 — 初始化 Git 并更新 .gitignore（2026-07-12）
  - Git 仓库已存在（`.git/` 已初始化），无需重新 init
  - .gitignore 已存在，补充「Runtime data directories」段：新增 `logs/`、`cache/`、`temp/`、`output/`（原有仅 `*.log` 忽略日志文件，未忽略运行时目录）
  - 其余要求项均已覆盖：`__pycache__/`、`*.pyc`（`*.py[cod]`）、`*.zip`、`.vscode/`、`.idea/`、`frontend/bin/`、`frontend/obj/`
  - 验证：`git check-ignore -v` 确认四个运行时目录分别命中 .gitignore 第 27-30 行；`git status` 未列出 logs/cache/temp/output；`git ls-files` 确认无已跟踪的运行时文件
  - 说明：当前工作区存在 Phase 0 迁移产生的大量未提交变更（删除根目录旧 engine/models/worker、新增 backend/formatter 等），本步骤未执行 commit，交由用户决定提交时机

## 阶段 1：后端核心

- [ ] Step 1.1 — 创建后端应用入口
- [ ] Step 1.2 — 创建统一响应格式工具
- [ ] Step 1.3 — 创建后端配置管理模块
- [ ] Step 1.4 — 创建共享 DTO 定义
- [ ] Step 1.5 — 创建错误码定义
- [ ] Step 1.6 — 创建文件管理 API
- [ ] Step 1.7 — 创建排版配置 API
- [ ] Step 1.8 — 创建模板管理 API
- [ ] Step 1.9 — 创建排版任务 API

## 阶段 2：排版引擎重构

- [ ] Step 2.1 — 迁移到规则引擎架构
- [ ] Step 2.2 — 对齐排版配置数据结构
- [ ] Step 2.3 — 实现页面设置规则
- [ ] Step 2.4 — 实现段落与标题排版规则

## 阶段 3：Service 层

- [ ] Step 3.1 — 创建文件管理 Service
- [ ] Step 3.2 — 创建排版任务 Service
- [ ] Step 3.3 — 创建模板管理 Service

## 阶段 4：历史记录 + 预览

- [ ] Step 4.1 — 实现历史记录模块
- [ ] Step 4.2 — 实现参数摘要预览

## 阶段 5：前端基础

- [ ] Step 5.1 — 初始化 WinUI3 项目
- [ ] Step 5.2 — 创建 ApiService
- [ ] Step 5.3 — 创建前端 DTO 类

## 阶段 6：主窗口框架

- [ ] Step 6.1 — 创建三栏主窗口布局
- [ ] Step 6.2 — 实现标题栏
- [ ] Step 6.3 — 实现左侧导航栏
- [ ] Step 6.4 — 实现状态栏

## 阶段 7：中间配置区域

- [ ] Step 7.1 — 配置卡片框架
- [ ] Step 7.2 — 页面设置界面
- [ ] Step 7.3 — 正文样式界面
- [ ] Step 7.4 — 标题样式界面
- [ ] Step 7.5 — 页眉页脚界面
- [ ] Step 7.6 — 图片/表格设置界面
- [ ] Step 7.7 — 高级设置和关于界面

## 阶段 8：右侧工作区域

- [ ] Step 8.1 — 文件管理卡片
- [ ] Step 8.2 — 排版控制卡片
- [ ] Step 8.3 — 结果与历史卡片

## 阶段 9：MVVM 绑定

- [ ] Step 9.1 — 主窗口 ViewModel
- [ ] Step 9.2 — 各页面独立 ViewModel
- [ ] Step 9.3 — 绑定 ViewModel 与 API
- [ ] Step 9.4 — 配置修改标记

## 阶段 10：集成测试

- [ ] Step 10.1 — 后端全接口集成测试
- [ ] Step 10.2 — 前后端联调
- [ ] Step 10.3 — 性能验证

## 阶段 11：打包发布

- [ ] Step 11.1 — PyInstaller 打包
- [ ] Step 11.2 — dotnet publish
- [ ] Step 11.3 — 一键启动脚本

---

> 最后更新：2026-07-12（Step 0.4 完成，阶段 0 全部完成；等待用户验证后进入 Step 1.1）
