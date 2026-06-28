# Contributing / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md)

---

# Contributing to System Restore Tool (English)

Thank you for wanting to contribute! 🎉

This project is an open-source tool for recovering Windows 10/11 system files via direct Win32 API calls. Any contribution is welcome: bug reports, features, translations, documentation.

## 📋 Table of contents

- [How to report a bug](#-how-to-report-a-bug)
- [How to propose a feature](#-how-to-propose-a-feature)
- [Development environment setup](#-development-environment-setup)
- [Code style](#-code-style)
- [Pull Request process](#-pull-request-process)
- [Safety rules](#-safety-rules)

## 🐛 How to report a bug

Before creating an issue:
1. Check [existing issues](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — the bug may already be known.
2. Update to the latest version from [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Collect diagnostic information.

In the issue, specify:
- **Program version** (from the banner at startup)
- **Windows version** (Win+R → `winver`)
- **Architecture** (x64 / ARM64)
- **Steps to reproduce**
- **Expected behavior**
- **Actual behavior**
- **Program log** (file `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — attach to the issue)

## 💡 How to propose a feature

1. Create an issue with the `enhancement` label.
2. Describe the use case: why it's needed, what problem it solves.
3. Suggest the API/DLL through which this can be implemented (if you know).

## 🛠 Development environment setup

### Requirements
- **Windows 10** (build 19041+) or **Windows 11**
- **.NET 6.0 SDK** or newer — https://dotnet.microsoft.com/download
- (Optional) Visual Studio 2022 / Rider / VS Code

### Build
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Result: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Run
The program requires administrator privileges (the manifest already specifies this).
Right-click → **Run as administrator**.

## 🎨 Code style

### General rules
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 spaces for indentation, **no tabs**
- Line length up to 120 characters
- `PascalCase` for public members, `camelCase` for local variables

### P/Invoke wrappers
- Use `PreserveSig = false` for HRESULT-returning functions
- Keep function names with `W` suffix (Unicode): `CreateFileW`, not `CreateFile`
- Name constants in Win32 style: `GENERIC_READ`, `OPEN_EXISTING`

### Logging
Use the static `Logger.Instance`:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### Localization
All UI strings must be in `Resources/strings.{ru,en,zh}.json` and accessed via:
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Pull Request process

1. **Fork** the repository
2. Create a branch: `git checkout -b feature/my-feature`
3. **Commit** with a clear message:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. Push: `git push origin feature/my-feature`
5. Open a PR to `main` with a description of changes

### Commit prefixes
- `feat:` — new functionality
- `fix:` — bug fix
- `docs:` — documentation only
- `refactor:` — refactoring without behavior change
- `test:` — adding/fixing tests
- `chore:` — project maintenance (dependencies, .gitignore, etc.)
- `i18n:` — localization changes

## ⚠️ Safety rules

This tool works with critical system components. Therefore:

1. **Never commit code that silently deletes system files** — every deletion must be explicit and logged.
2. **Test in a virtual machine** before PR, especially if changing `RegistryRestoreManager`, `BootRecoveryManager`, or `WinSxsRepairManager`.
3. **Do not add calls** that cannot be undone (e.g., `FormatEx`, `DeleteVolumeMountPoint`) without user confirmation.
4. **P/Invoke signatures** — double-check against [Win32 docs](https://learn.microsoft.com/windows/win32/api/). Errors in signatures = program crash or memory corruption.
5. **Private keys** and any credentials are prohibited in the repository.

## 📜 License

By contributing, you agree that your code will be published under the [MIT](LICENSE) license.

## 🙏 Acknowledgements

Contributors will be listed in [README.md](README.md).
