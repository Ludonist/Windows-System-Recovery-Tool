# 安全策略 / Политика безопасности / Security Policy

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# 安全策略（简体中文）

## 受支持的版本

| 版本 | 是否支持 | 安全更新 |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ 积极支持 | 当前 |
| 2.0.x  | ⚠️ 仅关键漏洞 | 直至 2.6.0 发布 |
| < 2.0  | ❌ 不支持 | — |

## 报告漏洞

如果您发现漏洞，**请勿创建公开的 issue**。

请按以下方式报告：
1. 发送邮件至：`security@example.com`（请替换为您的邮箱）
2. 邮件主题：`[SECURITY] System Restore Tool — <简要描述>`

### 报告中应包含的内容
- 漏洞描述及潜在影响
- 复现步骤
- 程序版本和 Windows 版本
- 可能的缓解措施（如已知）

### SLA（服务级别协议）
- **确认收到** — 48 小时内
- **初步评估** — 5 个工作日内
- **修复或临时方案** — 30 天内（取决于严重程度）

## 程序安全性

### 程序所做的事
- ✅ 仅在您的本机上运行
- ✅ 不通过网络发送数据（DISM RestoreHealth 除外，它使用 Windows Update）
- ✅ 需要管理员权限（通过清单指定）
- ✅ 将所有操作记录到 `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ 在关键操作前创建还原点

### 程序不做的事
- ❌ 不向开发者的服务器发送数据
- ❌ 不下载额外的可执行文件（仅通过 DISM/WU 下载）
- ❌ 未经您同意不修改引导加载程序
- ❌ 未经确认不删除文件

### 建议
1. 在使用前**创建还原点**（菜单项 17）
2. **不要运行不熟悉的操作** — 每项都有说明
3. 工作完成后**检查日志**
4. **仅从 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 下载 EXE** — 不要从第三方来源下载

### 供应链
- 源代码 — 基于 MIT 许可证开源
- 依赖项：`Microsoft.Dism 3.2.0`（NuGet，由 Microsoft 签名）
- 构建 — 通过 `dotnet publish` 从源代码构建
- 发布 — 在 GitHub Actions 上构建（透明可查）
