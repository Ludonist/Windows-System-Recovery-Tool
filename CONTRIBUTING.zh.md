# 贡献 / Участие / Contributing

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md)

---

# 贡献于 System Restore Tool（简体中文）

感谢您想做出贡献！🎉

本项目是一个开源工具，用于通过直接调用 Win32 API 恢复 Windows 10/11 系统文件。欢迎任何贡献：错误报告、功能、翻译、文档。

## 📋 目录

- [如何报告错误](#-如何报告错误)
- [如何提出功能](#-如何提出功能)
- [开发环境设置](#-开发环境设置)
- [代码风格](#-代码风格)
- [Pull Request 流程](#-pull-request-流程)
- [安全规则](#-安全规则)

## 🐛 如何报告错误

创建 issue 之前：
1. 检查[现有 issue](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — 错误可能已知。
2. 从 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 更新到最新版本。
3. 收集诊断信息。

在 issue 中指定：
- **程序版本**（从启动时的横幅中获取）
- **Windows 版本**（Win+R → `winver`）
- **架构**（x64 / ARM64）
- **复现步骤**
- **预期行为**
- **实际行为**
- **程序日志**（文件 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — 附加到 issue）

## 💡 如何提出功能

1. 创建带有 `enhancement` 标签的 issue。
2. 描述用例：为什么需要它，解决什么问题。
3. 建议可以通过哪些 API/DLL 实现（如果您知道）。

## 🛠 开发环境设置

### 要求
- **Windows 10**（内部版本 19041+）或 **Windows 11**
- **.NET 6.0 SDK** 或更高版本 — https://dotnet.microsoft.com/download
- （可选）Visual Studio 2022 / Rider / VS Code

### 构建
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

结果：`bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 运行
程序需要管理员权限（清单已指定）。
右键 → **以管理员身份运行**。

## 🎨 代码风格

### 通用规则
- **C# 10+**，.NET 6，`ImplicitUsings=enable`，`Nullable=disable`
- 4 个空格缩进，**无制表符**
- 行长度最多 120 个字符
- 公共成员使用 `PascalCase`，局部变量使用 `camelCase`

### P/Invoke 包装器
- 对于返回 HRESULT 的函数使用 `PreserveSig = false`
- 保留带有 `W` 后缀的函数名（Unicode）：`CreateFileW`，而不是 `CreateFile`
- 以 Win32 风格命名常量：`GENERIC_READ`、`OPEN_EXISTING`

### 日志记录
使用静态 `Logger.Instance`：
```csharp
Logger.Instance.Info("消息");
Logger.Instance.Success("成功");
Logger.Instance.Warn("警告");
Logger.Instance.Error("错误");
Logger.Instance.Progress(current, total, "标签");
```

### 本地化
所有 UI 字符串必须在 `Resources/strings.{ru,en,zh}.json` 中，并通过以下方式访问：
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Pull Request 流程

1. **Fork** 仓库
2. 创建分支：`git checkout -b feature/my-feature`
3. **Commit** 带有清晰消息：
   ```
   feat: 通过 tbs.dll 添加 TPM 检查
   
   - Tbsi_Get_TCG_Log_Ex 用于获取 PCR 日志
   - 集成到 IntegrityChecker
   - 更新了 README
   ```
4. 推送：`git push origin feature/my-feature`
5. 打开 PR 到 `main`，描述更改

### Commit 前缀
- `feat:` — 新功能
- `fix:` — 错误修复
- `docs:` — 仅文档
- `refactor:` — 重构，无行为更改
- `test:` — 添加/修复测试
- `chore:` — 项目维护（依赖项、.gitignore 等）
- `i18n:` — 本地化更改

## ⚠️ 安全规则

该工具处理关键系统组件。因此：

1. **切勿提交静默删除系统文件的代码** — 每次删除都必须是显式的并记录在案。
2. **在虚拟机中测试**，然后再 PR，特别是如果更改 `RegistryRestoreManager`、`BootRecoveryManager` 或 `WinSxsRepairManager`。
3. **不要添加**无法撤消的调用（例如 `FormatEx`、`DeleteVolumeMountPoint`），除非用户确认。
4. **P/Invoke 签名** — 根据 [Win32 文档](https://learn.microsoft.com/windows/win32/api/) 仔细检查。签名错误 = 程序崩溃或内存损坏。
5. **私钥**和任何凭据禁止在仓库中使用。

## 📜 许可证

通过贡献，您同意您的代码将根据 [MIT](LICENSE) 许可证发布。

## 🙏 致谢

贡献者将列在 [README.md](README.md) 中。
