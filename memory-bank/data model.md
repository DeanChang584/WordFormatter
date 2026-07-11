# Word Formatter 数据模型文档（Data Model）

> **Version**：2.0
> **Last Update**：2026-07
> **Architecture**：WinUI 3 + Python Backend

---

## 目录

1. [设计目标](#1-设计目标)
2. [数据模型总览](#2-数据模型总览)
3. [文件模型](#3-文件模型-file)
4. [排版配置模型](#4-排版配置模型-profile)
    - 4.1 [页面设置](#41-页面设置-page)
    - 4.2 [页眉页脚](#42-页眉页脚-headerfooter)
    - 4.3 [正文样式](#43-正文样式-body)
5. [标题样式模型](#5-标题样式模型-headingstyle)
6. [模板模型](#6-模板模型-template)
7. [排版任务模型](#7-排版任务模型-task)
8. [排版结果模型](#8-排版结果模型-result)
9. [预览模型](#9-预览模型-preview)
10. [历史记录模型](#10-历史记录模型-history)
11. [软件设置模型](#11-软件设置模型-settings)
12. [日志模型](#12-日志模型-log)
13. [JSON 命名规范](#13-json-命名规范)
14. [数据版本管理](#14-数据版本管理)
15. [附录](#附录)
     - [A. 数据对象关系图](#附录-a数据对象关系图)
     - [B. 设计原则](#附录-b设计原则)

---

## 1. 设计目标

本文档定义 Word Formatter 系统中全部数据对象（Data Model），作为以下模块的唯一数据契约：

- API 请求与响应
- JSON 配置文件
- 模板文件结构
- 历史记录存储
- 前后端数据交换

所有系统模块必须严格遵守本模型定义，任何增删改字段均需同步更新本文档。

---

## 2. 数据模型总览

系统包含以下核心数据对象：

| 对象           | 说明             |
| ------------ | -------------- |
| File         | 待处理 Word 文件    |
| Profile      | 排版配置（参数集）      |
| HeadingStyle | 标题样式（1~6 级）    |
| Template     | 模板（封装 Profile） |
| Task         | 排版任务           |
| TaskResult   | 任务执行结果         |
| Preview      | 排版预览信息         |
| History      | 历史任务记录         |
| Settings     | 软件全局设置         |
| Log          | 运行日志条目         |

**核心关系**：

```
Template
   │
   ▼
Profile
   │
   ▼
Task
 ├── File[]
 ├── Result
 └── Preview
```

---

## 3. 文件模型（File）

表示一个待处理的 Word 文件及其处理状态。

```json
{
  "id": "uuid",
  "name": "document.docx",
  "path": "C:/Users/.../document.docx",
  "size": 102400,
  "modifiedTime": "2026-07-01T12:00:00Z",
  "status": "waiting"
}
```

**字段说明**：

| 字段           | 类型     | 说明                                            |
| ------------ | ------ | --------------------------------------------- |
| id           | string | 文件唯一标识符（UUID）                                 |
| name         | string | 文件名（含扩展名）                                     |
| path         | string | 文件完整路径                                        |
| size         | int    | 文件大小（字节）                                      |
| modifiedTime | string | 最后修改时间（ISO 8601）                              |
| status       | enum   | 处理状态：`waiting` / `running` / `done` / `error` |

---

## 4. 排版配置模型（Profile）

Profile 是全部排版参数的集合，结构如下：

```
Profile
├── Page             # 页面设置
├── HeaderFooter     # 页眉页脚
├── Body             # 正文样式
├── Heading[]        # 1~6 级标题样式数组
├── Picture          # 图片设置
└── Table            # 表格设置
```

### 4.1 页面设置（Page）

```json
{
  "paperSize": "A4",
  "orientation": "portrait",
  "marginTop": 25.4,
  "marginBottom": 25.4,
  "marginLeft": 31.7,
  "marginRight": 31.7,
  "pageNumber": true
}
```

| 字段           | 类型      | 说明                             |
| ------------ | ------- | ------------------------------ |
| paperSize    | string  | 纸张规格（A4 / A3 / Letter 等）       |
| orientation  | enum    | 方向：`portrait`（纵向）/ `landscape` |
| marginTop    | float   | 上边距（mm）                        |
| marginBottom | float   | 下边距（mm）                        |
| marginLeft   | float   | 左边距（mm）                        |
| marginRight  | float   | 右边距（mm）                        |
| pageNumber   | boolean | 是否显示页码                         |

### 4.2 页眉页脚（HeaderFooter）

```json
{
  "fontCn": "宋体",
  "fontEn": "Times New Roman",
  "fontSize": 10.5,
  "fontStyle": "normal",
  "alignment": "center",
  "headerDistance": 15,
  "footerDistance": 15
}
```

| 字段             | 类型     | 说明                              |
| -------------- | ------ | ------------------------------- |
| fontCn         | string | 中文字体名                           |
| fontEn         | string | 西文字体名                           |
| fontSize       | float  | 字体大小（pt）                        |
| fontStyle      | enum   | 风格：`normal` / `bold` / `italic` |
| alignment      | enum   | 对齐：`left` / `center` / `right`  |
| headerDistance | float  | 页眉距顶端距离（mm）                     |
| footerDistance | float  | 页脚距底端距离（mm）                     |

> **备注**：页眉页脚的具体文字内容在模板的 `headerText` 与 `footerText` 中定义（可扩展）。

### 4.3 正文样式（Body）

```json
{
  "fontCn": "宋体",
  "fontEn": "Times New Roman",
  "fontSize": 12,
  "fontStyle": "normal",
  "alignment": "justify",
  "lineSpacing": 1.5,
  "indentType": "firstLine",
  "indentValue": 2,
  "spaceBefore": 0,
  "spaceAfter": 0
}
```

| 字段          | 类型     | 说明                                         |
| ----------- | ------ | ------------------------------------------ |
| fontCn      | string | 中文字体                                       |
| fontEn      | string | 西文字体                                       |
| fontSize    | float  | 字号（pt）                                     |
| fontStyle   | enum   | `normal` / `bold` / `italic`               |
| alignment   | enum   | 对齐：`left` / `center` / `right` / `justify` |
| lineSpacing | float  | 行距（倍数，如 1.5 表示 1.5 倍行距）                    |
| indentType  | enum   | 缩进类型：`firstLine`（首行缩进）/ `none`             |
| indentValue | float  | 缩进量（单位取决于上下文，通常为字符数）                       |
| spaceBefore | float  | 段前间距（pt）                                   |
| spaceAfter  | float  | 段后间距（pt）                                   |

---

## 5. 标题样式模型（HeadingStyle）

标题样式用于 Heading 1 ~ Heading 6，每级独立配置。

```json
{
  "level": 1,
  "fontCn": "黑体",
  "fontEn": "Arial",
  "fontSize": 16,
  "fontStyle": "bold",
  "alignment": "left",
  "lineSpacing": 1.5,
  "indentType": "none",
  "spaceBefore": 12,
  "spaceAfter": 6
}
```

| 字段          | 类型     | 说明                           |
| ----------- | ------ | ---------------------------- |
| level       | int    | 标题级别（1~6）                    |
| fontCn      | string | 中文字体                         |
| fontEn      | string | 西文字体                         |
| fontSize    | float  | 字号（pt）                       |
| fontStyle   | enum   | `normal` / `bold` / `italic` |
| alignment   | enum   | 对齐方式                         |
| lineSpacing | float  | 行距（倍数）                       |
| indentType  | enum   | `firstLine` / `none`         |
| indentValue | float  | 缩进值                          |
| spaceBefore | float  | 段前间距（pt）                     |
| spaceAfter  | float  | 段后间距（pt）                     |

---

## 6. 模板模型（Template）

模板是 Profile 的封装，包含元数据。

```json
{
  "id": "tpl_001",
  "name": "通用公文模板",
  "version": "2.0",
  "author": "",
  "description": "",
  "createTime": "2026-07-01T08:00:00Z",
  "updateTime": "2026-07-05T16:30:00Z",
  "profile": { }
}
```

**字段说明**：

| 字段          | 类型     | 说明             |
| ----------- | ------ | -------------- |
| id          | string | 模板唯一标识符        |
| name        | string | 模板名称           |
| version     | string | 模板数据格式版本       |
| author      | string | 作者             |
| description | string | 简要说明           |
| createTime  | string | 创建时间           |
| updateTime  | string | 最近更新时间         |
| profile     | object | 完整的 Profile 对象 |

系统预置默认模板（不可删除）并提供若干预设（如“教师”、“学生”、“毕业论文”等），用户可自行创建、导入导出。

---

## 7. 排版任务模型（Task）

表示一个批处理任务。

```json
{
  "taskId": "task_20260701_001",
  "status": "running",
  "createTime": "2026-07-01T10:00:00Z",
  "startTime": "2026-07-01T10:00:05Z",
  "finishTime": null,
  "files": []
}
```

| 字段         | 类型     | 说明                                                           |
| ---------- | ------ | ------------------------------------------------------------ |
| taskId     | string | 任务唯一标识符                                                      |
| status     | enum   | `waiting` / `running` / `completed` / `cancelled` / `failed` |
| createTime | string | 任务创建时间                                                       |
| startTime  | string | 开始处理时间（可能为 null）                                             |
| finishTime | string | 完成时间（未完成时为 null）                                             |
| files      | array  | 待处理文件列表（File 对象数组）                                           |

---

## 8. 排版结果模型（Result）

任务执行完毕后生成的汇总结果及单文件详情。

**汇总结果**：

```json
{
  "success": 998,
  "failed": 2,
  "skipped": 0,
  "elapsed": 125.6,
  "outputDirectory": "C:/Output",
  "results": []
}
```

| 字段              | 类型     | 说明          |
| --------------- | ------ | ----------- |
| success         | int    | 成功处理的文件数    |
| failed          | int    | 失败文件数       |
| skipped         | int    | 跳过的文件数（如取消） |
| elapsed         | float  | 总耗时（秒）      |
| outputDirectory | string | 输出目录路径      |
| results         | array  | 单文件结果数组（见下） |

**单文件结果**：

```json
{
  "file": "A.docx",
  "status": "success",
  "output": "A-R.docx",
  "message": ""
}
```

| 字段      | 类型     | 说明                              |
| ------- | ------ | ------------------------------- |
| file    | string | 源文件名                            |
| status  | enum   | `success` / `error` / `skipped` |
| output  | string | 输出文件名（成功时有效）                    |
| message | string | 错误原因或附加信息（失败时包含错误描述）            |

---

## 9. 预览模型（Preview）

用于在排版前向用户展示预期效果（第一阶段为参数摘要，后续可扩展为真实页面缩略图）。

```json
{
  "pageCount": 12,
  "warnings": ["字体 Arial 未安装"],
  "previewImages": []
}
```

| 字段            | 类型    | 说明                   |
| ------------- | ----- | -------------------- |
| pageCount     | int   | 预估总页数（若可计算）          |
| warnings      | array | 警告信息列表（如缺失字体等）       |
| previewImages | array | 预留的预览图（Base64 或 URL） |

---

## 10. 历史记录模型（History）

历史记录包含近期任务、输出路径和常用目录等信息。

**最近任务**：

```json
{
  "taskId": "task_20260701_001",
  "time": "2026-07-01T12:05:00Z",
  "template": "通用公文模板",
  "fileCount": 50,
  "elapsed": 45.2
}
```

| 字段        | 类型     | 说明      |
| --------- | ------ | ------- |
| taskId    | string | 任务 ID   |
| time      | string | 任务完成时间  |
| template  | string | 使用的模板名称 |
| fileCount | int    | 处理的文件总数 |
| elapsed   | float  | 总耗时（秒）  |

**最近输出目录**（系统记录最近的输出路径）：

```json
{
  "file": "C:/Source/A.docx",
  "output": "C:/Output/A-R.docx",
  "time": "2026-07-01T12:05:00Z"
}
```

**最近打开的文件夹**（由用户操作产生）：

```json
[
  "D:\\Word",
  "E:\\论文"
]
```

**固定目录**（用户手动固定常用的文件夹）：

```json
[
  "D:\\模板",
  "D:\\教师资料"
]
```

---

## 11. 软件设置模型（Settings）

全局软件配置，独立于具体排版参数。

```json
{
  "theme": "system",
  "language": "zh-CN",
  "defaultOutput": "sameFolder",
  "defaultTemplate": "Default",
  "recentCount": 20,
  "autoCheckUpdate": true
}
```

| 字段              | 类型      | 说明                               |
| --------------- | ------- | -------------------------------- |
| theme           | string  | 界面主题：`system` / `light` / `dark` |
| language        | string  | 界面语言（如 `zh-CN`）                  |
| defaultOutput   | string  | 默认输出方式：`sameFolder`（同目录）或自定义路径   |
| defaultTemplate | string  | 默认模板 ID 或名称                      |
| recentCount     | int     | 最近记录保留条数                         |
| autoCheckUpdate | boolean | 是否自动检查更新                         |

---

## 12. 日志模型（Log）

描述一条运行日志记录。

```json
{
  "time": "2026-07-01T10:30:15.123Z",
  "level": "INFO",
  "module": "Formatter",
  "message": "Successfully formatted 20 files."
}
```

| 字段      | 类型     | 说明                                        |
| ------- | ------ | ----------------------------------------- |
| time    | string | 日志时间戳（ISO 8601 含毫秒）                       |
| level   | enum   | 级别：`INFO` / `WARNING` / `ERROR` / `DEBUG` |
| module  | string | 产生日志的模块名                                  |
| message | string | 详细消息                                      |

---

## 13. JSON 命名规范

为确保跨语言一致性，所有字段命名严格遵循 **camelCase** 规范。

**正确示例**：

```
paperSize, fontSize, spaceBefore, headerDistance, isRunning, hasResult
```

**禁止使用**：

| 错误范例         | 问题描述              |
| ------------ | ----------------- |
| `paper_size` | 使用下划线（snake_case） |
| `PaperSize`  | 首字母大写（PascalCase） |
| `Paper_Size` | 混合使用无法识别          |

**布尔字段**：统一使用 `is` / `has` / `enable` 等动词前缀，例如 `isRunning`、`hasFooter`、`enablePreview`。

**枚举值**：全部小写，多个单词用连字符或无分隔，如 `left`、`justify`、`firstLine`、`sameFolder`。

---

## 14. 数据版本管理

所有持久化数据（模板、配置、历史等）必须包含版本号字段 `version`：

```json
{
  "version": "2.0"
}
```

**兼容性规则**：

- **向前兼容**：新版本软件必须能读取旧版本数据。
- **字段新增**：为新字段提供合理的默认值，防止旧数据解析失败。
- **字段废弃**：至少在一个主版本内保留对废弃字段的读取能力，并提供迁移路径。
- **模板导入**：导入模板时必须校验版本号，若版本不兼容应向用户提示并建议升级或拒绝。

---

## 附录

### 附录 A：数据对象关系图

```
Template
   │
   ▼
Profile
   │
   ▼
Task
 ├── File[]
 ├── Preview
 ├── Result
 └── History (由 Task 生成并存储)

Settings   (独立全局配置，作用于运行时环境)
Log        (独立运行时记录，可由任意模块产生)
```

### 附录 B：设计原则

- **平台无关**：数据模型独立于 UI 框架及底层存储方式（JSON / SQLite / 内存）。
- **字段一致**：同一业务概念在所有位置（API、JSON、模板、配置文件）使用相同字段名，禁止同义多词。
- **高内聚**：每个模型仅包含自身相关属性，不引用其他模型的内部细节。
- **向后兼容**：任何字段新增或变更不得破坏已有客户端或数据的正常使用。
- **独立演进**：模型版本独立于软件版本，用于控制数据格式升级。

---
