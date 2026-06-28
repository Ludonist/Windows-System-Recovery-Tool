# 🔧 Windows System Recovery Tool

<div align="center">

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64%20%7C%20ARM64-orange)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Version](https://img.shields.io/badge/version-2.5.0-brightgreen)
![Language](https://img.shields.io/badge/lang-C%23%2010-success)
![Languages](https://img.shields.io/badge/UI%20languages-RU%20%7C%20EN%20%7C%20中文-red)

**Профессиональный инструмент восстановления системных файлов Windows 10/11 через прямые вызовы Win32 API**

🌐 **Многоязычный интерфейс**: Русский · English · 简体中文

[Возможности](#-возможности) •
[Скриншоты](#-скриншоты) •
[Установка](#-установка) •
[Использование](#-использование) •
[Архитектура](#-архитектура) •
[API](#-используемые-win32-api) •
[Сборка](#-сборка-из-исходников) •
[Contributing](#-contributing)

</div>

---

## 📖 Описание

**Windows System Recovery Tool** — это консольная программа для Windows 10/11, которая восстанавливает и проверяет целостность системных файлов через **прямые вызовы Windows API** (`dismapi.dll`, `sfc.dll`, `wintrust.dll` и др.), а не через внешние команды `cmd.exe` / `dism.exe` / `sfc.exe`.

### Ключевые особенности

- 🎯 **66 операций** восстановления и диагностики в одном меню
- 🔌 **17 нативных Windows API** через P/Invoke (без shell-команд)
- 🛡️ **Проверка подмены файлов**: WinVerifyTrust + издатель сертификата
- 📊 **Снимки SHA256/SHA1** для сравнения состояния системы во времени
- 🧩 **Microsoft.Dism NuGet** как альтернативный управляемый путь
- 📋 **Подробное логирование** в `%LOCALAPPDATA%\SystemRestoreTool\`
- ⚡ **Self-contained сборка** — не требует установки .NET

### Программа решает задачи

| Симптом | Решение |
|---------|---------|
| `sfc /scannow` находит повреждения | DISM RestoreHealth + SFC /ScanNow |
| Подозрение на вирусную подмену DLL | WinVerifyTrust всех критических файлов |
| Windows Update не работает | Сброс SoftwareDistribution + перерегистрация DLL |
| Службы не запускаются | Перезапуск 40+ критических служб через SCM |
| Сбой загрузчика | Проверка и восстановление BCD |
| Нет точки восстановления | Создание через `SRSetRestorePoint` |
| Повреждён кэш иконок/шрифтов | Перестроение IconCache.db / FNTCACHE.DAT |
| Сомнения в целостности системы | Снимок SHA256 + сравнение с эталоном |

---

## ✨ Возможности

### 1. Восстановление через DISM API (`dismapi.dll`)

| Операция | Эквивалент консоли |
|----------|---------------------|
| CheckHealth | `Dism /Online /Cleanup-Image /CheckHealth` |
| ScanHealth | `Dism /Online /Cleanup-Image /ScanHealth` |
| RestoreHealth | `Dism /Online /Cleanup-Image /RestoreHealth` |
| StartComponentCleanup | `Dism /Online /Cleanup-Image /StartComponentCleanup` |
| StartComponentCleanup + ResetBase | `... /StartComponentCleanup /ResetBase` |

### 2. SFC через нативный `sfc_os.dll`

| Операция | Эквивалент |
|----------|------------|
| SfcSynchronousScan (ScanAndRepair) | `Sfc /ScanNow` |
| SfcSynchronousScan (VerifyOnly) | `Sfc /VerifyOnly` |
| SfcIsFileProtected | (нет аналога) |
| SfcGetNextProtectedFile | (нет аналога) |

### 3. Проверка целостности

- **40+ критических файлов** — kernel32.dll, ntdll.dll, winlogon.exe, lsass.exe, csrss.exe, smss.exe, драйверы (tcpip.sys, ntfs.sys, volmgr.sys, ACPI.sys, hal.dll, ...) и др.
- **Глубокая проверка System32 + drivers** — все .dll/.exe/.sys (~5000 файлов)
- **Полная проверка System32 + SysWOW64** — ~10000 файлов
- Для каждого файла: WFP-защита, подпись Authenticode, издатель (Microsoft vs 3rd-party)

### 4. Модули восстановления (11 модулей)

| Модуль | Назначение | API |
|--------|-----------|-----|
| `SystemRestorePointManager` | Точки восстановления | `srclient.dll!SRSetRestorePoint` |
| `ServicesRepairManager` | Перезапуск 40+ служб | `advapi32.dll` SCM |
| `BootRecoveryManager` | BCD, bootmgr, winload | WMI + bcdedit |
| `RegistryRestoreManager` | Бэкап кустов реестра | `advapi32.dll!RegSaveKey` |
| `WindowsUpdateRepairManager` | Сброс WU | SCM + File API |
| `WinSxsRepairManager` | Очистка WinSxS | `dismapi.dll!DismAnalyzeComponentStore` |
| `EventLogManager` | Журналы событий | `wevtapi.dll!EvtClearLog` |
| `UserEnvRestoreManager` | Профили, кэши | advapi32 + File API |
| `FileHashDatabaseManager` | Снимки SHA256 | `System.Security.Cryptography` |
| `DeviceManager` | Устройства | `setupapi.dll` |
| `PowerOptionsManager` | Схемы питания | `powrprof.dll` |
| `NetworkDiagnosticManager` | Сеть | `winhttp.dll` + `ws2_32.dll` |
| `WerManager` | WER-отчёты | `wer.dll` |
| `WindowsUpdateAgentManager` | Поиск обновлений | COM WUA API |

---

## 📸 Скриншоты

### Главное меню (Русский)

![Main Menu RU](docs/screenshots/01-main-menu-ru.png)

### Main Menu (English)

![Main Menu EN](docs/screenshots/02-main-menu-en.png)

### 主菜单 (简体中文)

![Main Menu ZH](docs/screenshots/03-main-menu-zh.png)

### Проверка целостности системных файлов

![Integrity Check](docs/screenshots/04-integrity-check.png)

### Смена языка интерфейса

![Language Switch](docs/screenshots/05-language-switch.png)

---

## 📥 Установка

### Вариант 1: Скачать готовый EXE (рекомендуется)

**Прямые ссылки на бинарники** (свежая сборка, ничего не требует):

| Architecture | Standalone (включает .NET) | Compact (требует .NET 6) |
|--------------|----------------------------:|--------------------------:|
| **x64** (Intel/AMD) | [standalone-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/raw/releases/v2.5.0/releases/SystemRestoreTool-standalone-win-x64.zip) (~40 MB) | [compact-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/latest) (~7 MB) |
| **x86** (32-bit) | [standalone-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/raw/releases/v2.5.0/releases/SystemRestoreTool-standalone-win-x86.zip) (~37 MB) | [compact-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/latest) (~7 MB) |
| **ARM64** (Surface/Qualcomm) | [standalone-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/raw/releases/v2.5.0/releases/SystemRestoreTool-standalone-win-arm64.zip) (~33 MB) | [compact-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/latest) (~6 MB) |

**Standalone-версия** (рекомендуется) — включает .NET 6 Runtime, ничего не требует.

**Compact-версия** — требует установки [.NET Desktop Runtime 6.0+](https://dotnet.microsoft.com/download/dotnet/6.0).

### Вариант 2: GitHub Releases

Все бинарники также на странице [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) — там же SHA256 для проверки целостности.

### Вариант 3: Сборка из исходников

См. раздел [Сборка из исходников](#-сборка-из-исходников).

### Требования

- **ОС**: Windows 10 (build 19041+, May 2020 Update) или Windows 11
- **Архитектура**: x64 / x86 / ARM64
- **Права**: Администратор (манифест уже это указывает)
- **Для compact-сборки**: .NET Desktop Runtime 6.0+

### Проверка целостности

После скачивания проверьте SHA256:
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```
Сравните с [SHA256SUMS.txt](https://github.com/Ludonist/Windows-System-Recovery-Tool/raw/releases/v2.5.0/releases/SHA256SUMS.txt).

---

## 🚀 Использование

### Интерактивный режим

Запустите `SystemRestoreTool.exe` от имени администратора. Откроется меню из 66 пунктов:

```
╔══════════════════════════════════════════════════════════════════════╗
║                                                                      ║
║   SYSTEM RESTORE TOOL  v2.5   —   Прямое использование Windows API   ║
║                                                                      ║
║   Восстановление и проверка ВСЕХ системных файлов Windows 10/11      ║
║   ...                                                                ║
╚══════════════════════════════════════════════════════════════════════╝
[i] Права администратора: ПОДТВЕРЖДЕНЫ
[i] Лог запуска: C:\Users\...\AppData\Local\SystemRestoreTool\srt_20260628_193015.log

ГЛАВНОЕ МЕНЮ — System Restore Tool v2.5

  [ 1] ★ СУПЕР-ПОЛНЫЙ ЦИКЛ: точка восст. + DISM + SFC + службы + WU + кэши
  [ 2]   ПОЛНЫЙ ЦИКЛ (нативный API)
  [ 3]   ПОЛНЫЙ ЦИКЛ (Microsoft.Dism NuGet)
  ...
  [66]   Выход

Выберите действие: _
```

### Авто-режимы (для скриптов)

```cmd
:: СУПЕР-полный цикл: точка восстановления + DISM + SFC + службы + WU + кэши
SystemRestoreTool.exe --super-full

:: То же, плюс ResetBase WinSxS
SystemRestoreTool.exe --super-full --reset-base

:: Полный DISM + SFC цикл
SystemRestoreTool.exe --full

:: Проверка 40+ критических файлов
SystemRestoreTool.exe --verify

:: Глубокая проверка System32 + drivers
SystemRestoreTool.exe --scan-all
```

### Логирование

Все операции пишутся одновременно в консоль (с цветом) и в файл:
```
%LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log
```

### 🌐 Многоязычность

Программа поддерживает **3 языка интерфейса**:

| Код | Название | English name |
|-----|----------|--------------|
| `ru` | Русский | Russian |
| `en` | English | English |
| `zh` | 简体中文 | Chinese (Simplified) |

**Смена языка:**
1. В главном меню выберите пункт **64** («🌐 Сменить язык интерфейса»)
2. Выберите нужный язык (1-3)
3. Выбор **сохраняется в реестре** (`HKCU\SOFTWARE\SystemRestoreTool\Language`) и применяется при следующем запуске

Файлы переводов: `Resources/strings.{ru,en,zh}.json`. Они встроены в EXE как embedded resources, поэтому ничего дополнительно копировать не нужно.

**Чтобы добавить новый язык:**
1. Скопируйте `Resources/strings.en.json` в `Resources/strings.xx.json` (где `xx` — код языка)
2. Переведите все значения
3. Добавьте код в `Localizer.SupportedLanguages` и `LanguageNames`
4. Добавьте запись в `.csproj` как `<EmbeddedResource>`

---

## 🏗 Архитектура

```
Windows-System-Recovery-Tool/
├── SystemRestoreTool.csproj      .NET 6.0, x64/x86/ARM64, Microsoft.Dism NuGet
├── app.manifest                  requireAdministrator
├── Program.cs                    Точка входа, меню на 67 пунктов
│
├── Api/                          P/Invoke слой (нативные DLL Windows)
│   ├── DismNativeApi.cs          dismapi.dll (DISM API)
│   ├── SfcNativeApi.cs           sfc.dll + sfc_os.dll + srclient.dll
│   ├── WinTrustNativeApi.cs      wintrust.dll + crypt32.dll (подписи)
│   ├── Kernel32NativeApi.cs      kernel32.dll + advapi32.dll
│   ├── SrclientNativeApi.cs      srclient.dll (точки восстановления)
│   ├── ServicesNativeApi.cs      advapi32.dll (Service Control Manager)
│   ├── EventLogNativeApi.cs      advapi32.dll + wevtapi.dll
│   ├── VssNativeApi.cs           vssapi.dll (Volume Shadow Copy)
│   ├── WerNativeApi.cs           wer.dll (Windows Error Reporting)
│   ├── SetupApiNativeApi.cs      setupapi.dll (устройства)
│   ├── PowerNativeApi.cs         powrprof.dll (питание)
│   └── NetNativeApi.cs           winhttp.dll + wininet.dll + ws2_32.dll
│
├── Engine/                       Высокоуровневая логика
│   ├── SystemRestoreEngine.cs    Оркестрация циклов восстановления
│   ├── DismManagedWrapper.cs     Через Microsoft.Dism NuGet
│   ├── SignatureVerifier.cs      Высокоуровневая проверка подписей
│   ├── IntegrityChecker.cs       Проверка 40+ критических + всех System32
│   └── Modules/                  Специализированные модули (14 шт.)
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
├── Resources/                    🌐 Локализация (встроены как embedded resources)
│   ├── strings.ru.json           Русский (по умолчанию)
│   ├── strings.en.json           English
│   └── strings.zh.json           简体中文
│
├── Utils/
│   ├── ConsoleHelper.cs          UI-помощник (меню, права, ввод)
│   ├── Logger.cs                 Логгер в файл + цветная консоль
│   └── Localizer.cs              Загрузка и переключение языков
│
├── docs/
│   ├── api-reference.md          Подробный справочник по Win32 API
│   └── screenshots/              Скриншоты программы (PNG)
│       ├── 01-main-menu-ru.png
│       ├── 02-main-menu-en.png
│       ├── 03-main-menu-zh.png
│       ├── 04-integrity-check.png
│       └── 05-language-switch.png
│
├── .github/
│   ├── repo-metadata.json        Topics и описание репозитория
│   └── workflows/
│       └── build-release.yml     CI: сборка x86/x64/ARM64 + Release
│
├── .gitignore                    Стандартный .NET gitignore
├── .editorconfig                 Стиль кода
├── LICENSE                       MIT
├── CHANGELOG.md                  История изменений
├── CONTRIBUTING.md               Правила для контрибьюторов
├── SECURITY.md                   Политика безопасности
└── README.md                     Этот файл
```

### Статистика кода

- **~7200 строк C#** кода
- **37 файлов** в 6 директориях
- **17 нативных DLL** через P/Invoke
- **14 модулей** восстановления
- **67 пунктов** меню
- **3 языка** интерфейса (RU/EN/ZH)

---

## 🔌 Используемые Win32 API

Все операции выполняются через **прямые P/Invoke вызовы** (без shell-команд):

| DLL | Назначение |
|-----|-----------|
| `dismapi.dll` | DISM: CheckHealth / ScanHealth / RestoreHealth / StartComponentCleanup |
| `sfc.dll` | SfcIsFileProtected / SfcGetNextProtectedFile |
| `sfc_os.dll` | SfcSynchronousScan (= Sfc /ScanNow) |
| `wintrust.dll` | WinVerifyTrust (Authenticode-подписи) |
| `crypt32.dll` | CryptQueryObject / CertGetNameString (издатель) |
| `srclient.dll` | SRSetRestorePoint (точки восстановления) |
| `advapi32.dll` | SCM (службы), реестр, привилегии |
| `kernel32.dll` | Файлы, перезагрузка, App Recovery/Restart |
| `wevtapi.dll` | Журналы событий (EvtClearLog) |
| `vssapi.dll` | Volume Shadow Copy |
| `wer.dll` | Windows Error Reporting |
| `setupapi.dll` | Установщик устройств |
| `cfgmgr32.dll` | CM_Reenumerate_DevNode |
| `powrprof.dll` | Схемы питания, батарея |
| `winhttp.dll` | HTTP-проверки серверов |
| `wininet.dll` | InternetGetConnectedState |
| `ws2_32.dll` | Winsock (WSAStartup) |
| **Microsoft.Dism NuGet** | Управляемая обёртка DISM API |
| **COM WUA API** | Windows Update Agent (`Microsoft.Update.Session`) |

Подробности — в [docs/api-reference.md](docs/api-reference.md).

---

## 🛠 Сборка из исходников

### Требования

- **Windows 10** (build 19041+) или **Windows 11**
- **.NET 6.0 SDK** или новее — https://dotnet.microsoft.com/download
- (Опционально) Visual Studio 2022 / JetBrains Rider / VS Code

### Шаги

```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Результат: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Публикация single-file

```bash
# Compact (требует .NET 6 на целевой машине, ~27 MB)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Self-contained (ничего не требует, ~45 MB)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### Поддерживаемые архитектуры

```bash
# x64 (Intel/AMD 64-bit) — основная
dotnet publish -c Release -r win-x64 ...

# x86 (32-bit) — для старых систем
dotnet publish -c Release -r win-x86 ...

# ARM64 (Surface Pro X, Snapdragon laptops)
dotnet publish -c Release -r win-arm64 ...
```

---

## 📋 Список проверяемых файлов

### 40+ критических файлов (`IntegrityChecker.CriticalFiles`)

- **Базовые DLL**: kernel32.dll, ntdll.dll, user32.dll, advapi32.dll, shell32.dll, ole32.dll, gdi32.dll, msvcrt.dll, ws2_32.dll, wininet.dll, urlmon.dll, combase.dll, sechost.dll, rpcrt4.dll, setupapi.dll
- **Загрузчики и процессы**: winload.exe, winresume.exe, winlogon.exe, csrss.exe, services.exe, lsass.exe, lsaiso.exe, smss.exe, svchost.exe, spoolsv.exe, wininit.exe, dwm.exe
- **Криптография**: bcrypt.dll, ncrypt.dll, schannel.dll, crypt32.dll, cryptbase.dll, cryptsp.dll, wintrust.dll, msasn1.dll
- **DISM/SFC**: sfc.dll, sfc_os.dll, dismapi.dll, dism.exe, sfc.exe
- **Управление**: mmc.exe, powershell.exe, cmd.exe, regedit.exe, taskmgr.exe, eventvwr.exe
- **Shell**: explorer.exe, shdocvw.dll, shellstyle.dll, themecpl.dll, themeui.dll
- **Драйверы ядра**: tcpip.sys, ntfs.sys, volmgr.sys, volmgrx.sys, disk.sys, partmgr.sys, ACPI.sys, hal.dll, ndis.sys, http.sys, Wdf01000.sys, ksecdd.sys, ksecpkg.sys, msisadrv.sys, pci.sys, mountmgr.sys, fltMgr.sys, luafv.sys, mrxsmb.sys, mup.sys
- **.NET Framework**: clr.dll, mscorlib.dll, System.dll
- **WinRT**: Windows.Foundation.winmd, WinRTTraceLogger.dll
- **WSL**: lxss.dll, wslapi.dll

### Глубокая проверка (System32 + drivers)

- Все `.dll`, `.exe`, `.sys`, `.cpl` в `C:\Windows\System32`
- Все `.sys` в `C:\Windows\System32\drivers`
- Драйверы UMDF: `C:\Windows\System32\drivers\UMDF`
- Драйверы Windows Defender: `C:\Windows\System32\drivers\wd`
- Все `.exe`, `.dll` в `C:\Windows`

### Полная проверка

Дополнительно: `C:\Windows\SysWOW64` (32-битные версии системных библиотек)

---

## 📊 Пример отчёта

```
[i] Проверено файлов:           40
[i] Под защитой WFP:            38
[i] Без защиты WFP:             2
[+] Валидная подпись Microsoft: 37
[!] Подписаны третьей стороной: 0
[!] Без подписи:                0
[X] С плохой подписью:          0
[X] Отсутствуют:                1
[i] Время проверки:             18.5 сек

Все критические файлы в порядке — подмен не обнаружено.
```

---

## 🔒 Безопасность

- ✅ Программа **не запускает** `cmd.exe`, `dism.exe`, `sfc.exe`, `powershell.exe` (за исключением `bcdedit.exe` и `netsh.exe` для операций без P/Invoke аналогов)
- ✅ Все операции — через нативные Win32 API
- ✅ Требуются права администратора (манифест `requireAdministrator`)
- ✅ Логи пишутся **локально** в `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Никакие данные не передаются по сети
- ✅ Open-source под лицензией MIT — вы можете аудировать код

## ⚠️ Ограничения

1. **Только Windows 10/11** — для Windows 7/8 нужно менять `TargetFramework`
2. **RestoreHealth** может потребовать доступ к Windows Update или установочному образу
3. **SfcSynchronousScan** недокументирован — на некоторых сборках может потребовать доп. флагов
4. **ResetBase** необратим — после него нельзя удалить установленные обновления
5. **bcdedit / bootrec** не имеют P/Invoke аналогов — вызываются через `Process.Start` напрямую (без cmd.exe)
6. **RegSaveKey** для живых кустов может не сработать (файлы заняты системой) — в этом случае используется прямое копирование файлов

---

## 📈 Roadmap

- [ ] GUI-версия (WPF) с графикой
- [ ] Локализация интерфейса (English / 中文 / Deutsch)
- [ ] Автоматическое распознавание "битых" пакетов через CBS.log
- [ ] Поддержка Windows Server 2022/2025 как отдельных профилей
- [ ] Планировщик задач (например, еженедельная проверка целостности)
- [ ] Экспорт отчётов в HTML/PDF

См. [open issues](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) для полного списка.

---

## 🤝 Contributing

Приветствуются pull request'ы! См. [CONTRIBUTING.md](CONTRIBUTING.md) для правил.

Особенно нужны:
- 🌍 Перевод интерфейса на другие языки
- 🐛 Баг-репорты с реальными краш-логами
- 📚 Документация по edge-cases на разных сборках Windows
- 🧪 Тесты на ARM64 устройствах (Surface Pro X, etc.)

---

## 📄 Лицензия

MIT License — см. [LICENSE](LICENSE).

Вы можете свободно использовать, модифицировать и распространять этот код.

---

## 🙏 Признательность

- [Microsoft.Dism NuGet](https://github.com/jeffkl/ManagedDism) — управляемая обёртка DISM API от Jeff Kluge
- [Microsoft Learn Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list) — документация по Windows API
- [pinvoke.net](https://www.pinvoke.net/) — справочник P/Invoke сигнатур

---

## ⭐ Star History

Если программа оказалась полезной — поставьте ⭐ репозиторию!

<div align="center">

**[⬆ Наверх](#-windows-system-recovery-tool)**

Made with ❤️ for Windows power users

</div>
