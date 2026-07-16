# Word Formatter API 接口文档

> **Version**：2.0
> **Last Update**：2026-07
> **适用于**：WinUI 3 + Python Backend（FastAPI）

---

## 目录

- [1. API 设计原则](#1-api-设计原则)
- [2. 通用规范](#2-通用规范)
- [3. 统一返回格式](#3-统一返回格式)
- [4. 错误码与 HTTP 状态码](#4-错误码与-http-状态码)
- [5. 健康检查](#5-健康检查)
- [6. 文件管理接口](#6-文件管理接口)
- [7. 排版配置接口](#7-排版配置接口)
- [8. 模板管理接口](#8-模板管理接口)
- [9. 排版任务接口](#9-排版任务接口)
- [10. 预览接口](#10-预览接口)
- [11. 历史记录接口](#11-历史记录接口)
- [12. 设置接口](#12-设置接口)
- [13. 数据模型](#13-数据模型)
- [14. API 版本与路由](#14-api-版本与路由)

---

## 1. API 设计原则

- 所有接口遵循 **REST** 风格。
- 通信协议：HTTP/1.1，请求与响应均使用 **JSON** 格式，字符编码 **UTF-8**。
- 服务默认监听地址：`http://127.0.0.1:8765`。
- 接口路径统一以 `/api` 为前缀（无版本号，桌面软件前后端强绑定）。
- 排版任务等耗时操作均采用异步模式，通过任务 ID 查询进度。
- UI 层严禁直接访问后端模块，所有交互必须通过 API 完成。

---

## 2. 通用规范

### 2.1 请求头

所有 POST / PUT 请求必须携带：

```
Content-Type: application/json
```

### 2.2 HTTP 方法语义

| 方法     | 用途        |
| ------ | --------- |
| GET    | 查询资源      |
| POST   | 创建资源或执行操作 |
| PUT    | 更新整个资源    |
| DELETE | 删除资源      |

### 2.3 时间与日期格式

涉及时间的字段统一采用 ISO 8601 格式（`YYYY-MM-DDTHH:mm:ssZ`），例如 `2026-07-01T12:00:00Z`。

---

## 3. 统一返回格式

所有 API 返回均遵循以下 JSON 结构：

- **成功响应**：
  
  ```json
  {
      "success": true,
      "code": 0,
      "message": "OK",
      "data": { }
  }
  ```

- **失败响应**：
  
  ```json
  {
      "success": false,
      "code": 1001,
      "message": "File not found",
      "data": null
  }
  ```

`data` 字段根据具体接口返回对应数据，失败时通常为 `null`。

---

## 4. 错误码与 HTTP 状态码

### 4.1 业务错误码

| Code | 含义      | 说明            |
| ---- | ------- | ------------- |
| 0    | 成功      | 操作成功完成        |
| 1000 | 未知错误    | 未分类的内部错误      |
| 1001 | 文件不存在   | 指定的文件路径不存在    |
| 1002 | 文件无法读取  | 文件权限不足或已损坏    |
| 1003 | 模板不存在   | 模板 ID 或名称无效   |
| 1004 | 模板损坏    | 模板文件内容非法      |
| 1005 | 配置错误    | 排版配置参数不合法     |
| 1006 | 参数错误    | 请求参数无效或缺失     |
| 1007 | 任务不存在   | 任务 ID 无效      |
| 1008 | 任务已取消   | 任务已被用户取消      |
| 1009 | 输出目录不存在 | 指定的输出目录无法访问   |
| 1010 | 排版失败    | 单个文件排版过程中出现错误 |

### 4.2 HTTP 状态码

| 状态码 | 使用场景                     |
| --- | ------------------------ |
| 200 | 请求成功（GET / PUT / DELETE） |
| 201 | 创建成功（POST）               |
| 202 | 任务已接受，异步处理（任务启动）         |
| 400 | 请求参数错误                   |
| 404 | 资源未找到                    |
| 500 | 服务器内部错误                  |

业务错误码与 HTTP 状态码配合使用，例如：

- 参数错误返回 `400`，body 中 `code=1006`。
- 文件不存在返回 `404`，body 中 `code=1001`。

---

## 5. 健康检查

### `GET /api/health`

检查后端服务是否正常运行。

**响应示例**：

```json
{
    "success": true,
    "code": 0,
    "message": "OK",
    "data": {
        "status": "ok",
        "version": "2.0"
    }
}
```

---

## 6. 文件管理接口

### 6.1 获取当前文件列表

**请求**：

```
GET /api/files
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "files": [
            "C:/A.docx",
            "C:/B.docx"
        ]
    }
}
```

### 6.2 添加文件

**请求**：

```
POST /api/files/add
Content-Type: application/json
```

```json
{
    "paths": [
        "C:/A.docx",
        "C:/B.docx"
    ]
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Files added",
    "data": {
        "count": 2
    }
}
```

### 6.3 添加文件夹

递归扫描文件夹中的 `.doc` / `.docx` 文件。

**请求**：

```
POST /api/files/add-folder
Content-Type: application/json
```

```json
{
    "folder": "C:/Documents",
    "include_subdir": true
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "count": 25
    }
}
```

### 6.4 移除指定文件

**请求**：

```
POST /api/files/remove
Content-Type: application/json
```

```json
{
    "paths": [
        "C:/A.docx"
    ]
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Files removed",
    "data": {
        "removed_count": 1
    }
}
```

### 6.5 清空文件列表

**请求**：

```
DELETE /api/files
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "File list cleared"
}
```

### 6.6 搜索文件

根据关键词在当前文件列表中过滤。

**请求**：

```
POST /api/files/search
Content-Type: application/json
```

```json
{
    "keyword": "论文"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "files": [
            "C:/毕业论文.docx"
        ]
    }
}
```

### 6.7 获取最近打开的目录/文件

**请求**：

```
GET /api/files/recent
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "recent": [
            { "path": "C:/Recently/Used.docx", "type": "file" },
            { "path": "D:/Documents", "type": "folder" }
        ]
    }
}
```

### 6.8 固定常用目录

**请求**：

```
POST /api/files/pin
Content-Type: application/json
```

```json
{
    "folder": "D:/常用文档"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "pinned": [
            "D:/常用文档"
        ]
    }
}
```

---

## 7. 排版配置接口

### 7.1 获取当前配置

**请求**：

```
GET /api/profile
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "profile": {
            "page": { ... },
            "header_footer": { ... },
            "body": { ... },
            "heading": { ... },
            "image": { ... },
            "table": { ... }
        }
    }
}
```

### 7.2 更新当前配置

**请求**：

```
PUT /api/profile
Content-Type: application/json
```

```json
{
    "profile": {
        "page": { ... },
        "body": { ... }
    }
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Profile updated"
}
```

### 7.3 恢复默认配置

**请求**：

```
POST /api/profile/reset
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Profile reset to default"
}
```

---

## 8. 模板管理接口

### 8.1 获取模板列表

**请求**：

```
GET /api/templates
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "templates": [
            {
                "id": "tpl_001",
                "name": "通用公文模板",
                "is_default": true
            },
            {
                "id": "tpl_002",
                "name": "日常写作模板",
                "is_default": false
            }
        ]
    }
}
```

### 8.2 保存模板

**请求**：

```
POST /api/templates
Content-Type: application/json
```

```json
{
    "name": "我的模板",
    "profile": { ... }
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "id": "tpl_003",
        "name": "我的模板"
    }
}
```

### 8.3 更新模板

**请求**：

```
PUT /api/templates/{id}
Content-Type: application/json
```

```json
{
    "name": "更新名称",
    "profile": { ... }
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Template updated"
}
```

### 8.4 删除模板

**请求**：

```
DELETE /api/templates/{id}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Template deleted"
}
```

### 8.5 导入模板

**请求**：

```
POST /api/templates/import
Content-Type: application/json
```

```json
{
    "path": "C:/templates/my_template.json"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "id": "tpl_004",
        "name": "导入的模板"
    }
}
```

### 8.6 导出模板

**请求**：

```
POST /api/templates/export
Content-Type: application/json
```

```json
{
    "template_id": "tpl_001",
    "target_path": "C:/exports/"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "exported_file": "C:/exports/通用公文模板.json"
    }
}
```

### 8.7 设为默认模板

**请求**：

```
POST /api/templates/default
Content-Type: application/json
```

```json
{
    "template_id": "tpl_001"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Default template set"
}
```

---

## 9. 排版任务接口

### 9.1 开始排版

**请求**：

```
POST /api/format/start
Content-Type: application/json
```

```json
{
    "files": [
        "C:/demo.docx",
        "C:/demo2.docx"
    ],
    "profile": "default",
    "output_dir": "C:/输出目录"
}
```

**响应** (HTTP 202)：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "task_id": "123456"
    }
}
```

### 9.2 查询任务状态

建议客户端每 500ms 轮询一次。

**请求**：

```
GET /api/format/status/123456
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "state": "running",
        "progress": 35,
        "current": 7,
        "total": 20,
        "current_file": "A.docx"
    }
}
```

**任务状态枚举**：

| 状态        | 含义       |
| --------- | -------- |
| idle      | 未开始      |
| preparing | 准备中（扫描等） |
| running   | 正在处理     |
| saving    | 保存文件中    |
| completed | 全部完成     |
| failed    | 任务失败     |
| cancelled | 已取消      |

### 9.3 取消任务

**请求**：

```
POST /api/format/cancel
Content-Type: application/json
```

```json
{
    "task_id": "123456"
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Task cancelled"
}
```

### 9.4 获取任务结果

任务完成后可通过此接口获取详细结果。

**请求**：

```
GET /api/format/result/123456
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "ok": 18,
        "fail": 2,
        "total": 20,
        "failed_files": [
            {
                "path": "C:/corrupted.docx",
                "error_code": 1010,
                "reason": "排版失败：文件损坏"
            }
        ],
        "output_dir": "C:/输出目录"
    }
}
```

---

## 10. 预览接口

### 10.1 参数摘要预览（Level 1）

### `POST /api/preview`

生成当前配置的参数摘要文本。

**请求**：

```
POST /api/preview
Content-Type: application/json
```

```json
{
    "file": "C:/demo.docx",
    "profile": "default"
}
```

> `file` 可选（默认空字符串）。`profile` 可以是模板 ID 字符串或完整 ProfileConfig 对象。

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "preview": "【纸张】A4 纵向\n【正文】宋体 小四，行距1.5倍，首行缩进2字符\n..."
    }
}
```

### 10.2 PDF 真实预览（Level 2）

### `POST /api/preview/pdf`

启动 PDF 预览任务。后端执行 format_docx 生成格式化后的 .docx 文件，前端通过 WPS/Word COM 转为 PDF 并在 WebView2 + PDF.js 中渲染。

**请求**：

```
POST /api/preview/pdf
Content-Type: application/json
```

```json
{
    "file": "C:/demo.docx",
    "profile": "default"
}
```

> `file` 必填。

**响应**（HTTP 202）：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "taskId": "preview_xxx"
    }
}
```

### `GET /api/preview/pdf/{task_id}`

轮询 PDF 预览任务状态。

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "state": "completed",
        "previewPath": "C:/temp/preview_xxx.docx",
        "error": null
    }
}
```

### `POST /api/preview/pdf/{task_id}/cancel`

取消进行中的 PDF 预览任务。

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Task cancelled"
}
```

---

## 11. 历史记录接口

### 11.1 获取最近任务列表

**请求**：

```
GET /api/history
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "history": [
            {
                "id": "h_001",
                "time": "2026-07-01T10:30:00Z",
                "duration": 45.2,
                "success": 20,
                "failed": 0,
                "template": "通用公文模板"
            }
        ]
    }
}
```

### 11.2 获取任务详情

**请求**：

```
GET /api/history/h_001
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "id": "h_001",
        "time": "2026-07-01T10:30:00Z",
        "duration": 45.2,
        "profile": { ... },
        "files": [ ... ],
        "results": { ... }
    }
}
```

### 11.3 清空历史记录

**请求**：

```
DELETE /api/history
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "History cleared"
}
```

---

## 12. 设置接口

### 12.1 获取软件设置

**请求**：

```
GET /api/settings
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "data": {
        "recent_dir": "C:/Documents",
        "default_template": "tpl_001",
        "output_dir": "",
        "dark_mode": false,
        "auto_update": false
    }
}
```

### 12.2 更新软件设置

**请求**：

```
PUT /api/settings
Content-Type: application/json
```

```json
{
    "settings": {
        "output_dir": "D:/Output",
        "dark_mode": true
    }
}
```

**响应**：

```json
{
    "success": true,
    "code": 0,
    "message": "Settings updated"
}
```

---

## 13. 数据模型

### 13.1 FileItem

```json
{
    "path": "C:/demo.docx",
    "name": "demo.docx",
    "size": 102400
}
```

### 13.2 Profile（完整配置）

```json
{
    "page": {
        "paperSize": "A4",
        "orientation": "portrait",
        "marginTop": 25.4,
        "marginBottom": 25.4,
        "marginLeft": 31.7,
        "marginRight": 31.7,
        "pageNumber": true,
        "customWidth": 210.0,
        "customHeight": 297.0,
        "documentGrid": {
            "mode": "无网格",
            "linesPerPage": 30,
            "charsPerLine": 35,
            "adjustRightIndent": true,
            "alignToGrid": true
        }
    },
    "headerFooter": {
        "fontCn": "宋体",
        "fontEn": "Times New Roman",
        "fontSize": 10.5,
        "fontStyle": "normal",
        "alignment": "center",
        "headerDistance": 15.0,
        "footerDistance": 17.5,
        "headerDistanceUnit": "mm",
        "footerDistanceUnit": "mm"
    },
    "body": {
        "fontCn": "宋体",
        "fontEn": "Times New Roman",
        "fontSize": 12.0,
        "fontStyle": "normal",
        "alignment": "justify",
        "lineSpacing": 1.5,
        "lineSpacingMode": "multiple",
        "lineSpacingUnit": "pt",
        "indentType": "firstLine",
        "indentValue": 2.0,
        "indentUnit": "字符",
        "spaceBefore": 0.0,
        "spaceAfter": 0.0,
        "spaceBeforeUnit": "行",
        "spaceAfterUnit": "行"
    },
    "heading": {
        "1": { "level": 1, "fontCn": "黑体", "fontEn": "Times New Roman", "fontSize": 22.0, "fontStyle": "bold", "fontColor": "#000000", "alignment": "left", "lineSpacing": 1.5, "lineSpacingMode": "multiple", "indentType": "none", "indentValue": 0.0, "indentUnit": "字符", "spaceBefore": 1.0, "spaceAfter": 1.0, "spaceBeforeUnit": "行", "spaceAfterUnit": "行" },
        "2": { "...": "..." },
        "3": { "...": "..." },
        "4": { "...": "..." },
        "5": { "...": "..." },
        "6": { "...": "..." }
    },
    "picture": {
        "sizeMode": "fixedWidth",
        "width": 12.0,
        "widthUnit": "cm",
        "height": 8.0,
        "heightUnit": "cm",
        "keepAspectRatio": true,
        "noEnlarge": false,
        "alignment": "center",
        "wrappingStyle": "inline",
        "compressionQuality": 220,
        "maxPixels": 1200,
        "autoCompress": true
    },
    "table": {
        "tableAlignment": "center",
        "widthMode": "auto",
        "widthValue": 0.0,
        "widthUnit": "cm",
        "autoFitColumns": true,
        "rowHeightMode": "auto",
        "rowHeight": 0.8,
        "rowHeightUnit": "cm",
        "headerFontCn": "宋体",
        "headerFontEn": "Times New Roman",
        "headerSize": 10.5,
        "headerBold": true,
        "headerTextCenter": true,
        "headerBgColor": "",
        "indentType": "none",
        "indentValue": 0.0,
        "indentUnit": "字符",
        "cellAlignH": "left",
        "cellAlignV": "middle",
        "borderStyle": "all",
        "borderColor": "#000000",
        "borderWidth": 0.5,
        "cellMargin": 0.19,
        "cellMarginUnit": "cm",
        "autoSplit": true,
        "repeatHeader": false
    }
}
```

### 13.3 Template

```json
{
    "id": "tpl_001",
    "name": "通用公文模板",
    "version": "2.0",
    "profile": { ... }
}
```

### 13.4 Task

```json
{
    "task_id": "123456",
    "state": "running",
    "progress": 35,
    "current": 7,
    "total": 20
}
```

### 13.5 History Record

```json
{
    "id": "h_001",
    "time": "2026-07-01T10:30:00Z",
    "duration": 45.2,
    "success": 20,
    "failed": 0,
    "template": "通用公文模板"
}
```

---

## 14. API 版本与路由

所有接口统一使用前缀 `/api`（无版本号，桌面软件前后端强绑定，无第三方调用）。例如：

- `GET  /api/files`
- `POST /api/files/add`
- `GET  /api/profile`
- `POST /api/format/start`

未来若开放公开 API，再引入 `/v1`、`/v2` 版本机制。

---
