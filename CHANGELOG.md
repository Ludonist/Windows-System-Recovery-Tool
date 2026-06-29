# Changelog / История изменений / 更新日志

> 🌐 **Note:** This changelog is in Russian. For English, see [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) pages. For Chinese, see [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) pages.
> 注：此更新日志为俄语。英语和中文版本请参见 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 页面。

Все заметные изменения этого проекта документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
и проект следует [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Планируется
- GUI-версия (WPF) с графикой вместо консоли
- Поддержка Windows Server 2022/2025 как отдельных профилей
- Автоматическое распознавание "битых" пакетов через CBS.log
- Локализация интерфейса (Italiano, العربية, हिन्दी, Tiếng Việt)
- YARA-rules сканер для поиска malware-паттернов
- AutoRuns-style детальный отчёт автозагрузки
- Анализ MiniDump с извлечением строк (strings-like)

## [2.6.0] — 2026-06-29

### Добавлено
- **🔍 FRST-Style Scanner** (`FrstScanner.cs`, ~1200 строк) — вдохновлён Farbar Recovery Scan Tool:
  - 20 секций полного скана системы
  - Процессы с подписями и MD5
  - Службы и драйверы (включая stopped)
  - Автозагрузка (Run keys + Startup folder)
  - Winlogon / AppInit_DLLs / Image File Execution Options
  - Запланированные задачи (рекурсивно через Schedule.Service COM)
  - Файл hosts + проверка подозрительных редиректов
  - TCP/UDP подключения
  - Установленные программы
  - Браузерные расширения (Chrome/Edge/Firefox/IE)
  - Ярлыки .lnk с проверкой целей
  - Правила брандмауэра (через COM HNetCfg.FwPolicy2)
  - Прокси-настройки
  - Подозрительные файлы в Temp/AppData
  - DNS-кэш
  - WMI-запросы (OS, CPU, BIOS, disk, network, video, sound)
  - Реестр Run keys deep (Policies\Explorer\Run)
  - Homepage/Search providers браузеров
  - Mounted devices + USBSTOR history
  - System Restore points
  - Установленные обновления (hotfixes)
  - Результат: `FRST_Report_{timestamp}.txt`

- **🛡️ AdvancedChecksManager** (~700 строк, 40+ проверок):
  - UAC, SmartScreen, Windows Defender, real-time protection
  - Antivirus update status, firewall state
  - Подписи всех EXE/SYS/DLL в System32, SysWOW64, drivers
  - Winlogon Userinit/Shell, AppInit_DLLs, IFEO Debugger hijacks
  - Run keys count, Policies\Explorer\Run, hosts redirects
  - SafeBoot keys, critical services running, services without path
  - Services with bad paths (Temp/AppData), disabled critical services
  - Winsock LSP, listening ports, pending TCP, DNS/DHCP state
  - Startup folder clean, no executables in Temp, no scripts in Startup
  - Chrome/Edge/Firefox extensions count, IE BHOs
  - pending.xml, COMPONENTS hive, CBS.log errors, Prefetch
  - Pagefile/hiberfile presence, PowerShell ExecutionPolicy
  - Pending reboot, disk space, WU pending, HVCI/VBS, TPM, SecureBoot
  - Last boot time, NTP sync

- **🔧 HostsFileManager**:
  - ShowHosts (numbered lines)
  - CheckSuspiciousEntries (30+ known Microsoft/Google/etc domains)
  - RestoreDefaultHosts (Microsoft's default content)
  - BackupHosts (timestamp)

- **🔧 StartupManager**:
  - ListAll: Run keys (HKLM/HKCU x86/x64) + Startup folder + Winlogon
  - Auto-detect suspicious: Temp paths, PowerShell -enc, mshta+http,
    regsvr32 squiblydoo, rundll32+javascript, scripts in Startup

- **💾 MiniDumpManager** (через `dbghelp.dll!MiniDumpWriteDump`):
  - CreateDump (один процесс, 6 типов: Normal/DataSegs/FullMemory/HandleData/ThreadInfo/UnloadedModules)
  - DumpAllProcesses (все запущенные процессы)
  - AnalyzeDumpFile (читает заголовок MDMP, извлекает SystemInfo, ModuleList)
  - CreateDumpInteractive (выбор процесса и типа через меню)

- **🌐 6 новых языков интерфейса** (всего 9):
  - Deutsch (Deutsch)
  - Français (Français)
  - Español (Español)
  - 日本語 (Japanese)
  - 한국어 (Korean)
  - Português (Portuguese)
  - Файлы `Resources/strings.{de,fr,es,ja,ko,pt}.json` (embedded resources)
  - `Localizer.cs` обновлён для 9 языков
  - `.csproj` обновлён для 9 embedded resources

- **📸 6 новых скриншотов** для новых языков (DE/FR/ES/JA/KO/PT)
- **📸 3 новых скриншота** функционала:
  - `07-frst-scan-report.png` — отчёт FRST-сканера
  - `08-advanced-checks.png` — расширенные проверки безопасности
  - `09-minidump.png` — MiniDump Manager

- **📚 Полная локализация документации (9 языков)**:
  - 9 README файлов (RU/EN/ZH/DE/FR/ES/JA/KO/PT)
  - 9 CHANGELOG файлов
  - 9 SECURITY файлов
  - 9 CONTRIBUTING файлов
  - 9 api-reference.md файлов (в docs/)

- **🌐 GitHub Pages** (`docs/index.html`):
  - Автоопределение языка браузера через `navigator.language`
  - 9 кнопок-флагов для переключения языка
  - Тёмная тема в стиле GitHub
  - Адаптивный дизайн (CSS Grid)
  - Все строки UI переведены на 9 языков через JS TRANSLATIONS
  - Секции: about, features, downloads, quick start, screenshots, APIs, links
  - Добавлены карточки FRST-Style Scanner и MiniDump Manager

### Изменено
- Меню расширено с 67 до 75 пунктов (FRST scan, Advanced checks, hosts, startup, MiniDump)
- Версия: v2.5.1 → v2.6.0
- README badge: `UI languages - 9` (было `RU | EN | 中文`)
- GitHub Pages: добавлены FRST и MiniDump в features grid
- Statistics: 14 → 19 модулей, 67 → 73 операций, 3 → 9 языков

### Подтверждено
- Сборка: `dotnet build -c Release` — ✅ успешно (3 warnings, 0 errors)
- GitHub Actions workflow: ✅ все 3 архитектуры собраны (x86/x64/ARM64)
- Release v2.6.0 опубликован с 12 ассетами

## [2.5.1] — 2026-06-28

### Изменено
- **Версия во всех файлах обновлена до 2.5.1** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Прямые ссылки на скачивание** в README указывают на GitHub Releases v2.5.1 (вместо ветки releases/v2.5.0)
- **GitHub Actions workflow** исправлен:
  - `PublishReadyToRun=false` для ARM64 (раньше крашился crossgen2)
  - `fail-fast: false` — одна архитектура не отменяет другие
  - Явные `permissions: contents: write` для создания релиза
  - Обновлены версии actions: checkout v5, setup-dotnet v5, action-gh-release v3

### Удалено
- 4 временные ветки Dependabot (cleanup)
- Dependabot для `github-actions` ecosystem (оставлен только для NuGet) — раньше создавал PR с несуществующими версиями

### Без изменений (относительно v2.5.0)
- 17 нативных Win32 API
- 67 операций в меню
- 14 модулей восстановления
- 3 языка интерфейса (RU/EN/ZH)
- 5 скриншотов в `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Добавлено
- **🌐 Многоязычный интерфейс (3 языка)**:
  - Русский (по умолчанию)
  - English
  - 简体中文 (Chinese Simplified)
  - Файлы переводов: `Resources/strings.{ru,en,zh}.json` (встроены как embedded resources)
  - Класс `Localizer` для управления языками
  - Сохранение выбора в реестре (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Пункт меню 64 «🌐 Сменить язык интерфейса»
  - Баннер показывает текущий язык

- **📸 Скриншоты программы** в `docs/screenshots/`:
  - `01-main-menu-ru.png` — главное меню (Русский)
  - `02-main-menu-en.png` — Main Menu (English)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — отчёт о проверке целостности
  - `05-language-switch.png` — смена языка

- **5 новых нативных Windows API**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — управление устройствами и драйверами
  - `powrprof.dll` — схемы питания, батарея, гибернация
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — диагностика сети
  - COM WUA API (`Microsoft.Update.Session`) — поиск обновлений

- **6 новых модулей**:
  - `DeviceManager` — список устройств, проверка подписей драйверов, scan hardware changes
  - `PowerOptionsManager` — управление схемами питания и гибернацией
  - `NetworkDiagnosticManager` — проверка серверов WU, сброс Winsock/TCP/IP/DNS/Firewall
  - `WerManager` — статистика и очистка отчётов об ошибках Windows
  - `WindowsUpdateAgentManager` — поиск обновлений через COM API

- **Меню расширено с 44 до 67 пунктов** (добавлен пункт смены языка)
- **Self-contained сборка** (45 MB) — не требует установки .NET
- Поддержка всех битностей: x86, x64, ARM64
- Сжатие single-file EXE (`EnableCompressionInSingleFile`)
- **`--help` / `--version`** команды
- Дружелюбные сообщения об ошибках с рамками
- **GitHub Actions workflow** для автоматической сборки релизов
- **`docs/api-reference.md`** — подробный справочник по Win32 API
- **`SECURITY.md`** и **`.editorconfig`**

### Изменено
- Обновлён баннер программы: показывает все 17 используемых API + текущий язык
- Логгер теперь пишет в `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` с таймстампом
- `SignatureVerifier` корректно извлекает Subject/Issuer из подписи
- `Microsoft.Dism` NuGet-обёртка переписана под реальный API 3.2.0
- `app.manifest` обновлён с DPI-настройками

### Исправлено
- `RegSaveKeyW`/`RegRestoreKeyW` теперь имеют корректные сигнатуры
- `DismProgressCallback` принимает `DismProgress` (один параметр, не три)
- `BootRecoveryManager` — исправлено использование переменной `se`

## [2.0.0] — 2026-06-28

### Добавлено
- **Полная переработка архитектуры** — модульная структура
- **9 модулей восстановления**:
  - `SystemRestorePointManager` — точки восстановления через `srclient.dll`
  - `ServicesRepairManager` — перезапуск 40+ критических служб
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — бэкап кустов реестра
  - `WindowsUpdateRepairManager` — сброс SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — анализ и очистка хранилища компонентов
  - `EventLogManager` — бэкап/очистка журналов через `wevtapi.dll`
  - `UserEnvRestoreManager` — профили пользователей, кэш иконок/шрифтов
  - `FileHashDatabaseManager` — снимок SHA256/SHA1 для сравнения

- **Расширенная проверка целостности**:
  - 40+ критических файлов (DLL, EXE, драйверы)
  - Глубокая проверка System32 + drivers (~5000 файлов)
  - Полная проверка System32 + SysWOW64 (~10000 файлов)

- **Новые нативные API**:
  - `kernel32.dll` — файлы, привилегии, перезагрузка, модули
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — журналы событий
  - `vssapi.dll` — Volume Shadow Copy

- **44 пункта меню**
- **Авто-режимы**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Добавлено
- Базовая версия программы
- DISM API (`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- SFC API (`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- WinTrust API (`wintrust.dll`, `crypt32.dll`): проверка подписей Authenticode
- Microsoft.Dism NuGet-пакет как альтернативный путь
- Базовое меню из 15 пунктов
- Логирование в `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.6.0...HEAD
[2.6.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.6.0
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
