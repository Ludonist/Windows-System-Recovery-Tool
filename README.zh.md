# 🔧 Windows 系统恢复工具

<div align="center">

🌐 **可用语言 / Доступные языки / Available languages:**
[🇷🇺 Русский](README.md) · [🇬🇧 English](README.en.md) · [🇨🇳 简体中文](README.zh.md)

---

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64%20%7C%20ARM64-orange)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Version](https://img.shields.io/badge/version-2.5.1-brightgreen)
![Language](https://img.shields.io/badge/lang-C%23%2010-success)
![Languages](https://img.shields.io/badge/UI%20languages-RU%20%7C%20EN%20%7C%20中文-red)

**通过直接调用 Win32 API 恢复 Windows 10/11 系统文件的专业工具**

🌐 **多语言界面**：Русский · English · 简体中文

[功能](#-功能) ·
[截图](#-截图) ·
[安装](#-安装) ·
[使用](#-使用) ·
[架构](#-架构) ·
[API](#-使用的-win32-api) ·
[构建](#-从源代码构建) ·
[贡献](#-贡献)

</div>

---

## 📖 关于

**Windows System Recovery Tool** 是一款 Windows 10/11 控制台程序，通过**直接调用 Windows API**（`dismapi.dll`、`sfc.dll`、`wintrust.dll` 等）恢复和验证系统文件完整性 — 而不是通过外部命令 `cmd.exe` / `dism.exe` / `sfc.exe`。

### 主要特点

- 🎯 **67 项**恢复和诊断操作，集成在一个菜单中
- 🔌 **17 个原生 Windows API** 通过 P/Invoke（无 shell 命令）
- 🛡️ **文件篡改检测**：WinVerifyTrust + 证书发布者
- 📊 **SHA256/SHA1 快照**用于比较系统状态随时间的变化
- 🧩 **Microsoft.Dism NuGet** 作为替代的托管路径
- 📋 **详细日志**记录到 `%LOCALAPPDATA%\SystemRestoreTool\`
- ⚡ **自包含构建** — 无需安装 .NET
- 🌐 **多语言界面**：Русский · English · 简体中文

### 该工具解决的问题

| 症状 | 解决方案 |
|---------|----------|
| `sfc /scannow` 发现损坏 | DISM RestoreHealth + SFC /ScanNow |
| 怀疑病毒篡改 DLL | 对所有关键文件执行 WinVerifyTrust |
| Windows Update 损坏 | 重置 SoftwareDistribution + 重新注册 DLL |
| 服务无法启动 | 通过 SCM 重启 40+ 个关键服务 |
| 引导加载程序故障 | BCD 检查和恢复 |
| 没有还原点 | 通过 `SRSetRestorePoint` 创建 |
| 图标/字体缓存损坏 | 重建 IconCache.db / FNTCACHE.DAT |
| 系统完整性疑虑 | SHA256 快照 + 与基准比较 |

---

## ✨ 功能

### 1. 通过 DISM API 恢复 (`dismapi.dll`)

| 操作 | 控制台等效命令 |
|----------|---------------------|
| CheckHealth | `Dism /Online /Cleanup-Image /CheckHealth` |
| ScanHealth | `Dism /Online /Cleanup-Image /ScanHealth` |
| RestoreHealth | `Dism /Online /Cleanup-Image /RestoreHealth` |
| StartComponentCleanup | `Dism /Online /Cleanup-Image /StartComponentCleanup` |
| StartComponentCleanup + ResetBase | `... /StartComponentCleanup /ResetBase` |

### 2. 通过原生 `sfc_os.dll` 执行 SFC

| 操作 | 等效命令 |
|----------|------------|
| SfcSynchronousScan (ScanAndRepair) | `Sfc /ScanNow` |
| SfcSynchronousScan (VerifyOnly) | `Sfc /VerifyOnly` |
| SfcIsFileProtected | （无等效） |
| SfcGetNextProtectedFile | （无等效） |

### 3. 完整性检查

- **40+ 个关键文件** — kernel32.dll、ntdll.dll、winlogon.exe、lsass.exe、csrss.exe、smss.exe、驱动程序（tcpip.sys、ntfs.sys、volmgr.sys、ACPI.sys、hal.dll 等）
- **深度检查 System32 + 驱动程序** — 所有 .dll/.exe/.sys（约 5000 个文件）
- **完整检查 System32 + SysWOW64** — 约 10000 个文件
- 对每个文件：WFP 保护、Authenticode 签名、发布者（Microsoft vs 第三方）

### 4. 恢复模块（14 个模块）

| 模块 | 用途 | API |
|--------|---------|-----|
| `SystemRestorePointManager` | 还原点 | `srclient.dll!SRSetRestorePoint` |
| `ServicesRepairManager` | 重启 40+ 个服务 | `advapi32.dll` SCM |
| `BootRecoveryManager` | BCD、bootmgr、winload | WMI + bcdedit |
| `RegistryRestoreManager` | 注册表配置单元备份 | `advapi32.dll!RegSaveKey` |
| `WindowsUpdateRepairManager` | WU 重置 | SCM + File API |
| `WinSxsRepairManager` | WinSxS 清理 | `dismapi.dll!DismAnalyzeComponentStore` |
| `EventLogManager` | 事件日志 | `wevtapi.dll!EvtClearLog` |
| `UserEnvRestoreManager` | 配置文件、缓存 | advapi32 + File API |
| `FileHashDatabaseManager` | SHA256 快照 | `System.Security.Cryptography` |
| `DeviceManager` | 设备 | `setupapi.dll` |
| `PowerOptionsManager` | 电源方案 | `powrprof.dll` |
| `NetworkDiagnosticManager` | 网络 | `winhttp.dll` + `ws2_32.dll` |
| `WerManager` | WER 报告 | `wer.dll` |
| `WindowsUpdateAgentManager` | 更新搜索 | COM WUA API |

---

## 📸 截图

### 主菜单（Русский）

![Main Menu RU](docs/screenshots/01-main-menu-ru.png)

### 主菜单（English）

![Main Menu EN](docs/screenshots/02-main-menu-en.png)

### 主菜单（简体中文）

![Main Menu ZH](docs/screenshots/03-main-menu-zh.png)

### 系统文件完整性检查

![Integrity Check](docs/screenshots/04-integrity-check.png)

### 切换界面语言

![Language Switch](docs/screenshots/05-language-switch.png)

---

## 📥 安装

### 选项 1：下载预构建的 EXE（推荐）

**直接下载链接**（最新构建，无依赖）：

| 架构 | Standalone（包含 .NET） | Compact（需要 .NET 6） |
|--------------|----------------------------:|--------------------------:|
| **x64**（Intel/AMD） | [standalone-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64.zip) （约 40 MB） | [compact-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64-compact.zip) （约 7 MB） |
| **x86**（32 位） | [standalone-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86.zip) （约 37 MB） | [compact-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86-compact.zip) （约 7 MB） |
| **ARM64**（Surface/Qualcomm） | [standalone-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64.zip) （约 33 MB） | [compact-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64-compact.zip) （约 6 MB） |

**Standalone 版本**（推荐）— 包含 .NET 6 运行时，无需其他依赖。

**Compact 版本** — 需要安装 [.NET Desktop Runtime 6.0+](https://dotnet.microsoft.com/download/dotnet/6.0)。

### 选项 2：GitHub Releases

所有二进制文件也可在 [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) 页面获取，以及用于完整性验证的 SHA256 校验和。

### 选项 3：从源代码构建

参见[从源代码构建](#-从源代码构建)。

### 要求

- **操作系统**：Windows 10（内部版本 19041+，2020 年 5 月更新）或 Windows 11
- **架构**：x64 / x86 / ARM64
- **权限**：管理员（清单已指定）
- **Compact 构建**：.NET Desktop Runtime 6.0+

### 验证完整性

下载后验证 SHA256：
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```
与 [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) 页面上每个存档旁边的 `.sha256` 文件进行比较。

---

## 🚀 使用

### 交互模式

以管理员身份运行 `SystemRestoreTool.exe`。将打开一个 67 项菜单：

```
╔══════════════════════════════════════════════════════════════════════╗
║                                                                      ║
║   WINDOWS 系统恢复工具  v2.5.1                                       ║
║   直接恢复 Windows 10/11 系统文件                                    ║
║   ...                                                                ║
╚══════════════════════════════════════════════════════════════════════╝
[+] 管理员权限：已确认
[i] 日志文件: C:\Users\...\AppData\Local\SystemRestoreTool\srt_20260628_193015.log

主菜单 — 系统恢复工具 v2.5

  [ 1] ★ 超完整周期：还原点 + DISM + SFC + 服务 + WU + 缓存
  [ 2]   完整周期（原生 API）
  [ 3]   完整周期（Microsoft.Dism NuGet）
  ...
  [67]   退出

选择操作: _
```

### 自动模式（用于脚本）

```cmd
:: 超完整周期：还原点 + DISM + SFC + 服务 + WU + 缓存
SystemRestoreTool.exe --super-full

:: 同上 + ResetBase WinSxS
SystemRestoreTool.exe --super-full --reset-base

:: 完整 DISM + SFC 周期
SystemRestoreTool.exe --full

:: 检查 40+ 个关键文件
SystemRestoreTool.exe --verify

:: 深度检查 System32 + 驱动程序
SystemRestoreTool.exe --scan-all
```

### 日志记录

所有操作同时写入控制台（带颜色）和文件：
```
%LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log
```

### 🌐 多语言界面

程序支持 **3 种界面语言**：

| 代码 | 原生名称 | 英文名称 |
|------|-------------|--------------|
| `ru` | Русский | Russian |
| `en` | English | English |
| `zh` | 简体中文 | Chinese (Simplified) |

**切换语言：**
1. 在主菜单中选择 **64**（"🌐 切换界面语言"）
2. 选择所需语言（1-3）
3. 选择**保存到注册表**（`HKCU\SOFTWARE\SystemRestoreTool\Language`），下次启动时应用

翻译文件：`Resources/strings.{ru,en,zh}.json`。它们作为嵌入资源嵌入在 EXE 中，因此无需复制其他内容。

**添加新语言：**
1. 将 `Resources/strings.en.json` 复制到 `Resources/strings.xx.json`（其中 `xx` 是语言代码）
2. 翻译所有值
3. 将代码添加到 `Localizer.SupportedLanguages` 和 `LanguageNames`
4. 在 `.csproj` 中添加条目作为 `<EmbeddedResource>`

---

## 🏗 架构

```
Windows-System-Recovery-Tool/
├── SystemRestoreTool.csproj      .NET 6.0, x64/x86/ARM64, Microsoft.Dism NuGet
├── app.manifest                  requireAdministrator
├── Program.cs                    入口点，67 项菜单
│
├── Api/                          P/Invoke 层（原生 Windows DLL）
│   ├── DismNativeApi.cs          dismapi.dll (DISM API)
│   ├── SfcNativeApi.cs           sfc.dll + sfc_os.dll + srclient.dll
│   ├── WinTrustNativeApi.cs      wintrust.dll + crypt32.dll（签名）
│   ├── Kernel32NativeApi.cs      kernel32.dll + advapi32.dll
│   ├── SrclientNativeApi.cs      srclient.dll（还原点）
│   ├── ServicesNativeApi.cs      advapi32.dll（服务控制管理器）
│   ├── EventLogNativeApi.cs      advapi32.dll + wevtapi.dll
│   ├── VssNativeApi.cs           vssapi.dll（卷影副本）
│   ├── WerNativeApi.cs           wer.dll（Windows 错误报告）
│   ├── SetupApiNativeApi.cs      setupapi.dll（设备）
│   ├── PowerNativeApi.cs         powrprof.dll（电源）
│   └── NetNativeApi.cs           winhttp.dll + wininet.dll + ws2_32.dll
│
├── Engine/                       高级逻辑
│   ├── SystemRestoreEngine.cs    恢复周期编排
│   ├── DismManagedWrapper.cs     通过 Microsoft.Dism NuGet
│   ├── SignatureVerifier.cs      高级签名验证
│   ├── IntegrityChecker.cs       检查 40+ 个关键文件 + 所有 System32
│   └── Modules/                  专用模块（14 个）
│       ├── SystemRestorePointManager.cs
│       ├── ServicesRepairManager.cs
│       ├── BootRecoveryManager.cs
│       ├── RegistryRestoreManager.cs
│       ├── WindowsUpdateRepairManager.cs
│       ├── WinSxsRepairManager.cs
│       ├── EventLogManager.cs
│       ├── UserEnvRestoreManager.cs
│       ├── FileHashDatabaseManager.cs
│       ├── DeviceManager.cs
│       ├── PowerOptionsManager.cs
│       ├── NetworkDiagnosticManager.cs
│       ├── WerManager.cs
│       └── WindowsUpdateAgentManager.cs
│
├── Resources/                    🌐 本地化（嵌入资源）
│   ├── strings.ru.json           Русский（默认）
│   ├── strings.en.json           English
│   └── strings.zh.json           简体中文
│
├── Utils/
│   ├── ConsoleHelper.cs          UI 助手（菜单、权限、输入）
│   ├── Logger.cs                 文件 + 彩色控制台日志记录器
│   └── Localizer.cs              语言加载和切换
│
├── docs/
│   ├── api-reference.md          详细的 Win32 API 参考
│   └── screenshots/              程序截图 (PNG)
│
├── .github/
│   ├── repo-metadata.json        Topics 和仓库描述
│   └── workflows/
│       └── build-release.yml     CI: 构建 x86/x64/ARM64 + Release
│
├── .gitignore                    标准 .NET gitignore
├── .editorconfig                 代码风格
├── LICENSE                       MIT
├── CHANGELOG.md                  更改历史
├── CONTRIBUTING.md               贡献者规则
├── SECURITY.md                   安全策略
├── README.md                     俄语文档（默认）
├── README.en.md                  英语文档
└── README.zh.md                  中文文档
```

### 代码统计

- **约 7200 行 C#** 代码
- **37 个文件**，分布在 6 个目录中
- **17 个原生 DLL** 通过 P/Invoke
- **14 个恢复模块**
- **67 个菜单项**
- **3 种界面语言** (RU/EN/ZH)

---

## 🔌 使用的 Win32 API

所有操作使用**直接 P/Invoke 调用**（无 shell 命令）：

| DLL | 用途 |
|-----|-----------|
| `dismapi.dll` | DISM: CheckHealth / ScanHealth / RestoreHealth / StartComponentCleanup |
| `sfc.dll` | SfcIsFileProtected / SfcGetNextProtectedFile |
| `sfc_os.dll` | SfcSynchronousScan (= Sfc /ScanNow) |
| `wintrust.dll` | WinVerifyTrust（Authenticode 签名）|
| `crypt32.dll` | CryptQueryObject / CertGetNameString（发布者）|
| `srclient.dll` | SRSetRestorePoint（还原点）|
| `advapi32.dll` | SCM（服务）、注册表、权限 |
| `kernel32.dll` | 文件、重启、App Recovery/Restart |
| `wevtapi.dll` | 事件日志 (EvtClearLog) |
| `vssapi.dll` | 卷影副本 |
| `wer.dll` | Windows 错误报告 |
| `setupapi.dll` | 设备安装程序 |
| `cfgmgr32.dll` | CM_Reenumerate_DevNode |
| `powrprof.dll` | 电源方案、电池 |
| `winhttp.dll` | HTTP 服务器检查 |
| `wininet.dll` | InternetGetConnectedState |
| `ws2_32.dll` | Winsock (WSAStartup) |
| **Microsoft.Dism NuGet** | 托管 DISM API 包装器 |
| **COM WUA API** | Windows Update Agent (`Microsoft.Update.Session`) |

详情见 [docs/api-reference.md](docs/api-reference.md)。

---

## 🛠 从源代码构建

### 要求

- **Windows 10**（内部版本 19041+）或 **Windows 11**
- **.NET 6.0 SDK** 或更高版本 — https://dotnet.microsoft.com/download
- （可选）Visual Studio 2022 / JetBrains Rider / VS Code

### 步骤

```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

结果：`bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 单文件发布

```bash
# Compact（目标机器需要 .NET 6，约 27 MB）
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Self-contained（无依赖，约 45 MB）
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### 支持的架构

```bash
# x64（Intel/AMD 64 位）— 主要
dotnet publish -c Release -r win-x64 ...

# x86（32 位）— 用于旧系统
dotnet publish -c Release -r win-x86 ...

# ARM64（Surface Pro X、Snapdragon 笔记本）
dotnet publish -c Release -r win-arm64 -p:PublishReadyToRun=false ...
```

---

## 📋 检查文件列表

### 40+ 个关键文件 (`IntegrityChecker.CriticalFiles`)

- **基础 DLL**：kernel32.dll、ntdll.dll、user32.dll、advapi32.dll、shell32.dll、ole32.dll、gdi32.dll、msvcrt.dll、ws2_32.dll、wininet.dll、urlmon.dll、combase.dll、sechost.dll、rpcrt4.dll、setupapi.dll
- **加载器和进程**：winload.exe、winresume.exe、winlogon.exe、csrss.exe、services.exe、lsass.exe、lsaiso.exe、smss.exe、svchost.exe、spoolsv.exe、wininit.exe、dwm.exe
- **加密**：bcrypt.dll、ncrypt.dll、schannel.dll、crypt32.dll、cryptbase.dll、cryptsp.dll、wintrust.dll、msasn1.dll
- **DISM/SFC**：sfc.dll、sfc_os.dll、dismapi.dll、dism.exe、sfc.exe
- **管理**：mmc.exe、powershell.exe、cmd.exe、regedit.exe、taskmgr.exe、eventvwr.exe
- **Shell**：explorer.exe、shdocvw.dll、shellstyle.dll、themecpl.dll、themeui.dll
- **内核驱动程序**：tcpip.sys、ntfs.sys、volmgr.sys、volmgrx.sys、disk.sys、partmgr.sys、ACPI.sys、hal.dll、ndis.sys、http.sys、Wdf01000.sys、ksecdd.sys、ksecpkg.sys、msisadrv.sys、pci.sys、mountmgr.sys、fltMgr.sys、luafv.sys、mrxsmb.sys、mup.sys
- **.NET Framework**：clr.dll、mscorlib.dll、System.dll
- **WinRT**：Windows.Foundation.winmd、WinRTTraceLogger.dll
- **WSL**：lxss.dll、wslapi.dll

### 深度检查（System32 + 驱动程序）

- `C:\Windows\System32` 中的所有 `.dll`、`.exe`、`.sys`、`.cpl`
- `C:\Windows\System32\drivers` 中的所有 `.sys`
- UMDF 驱动程序：`C:\Windows\System32\drivers\UMDF`
- Windows Defender 驱动程序：`C:\Windows\System32\drivers\wd`
- `C:\Windows` 中的所有 `.exe`、`.dll`

### 完整检查

另外：`C:\Windows\SysWOW64`（系统库的 32 位版本）

---

## 📊 示例报告

```
[i] 检查的文件:                40
[i] WFP 保护:                  38
[i] 无 WFP 保护:                2
[+] 有效的 Microsoft 签名:      37
[!] 第三方签名:                 0
[!] 无签名:                     0
[X] 签名错误:                   0
[X] 缺失:                       1
[i] 检查持续时间:               18.5 秒

所有关键文件完整 — 未检测到篡改。
```

---

## 🔒 安全

- ✅ 程序**不会启动** `cmd.exe`、`dism.exe`、`sfc.exe`、`powershell.exe`（除了 `bcdedit.exe` 和 `netsh.exe`，用于没有 P/Invoke 等效的操作）
- ✅ 所有操作使用原生 Win32 API
- ✅ 需要管理员权限（清单 `requireAdministrator`）
- ✅ 日志**本地**写入 `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ 不通过网络发送数据
- ✅ 根据 MIT 许可证开源 — 您可以审计代码

## ⚠️ 限制

1. **仅限 Windows 10/11** — 对于 Windows 7/8，需要更改 `TargetFramework`
2. **RestoreHealth** 可能需要访问 Windows Update 或安装介质
3. **SfcSynchronousScan** 未文档化 — 在某些版本上可能需要额外标志
4. **ResetBase** 不可逆 — 之后无法卸载已安装的更新
5. **bcdedit / bootrec** 没有 P/Invoke 等效项 — 通过 `Process.Start` 直接调用（不使用 cmd.exe）
6. **RegSaveKey** 对于活动配置单元可能会失败（文件被系统锁定）— 在这种情况下，使用直接文件复制

---

## 📈 路线图

- [ ] GUI 版本（WPF），带图形界面
- [ ] 界面本地化（Deutsch / Français / Español / 日本語）
- [ ] 通过 CBS.log 自动检测"损坏的"包
- [ ] 支持 Windows Server 2022/2025 作为单独的配置文件
- [ ] 任务计划程序（例如，每周完整性检查）
- [ ] 将报告导出为 HTML/PDF

完整列表参见 [打开的 issue](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues)。

---

## 🤝 贡献

欢迎提交 Pull Request！规则参见 [CONTRIBUTING.md](CONTRIBUTING.md)。

特别需要：
- 🌍 将界面翻译成其他语言
- 🐛 带有真实崩溃日志的 Bug 报告
- 📚 记录不同 Windows 版本上的边缘情况
- 🧪 在 ARM64 设备（Surface Pro X 等）上测试

---

## 📄 许可证

MIT 许可证 — 参见 [LICENSE](LICENSE)。

您可以自由使用、修改和分发此代码。

---

## 🙏 致谢

- [Microsoft.Dism NuGet](https://github.com/jeffkl/ManagedDism) — Jeff Kluge 的托管 DISM API 包装器
- [Microsoft Learn Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list) — Windows API 文档
- [pinvoke.net](https://www.pinvoke.net/) — P/Invoke 签名参考

---

## ⭐ Star 历史

如果此程序对您有用 — 请给仓库点 ⭐！

<div align="center">

**[⬆ 返回顶部](#-windows-系统恢复工具)**

用 ❤️ 为 Windows 高级用户制作

</div>
