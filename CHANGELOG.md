# Changelog

Все заметные изменения этого проекта документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
и проект следует [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Планируется
- GUI-версия (WPF) с графикой вместо консоли
- Поддержка Windows Server 2022/2025 как отдельных профилей
- Автоматическое распознавание "битых" пакетов через CBS.log
- Локализация интерфейса (English / 中文 / Deutsch)

## [2.5.0] — 2026-06-28

### Добавлено
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

- **Меню расширено с 44 до 66 пунктов**
- **Self-contained сборка** (45 MB) — не требует установки .NET
- Поддержка всех битностей: x86, x64, ARM64
- Сжатие single-file EXE (`EnableCompressionInSingleFile`)

### Изменено
- Обновлён баннер программы: теперь показывает все 17 используемых API
- Логгер теперь пишет в `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` с таймстампом
- `SignatureVerifier` корректно извлекает Subject/Issuer из подписи
- `Microsoft.Dism` NuGet-обёртка переписана под реальный API 3.2.0

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

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.0...HEAD
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
