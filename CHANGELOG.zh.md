# 更新日志 / История изменений / Changelog

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **注意：** 本更新日志为简体中文。其他语言版本请参见 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 页面。

本项目的所有重要变更均记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/)，
项目遵循 [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html)。

## [Unreleased]

### 计划中
- GUI 版本（WPF），以图形界面替代控制台
- 将 Windows Server 2022/2025 作为独立配置文件支持
- 通过 CBS.log 自动识别"损坏的"包
- 界面本地化（Deutsch、Français、Español、日本語）

## [2.5.1] — 2026-06-28

### 变更
- **所有文件中的版本号更新为 2.5.1**（README、Program.cs、.csproj、app.manifest、Resources/strings.*.json）
- **README 中的直接下载链接**指向 GitHub Releases v2.5.1（替代 releases/v2.5.0 分支）
- **修复 GitHub Actions workflow**：
  - 对 ARM64 设置 `PublishReadyToRun=false`（此前 crossgen2 会崩溃）
  - `fail-fast: false` — 一个架构失败不会取消其他架构的构建
  - 显式添加 `permissions: contents: write` 用于创建发布
  - 更新 actions 版本：checkout v5、setup-dotnet v5、action-gh-release v3

### 移除
- 4 个临时的 Dependabot 分支（清理）
- 针对 `github-actions` 生态系统的 Dependabot（仅保留 NuGet）— 此前会创建不存在版本的 PR

### 未变更（相对于 v2.5.0）
- 17 个原生 Win32 API
- 67 个菜单操作
- 14 个恢复模块
- 3 种界面语言（RU/EN/ZH）
- `docs/screenshots/` 中的 5 张截图

## [2.5.0] — 2026-06-28

### 新增
- **🌐 多语言界面（3 种语言）**：
  - 俄语（默认）
  - English
  - 简体中文（Chinese Simplified）
  - 翻译文件：`Resources/strings.{ru,en,zh}.json`（作为嵌入资源编译）
  - `Localizer` 类用于管理语言
  - 选择保存到注册表（`HKCU\SOFTWARE\SystemRestoreTool\Language`）
  - 菜单项 64「🌐 切换界面语言」
  - 横幅显示当前语言

- **📸 程序截图**位于 `docs/screenshots/`：
  - `01-main-menu-ru.png` — 主菜单（俄语）
  - `02-main-menu-en.png` — Main Menu（英语）
  - `03-main-menu-zh.png` — 主菜单（简体中文）
  - `04-integrity-check.png` — 完整性检查报告
  - `05-language-switch.png` — 语言切换

- **5 个新的原生 Windows API**：
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — 设备和驱动管理
  - `powrprof.dll` — 电源方案、电池、休眠
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — 网络诊断
  - COM WUA API（`Microsoft.Update.Session`）— 更新搜索

- **6 个新模块**：
  - `DeviceManager` — 设备列表、驱动签名验证、扫描硬件变更
  - `PowerOptionsManager` — 电源方案和休眠管理
  - `NetworkDiagnosticManager` — 检查 WU 服务器、重置 Winsock/TCP/IP/DNS/防火墙
  - `WerManager` — Windows 错误报告统计与清理
  - `WindowsUpdateAgentManager` — 通过 COM API 搜索更新

- **菜单从 44 项扩展到 67 项**（新增语言切换项）
- **自包含构建**（45 MB）— 无需安装 .NET
- 支持所有位数：x86、x64、ARM64
- 单文件 EXE 压缩（`EnableCompressionInSingleFile`）
- **`--help` / `--version`** 命令
- 友好的带边框错误提示
- **GitHub Actions workflow** 用于自动构建发布
- **`docs/api-reference.md`** — 详细的 Win32 API 参考
- **`SECURITY.md`** 和 **`.editorconfig`**

### 变更
- 程序横幅更新：显示所用的全部 17 个 API + 当前语言
- 日志记录器现在写入 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`，并附带时间戳
- `SignatureVerifier` 正确从签名中提取 Subject/Issuer
- `Microsoft.Dism` NuGet 封装针对实际的 3.2.0 API 重写
- `app.manifest` 更新了 DPI 设置

### 修复
- `RegSaveKeyW`/`RegRestoreKeyW` 现在具有正确的签名
- `DismProgressCallback` 接受 `DismProgress`（一个参数，而非三个）
- `BootRecoveryManager` — 修复了 `se` 变量的使用

## [2.0.0] — 2026-06-28

### 新增
- **架构完全重新设计** — 模块化结构
- **9 个恢复模块**：
  - `SystemRestorePointManager` — 通过 `srclient.dll` 创建还原点
  - `ServicesRepairManager` — 重启 40+ 个关键服务
  - `BootRecoveryManager` — BCD、bootmgr、winload
  - `RegistryRestoreManager` — 注册表配置单元备份
  - `WindowsUpdateRepairManager` — 重置 SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — 组件存储分析与清理
  - `EventLogManager` — 通过 `wevtapi.dll` 备份/清理日志
  - `UserEnvRestoreManager` — 用户配置文件、图标/字体缓存
  - `FileHashDatabaseManager` — SHA256/SHA1 快照用于对比

- **扩展的完整性检查**：
  - 40+ 个关键文件（DLL、EXE、驱动）
  - 深度扫描 System32 + drivers（约 5000 个文件）
  - 全面扫描 System32 + SysWOW64（约 10000 个文件）

- **新的原生 API**：
  - `kernel32.dll` — 文件、权限、重启、模块
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — 事件日志
  - `vssapi.dll` — Volume Shadow Copy

- **44 个菜单项**
- **自动模式**：`--super-full`、`--full`、`--verify`、`--scan-all`

## [1.0.0] — 2026-06-28

### 新增
- 程序的初始版本
- DISM API（`dismapi.dll`）：CheckHealth、ScanHealth、RestoreHealth、StartComponentCleanup
- SFC API（`sfc.dll`、`sfc_os.dll`）：SfcIsFileProtected、SfcGetNextProtectedFile、SfcSynchronousScan
- WinTrust API（`wintrust.dll`、`crypt32.dll`）：Authenticode 签名验证
- Microsoft.Dism NuGet 包作为替代方案
- 包含 15 项的基础菜单
- 日志写入 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
