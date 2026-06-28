# 安全策略 / Политика безопасности / Security Policy

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md)

---

# 安全策略（简体中文）

## 支持的版本

| 版本 | 支持 | 安全更新 |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ 活跃 | 当前 |
| 2.0.x  | ⚠️ 仅关键 | 直到 2.6.0 发布 |
| < 2.0  | ❌ 否     | — |

## 报告漏洞

如果您发现漏洞，**请勿创建公开的 issue**。

相反：
1. 发送电子邮件至：`security@example.com`（替换为您的电子邮件）
2. 主题：`[SECURITY] System Restore Tool — <简要描述>`

### 报告中应包含的内容
- 漏洞描述和潜在影响
- 复现步骤
- 程序版本和 Windows 版本
- 可能的缓解措施（如已知）

### SLA
- **确认** — 48 小时内
- **初步评估** — 5 个工作日内
- **修复或变通方法** — 30 天内（取决于严重程度）

## 程序安全

### 程序做什么
- ✅ 仅在您的机器上本地运行
- ✅ 不通过网络发送数据（DISM RestoreHealth 除外，它使用 Windows Update）
- ✅ 需要管理员权限（通过清单）
- ✅ 将所有操作记录到 `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ 在关键操作之前创建还原点

### 程序不做什么
- ❌ 不向开发者服务器发送数据
- ❌ 不下载额外的二进制文件（仅通过 DISM/WU）
- ❌ 未经您的同意不修改引导加载程序
- ❌ 未经确认不删除文件

### 建议
1. 在使用前**创建还原点**（菜单项 17）
2. **不要运行不熟悉的操作** — 每项都有描述
3. 工作完成后**检查日志**
4. **仅从 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 下载 EXE** — 不要从第三方来源

### 供应链
- 源代码 — 根据 MIT 开源
- 依赖项：`Microsoft.Dism 3.2.0`（NuGet，由 Microsoft 签名）
- 构建 — 通过 `dotnet publish` 从源代码
- 发布 — 在 GitHub Actions 上构建（透明）
