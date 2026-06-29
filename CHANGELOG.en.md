# Changelog / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **Note:** This changelog is in English. For other languages, see the [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) pages.

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
and the project follows [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Planned
- GUI version (WPF) with graphics instead of a console
- Support for Windows Server 2022/2025 as separate profiles
- Automatic detection of "broken" packages via CBS.log
- Interface localization (Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### Changed
- **Version updated to 2.5.1 in all files** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Direct download links** in README now point to GitHub Releases v2.5.1 (instead of the releases/v2.5.0 branch)
- **GitHub Actions workflow** fixed:
  - `PublishReadyToRun=false` for ARM64 (previously crossgen2 crashed)
  - `fail-fast: false` — one architecture failing does not cancel the others
  - Explicit `permissions: contents: write` for creating the release
  - Updated action versions: checkout v5, setup-dotnet v5, action-gh-release v3

### Removed
- 4 temporary Dependabot branches (cleanup)
- Dependabot for the `github-actions` ecosystem (only NuGet remains) — previously created PRs with non-existent versions

### Unchanged (relative to v2.5.0)
- 17 native Win32 APIs
- 67 menu operations
- 14 recovery modules
- 3 interface languages (RU/EN/ZH)
- 5 screenshots in `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Added
- **🌐 Multilingual interface (3 languages)**:
  - Russian (default)
  - English
  - 简体中文 (Chinese Simplified)
  - Translation files: `Resources/strings.{ru,en,zh}.json` (embedded as embedded resources)
  - `Localizer` class for language management
  - Choice persisted in the registry (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Menu item 64 "🌐 Change interface language"
  - Banner displays the current language

- **📸 Program screenshots** in `docs/screenshots/`:
  - `01-main-menu-ru.png` — main menu (Russian)
  - `02-main-menu-en.png` — Main Menu (English)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — integrity check report
  - `05-language-switch.png` — language switching

- **5 new native Windows APIs**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — device and driver management
  - `powrprof.dll` — power schemes, battery, hibernation
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — network diagnostics
  - COM WUA API (`Microsoft.Update.Session`) — update search

- **6 new modules**:
  - `DeviceManager` — device list, driver signature verification, scan hardware changes
  - `PowerOptionsManager` — power scheme and hibernation management
  - `NetworkDiagnosticManager` — check WU servers, reset Winsock/TCP/IP/DNS/Firewall
  - `WerManager` — Windows error report statistics and cleanup
  - `WindowsUpdateAgentManager` — search for updates via the COM API

- **Menu expanded from 44 to 67 items** (language switch item added)
- **Self-contained build** (45 MB) — does not require .NET to be installed
- Support for all bitnesses: x86, x64, ARM64
- Single-file EXE compression (`EnableCompressionInSingleFile`)
- **`--help` / `--version`** commands
- Friendly error messages with frames
- **GitHub Actions workflow** for automated release builds
- **`docs/api-reference.md`** — detailed Win32 API reference
- **`SECURITY.md`** and **`.editorconfig`**

### Changed
- Program banner updated: shows all 17 APIs used + current language
- Logger now writes to `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` with a timestamp
- `SignatureVerifier` correctly extracts Subject/Issuer from signatures
- `Microsoft.Dism` NuGet wrapper rewritten for the actual 3.2.0 API
- `app.manifest` updated with DPI settings

### Fixed
- `RegSaveKeyW`/`RegRestoreKeyW` now have correct signatures
- `DismProgressCallback` accepts `DismProgress` (one parameter, not three)
- `BootRecoveryManager` — fixed usage of the `se` variable

## [2.0.0] — 2026-06-28

### Added
- **Complete architecture redesign** — modular structure
- **9 recovery modules**:
  - `SystemRestorePointManager` — restore points via `srclient.dll`
  - `ServicesRepairManager` — restart 40+ critical services
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — registry hive backup
  - `WindowsUpdateRepairManager` — reset SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — component store analysis and cleanup
  - `EventLogManager` — log backup/cleanup via `wevtapi.dll`
  - `UserEnvRestoreManager` — user profiles, icon/font cache
  - `FileHashDatabaseManager` — SHA256/SHA1 snapshot for comparison

- **Extended integrity check**:
  - 40+ critical files (DLL, EXE, drivers)
  - Deep scan of System32 + drivers (~5000 files)
  - Full scan of System32 + SysWOW64 (~10000 files)

- **New native APIs**:
  - `kernel32.dll` — files, privileges, reboot, modules
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — event logs
  - `vssapi.dll` — Volume Shadow Copy

- **44 menu items**
- **Auto modes**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Added
- Initial version of the program
- DISM API (`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- SFC API (`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- WinTrust API (`wintrust.dll`, `crypt32.dll`): Authenticode signature verification
- Microsoft.Dism NuGet package as an alternative path
- Basic menu with 15 items
- Logging to `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
