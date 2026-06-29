# 贡献 / Участие / Contributing

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# 为 System Restore Tool 做贡献（简体中文）

感谢您愿意做出贡献！🎉

本项目是一款开源工具，通过直接调用 Win32 API 来恢复 Windows 10/11 系统文件。欢迎任何形式的贡献：错误报告、新功能、翻译、文档。

## 📋 目录

- [如何报告 Bug](#-如何报告-bug)
- [如何提议新功能](#-如何提议新功能)
- [开发环境搭建](#-开发环境搭建)
- [代码风格](#-代码风格)
- [Pull Request 流程](#-pull-request-流程)
- [安全规则](#-安全规则)

## 🐛 如何报告 Bug

创建 issue 之前：
1. 检查[已有的 issue](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — 这个 bug 可能已被报告。
2. 更新到 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 中的最新版本。
3. 收集诊断信息。

在 issue 中请注明：
- **程序版本**（启动时横幅中显示）
- **Windows 版本**（Win+R → `winver`）
- **架构**（x64 / ARM64）
- **复现步骤**
- **预期行为**
- **实际行为**
- **程序日志**（文件 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — 附加到 issue 中）

## 💡 如何提议新功能

1. 创建一个带 `enhancement` 标签的 issue。
2. 描述使用场景：为什么需要它，解决了什么问题。
3. 建议实现此功能的 API/DLL（如您知道）。

## 🛠 开发环境搭建

### 要求
- **Windows 10**（build 19041+）或 **Windows 11**
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
程序需要管理员权限（清单中已指定）。
右键 → **以管理员身份运行**。

## 🎨 代码风格

### 通用规则
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 缩进使用 4 个空格，**不使用 Tab**
- 行宽不超过 120 个字符
- 公共成员使用 `PascalCase`，局部变量使用 `camelCase`

### P/Invoke 封装
- 对返回 HRESULT 的函数使用 `PreserveSig = false`
- 函数名保留 `W` 后缀（Unicode）：`CreateFileW`，而不是 `CreateFile`
- 常量使用 Win32 风格命名：`GENERIC_READ`, `OPEN_EXISTING`

### 日志
使用静态 `Logger.Instance`：
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### 本地化
所有 UI 字符串必须放在 `Resources/strings.{ru,en,zh}.json` 中，并通过以下方式访问：
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Pull Request 流程

1. **Fork** 本仓库
2. 创建分支：`git checkout -b feature/my-feature`
3. **提交**并附带清晰的提交信息：
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. 推送：`git push origin feature/my-feature`
5. 向 `main` 发起 PR，并描述变更内容

### 提交前缀
- `feat:` — 新功能
- `fix:` — 错误修复
- `docs:` — 仅文档
- `refactor:` — 不改变行为的重构
- `test:` — 添加/修复测试
- `chore:` — 项目维护（依赖、.gitignore 等）
- `i18n:` — 本地化变更

## ⚠️ 安全规则

本工具操作关键系统组件。因此：

1. **绝不提交会静默删除系统文件的代码** — 每次删除都必须是显式的且有日志记录。
2. **在虚拟机中测试**后再发起 PR，特别是修改 `RegistryRestoreManager`、`BootRecoveryManager` 或 `WinSxsRepairManager` 时。
3. **不要添加**无法撤销的调用（例如 `FormatEx`、`DeleteVolumeMountPoint`），除非用户确认。
4. **P/Invoke 签名** — 务必对照 [Win32 文档](https://learn.microsoft.com/windows/win32/api/) 核对。签名错误 = 程序崩溃或内存损坏。
5. **私钥**及任何凭据禁止放入仓库。

## 📜 许可证

通过贡献，您同意您的代码将以 [MIT](LICENSE) 许可证发布。

## 🙏 致谢

贡献者将列于 [README.md](README.md) 中。
