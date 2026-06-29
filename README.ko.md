# 🔧 WINDOWS 시스템 복구 도구

<div align="center">

🌐 **사용 가능한 언어 / Available languages / 可用语言 / Unterstützte Sprachen / Langues disponibles / Idiomas disponibles / 利用可能な言語 / 사용 가능한 언어 / Idiomas disponíveis:**

[🇷🇺 Русский](README.md) · [🇬🇧 English](README.en.md) · [🇨🇳 简体中文](README.zh.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇵🇹 Português](README.pt.md)

🌐 **언어 자동 감지 문서**: https://ludonist.github.io/Windows-System-Recovery-Tool/

---

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64%20%7C%20ARM64-orange)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Version](https://img.shields.io/badge/version-2.6.0-brightgreen)
![Language](https://img.shields.io/badge/lang-C%23%2010-success)
![Languages](https://img.shields.io/badge/UI%20languages-RU%20%7C%20EN%20%7C%20中文-red)

**Win32 API 직접 호출을 통한 Windows 10/11 시스템 파일 복구 전문 도구**

🌐 **다국어 UI**: Русский · English · 简体中文 · Deutsch · Français · Español · 日本語 · 한국어 · Português

[기능](#-features) ·
[스크린샷](#-screenshots) ·
[설치](#-installation) ·
[사용법](#-usage) ·
[아키텍처](#-architecture) ·
[API](#-win32-apis-used) ·
[빌드](#-building-from-source) ·
[기여](#-contributing)

</div>

---

## 📖 소개

**WINDOWS 시스템 복구 도구**는 Windows 10/11용 콘솔 응용 프로그램으로, 외부 명령어 `cmd.exe` / `dism.exe` / `sfc.exe`를 사용하지 않고 **직접 Windows API 호출**(`dismapi.dll`, `sfc.dll`, `wintrust.dll` 등)을 통해 시스템 파일을 복구하고 무결성을 검증합니다.

### 주요 기능

- 🎯 **67개의 작업**을 하나의 메뉴에서 복구 및 진단
- 🔌 P/Invoke를 통한 **17개의 네이티브 Windows API** (셸 명령어 없음)
- 🛡️ **파일 변조 감지**: WinVerifyTrust + 인증서 게시자
- 📊 시간에 따른 시스템 상태 비교를 위한 **SHA256/SHA1 스냅샷**
- 🧩 대체 관리형 구현으로 **Microsoft.Dism NuGet** 사용
- 📋 `%LOCALAPPDATA%\SystemRestoreTool\`에 **상세 로깅**
- ⚡ **자체 포함 빌드** — .NET 설치 불필요
- 🌐 **다국어 UI**: Русский / English / 简体中文 / Deutsch / Français / Español / 日本語 / 한국어 / Português

### 이 도구가 해결하는 문제

| 증상 | 해결책 |
|---------|----------|
| `sfc /scannow`가 손상 발견 | DISM RestoreHealth + SFC /ScanNow |
| 바이러스성 DLL 변조 의심 | 모든 중요 파일에 WinVerifyTrust 실행 |
| Windows Update 고장 | SoftwareDistribution 재설정 + DLL 재등록 |
| 서비스가 시작되지 않음 | SCM을 통해 40개 이상의 중요 서비스 재시작 |
| 부트로더 실패 | BCD 확인 및 복구 |
| 복원 지점 없음 | `SRSetRestorePoint`로 생성 |
| 아이콘/글꼴 캐시 손상 | IconCache.db / FNTCACHE.DAT 재구축 |
| 시스템 무결성 우려 | SHA256 스냅샷 + 베이스라인과 비교 |

---

## ✨ 기능

### 1. DISM API를 통한 복구 (`dismapi.dll`)

| 작업 | 콘솔 동등 명령어 |
|----------|---------------------|
| CheckHealth | `Dism /Online /Cleanup-Image /CheckHealth` |
| ScanHealth | `Dism /Online /Cleanup-Image /ScanHealth` |
| RestoreHealth | `Dism /Online /Cleanup-Image /RestoreHealth` |
| StartComponentCleanup | `Dism /Online /Cleanup-Image /StartComponentCleanup` |
| StartComponentCleanup + ResetBase | `... /StartComponentCleanup /ResetBase` |

### 2. 네이티브 `sfc_os.dll`을 통한 SFC

| 작업 | 동등 명령어 |
|----------|------------|
| SfcSynchronousScan (ScanAndRepair) | `Sfc /ScanNow` |
| SfcSynchronousScan (VerifyOnly) | `Sfc /VerifyOnly` |
| SfcIsFileProtected | (동등 명령어 없음) |
| SfcGetNextProtectedFile | (동등 명령어 없음) |

### 3. 무결성 검사

- **40개 이상의 중요 파일** — kernel32.dll, ntdll.dll, winlogon.exe, lsass.exe, csrss.exe, smss.exe, 드라이버(tcpip.sys, ntfs.sys, volmgr.sys, ACPI.sys, hal.dll, ...) 등
- **System32 + 드라이버 심층 검사** — 모든 .dll/.exe/.sys (약 5000개 파일)
- **System32 + SysWOW64 전체 검사** — 약 10000개 파일
- 각 파일에 대해: WFP 보호, Authenticode 서명, 게시자(Microsoft vs 타사)

### 4. 복구 모듈 (14개 모듈)

| 모듈 | 목적 | API |
|--------|---------|-----|
| `SystemRestorePointManager` | 복원 지점 | `srclient.dll!SRSetRestorePoint` |
| `ServicesRepairManager` | 40개 이상의 서비스 재시작 | `advapi32.dll` SCM |
| `BootRecoveryManager` | BCD, bootmgr, winload | WMI + bcdedit |
| `RegistryRestoreManager` | 레지스트리 하이브 백업 | `advapi32.dll!RegSaveKey` |
| `WindowsUpdateRepairManager` | WU 재설정 | SCM + File API |
| `WinSxsRepairManager` | WinSxS 정리 | `dismapi.dll!DismAnalyzeComponentStore` |
| `EventLogManager` | 이벤트 로그 | `wevtapi.dll!EvtClearLog` |
| `UserEnvRestoreManager` | 프로필, 캐시 | advapi32 + File API |
| `FileHashDatabaseManager` | SHA256 스냅샷 | `System.Security.Cryptography` |
| `DeviceManager` | 장치 | `setupapi.dll` |
| `PowerOptionsManager` | 전원 구성표 | `powrprof.dll` |
| `NetworkDiagnosticManager` | 네트워크 | `winhttp.dll` + `ws2_32.dll` |
| `WerManager` | WER 보고서 | `wer.dll` |
| `WindowsUpdateAgentManager` | 업데이트 검색 | COM WUA API |

---

## 📸 스크린샷

### 메인 메뉴 (러시아어)

![Main Menu RU](docs/screenshots/01-main-menu-ru.png)

### 메인 메뉴 (영어)

![Main Menu EN](docs/screenshots/02-main-menu-en.png)

### 主菜单 (简体中文)

![Main Menu ZH](docs/screenshots/03-main-menu-zh.png)

### 시스템 파일 무결성 검사

![Integrity Check](docs/screenshots/04-integrity-check.png)

### 인터페이스 언어 전환

![Language Switch](docs/screenshots/05-language-switch.png)

---

## 📥 설치

### 옵션 1: 빌드된 EXE 다운로드 (권장)

**직접 다운로드 링크** (최신 빌드, 의존성 없음):

| 아키텍처 | 독립 실행형 (.NET 포함) | 컴팩트 (.NET 6 필요) |
|--------------|----------------------------:|--------------------------:|
| **x64** (Intel/AMD) | [standalone-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64.zip) (~40 MB) | [compact-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64-compact.zip) (~7 MB) |
| **x86** (32-bit) | [standalone-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86.zip) (~37 MB) | [compact-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86-compact.zip) (~7 MB) |
| **ARM64** (Surface/Qualcomm) | [standalone-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64.zip) (~33 MB) | [compact-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64-compact.zip) (~6 MB) |

**독립 실행형 버전** (권장) — .NET 6 Runtime을 포함하며, 다른 것은 필요 없습니다.

**컴팩트 버전** — [.NET Desktop Runtime 6.0+](https://dotnet.microsoft.com/download/dotnet/6.0)이 필요합니다.

### 옵션 2: GitHub Releases

모든 바이너리는 [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) 페이지에서도 사용 가능하며, 무결성 검증을 위한 SHA256 체크섬도 함께 제공됩니다.

### 옵션 3: 소스에서 빌드

[소스에서 빌드](#-building-from-source)를 참조하세요.

### 요구 사항

- **운영체제**: Windows 10 (빌드 19041+, May 2020 Update) 또는 Windows 11
- **아키텍처**: x64 / x86 / ARM64
- **권한**: 관리자 (매니페스트에 이미 지정됨)
- **컴팩트 빌드의 경우**: .NET Desktop Runtime 6.0+

### 무결성 검증

다운로드 후 SHA256 검증:
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```
[Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) 페이지의 각 아카이브 옆에 있는 `.sha256` 파일과 비교하세요.

---

## 🚀 사용법

### 대화형 모드

`SystemRestoreTool.exe`를 관리자로 실행하면 67개 항목의 메뉴가 열립니다:

```
╔══════════════════════════════════════════════════════════════════════╗
║                                                                      ║
║   WINDOWS SYSTEM RECOVERY TOOL  v2.5.1                               ║
║   Direct recovery of Windows 10/11 system files                      ║
║   ...                                                                ║
╚══════════════════════════════════════════════════════════════════════╝
[+] Administrator privileges: CONFIRMED
[i] Log file: C:\Users\...\AppData\Local\SystemRestoreTool\srt_20260628_193015.log

MAIN MENU — System Restore Tool v2.5

  [ 1] ★ SUPER-FULL CYCLE: restore point + DISM + SFC + services + WU + caches
  [ 2]   FULL CYCLE (native API)
  [ 3]   FULL CYCLE (Microsoft.Dism NuGet)
  ...
  [67]   Exit

Select action: _
```

### 자동 모드 (스크립트용)

```cmd
:: Super-full cycle: restore point + DISM + SFC + services + WU + caches
SystemRestoreTool.exe --super-full

:: Same + ResetBase WinSxS
SystemRestoreTool.exe --super-full --reset-base

:: Full DISM + SFC cycle
SystemRestoreTool.exe --full

:: Check 40+ critical files
SystemRestoreTool.exe --verify

:: Deep check System32 + drivers
SystemRestoreTool.exe --scan-all
```

### 로깅

모든 작업은 콘솔(컬러 포함)과 파일에 동시에 기록됩니다:
```
%LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log
```

### 🌐 다국어 UI

이 프로그램은 **3가지 인터페이스 언어**를 지원합니다:

| 코드 | 원어 명칭 | 영어 명칭 |
|------|-------------|--------------|
| `ru` | Русский | Russian |
| `en` | English | English |
| `zh` | 简体中文 | Chinese (Simplified) |

**언어 전환 방법:**
1. 메인 메뉴에서 항목 **64**("🌐 Switch interface language")를 선택
2. 원하는 언어 선택 (1-3)
3. 선택은 **레지스트리에 저장**되며(`HKCU\SOFTWARE\SystemRestoreTool\Language`), 다음 실행 시 적용됩니다

번역 파일: `Resources/strings.{ru,en,zh}.json`. 이들은 포함 리소스로 EXE에 내장되어 있어, 다른 것을 복사할 필요가 없습니다.

**새 언어 추가:**
1. `Resources/strings.en.json`을 `Resources/strings.xx.json`으로 복사 (`xx` = 언어 코드)
2. 모든 값을 번역
3. `Localizer.SupportedLanguages` 및 `LanguageNames`에 코드 추가
4. `.csproj`에 `<EmbeddedResource>`로 항목 추가

---

## 🏗 아키텍처

```
Windows-System-Recovery-Tool/
├── SystemRestoreTool.csproj      .NET 6.0, x64/x86/ARM64, Microsoft.Dism NuGet
├── app.manifest                  requireAdministrator
├── Program.cs                    진입점, 67개 항목 메뉴
│
├── Api/                          P/Invoke 계층 (네이티브 Windows DLL)
│   ├── DismNativeApi.cs          dismapi.dll (DISM API)
│   ├── SfcNativeApi.cs           sfc.dll + sfc_os.dll + srclient.dll
│   ├── WinTrustNativeApi.cs      wintrust.dll + crypt32.dll (서명)
│   ├── Kernel32NativeApi.cs      kernel32.dll + advapi32.dll
│   ├── SrclientNativeApi.cs      srclient.dll (복원 지점)
│   ├── ServicesNativeApi.cs      advapi32.dll (Service Control Manager)
│   ├── EventLogNativeApi.cs      advapi32.dll + wevtapi.dll
│   ├── VssNativeApi.cs           vssapi.dll (Volume Shadow Copy)
│   ├── WerNativeApi.cs           wer.dll (Windows Error Reporting)
│   ├── SetupApiNativeApi.cs      setupapi.dll (장치)
│   ├── PowerNativeApi.cs         powrprof.dll (전원)
│   └── NetNativeApi.cs           winhttp.dll + wininet.dll + ws2_32.dll
│
├── Engine/                       고수준 로직
│   ├── SystemRestoreEngine.cs    복구 주기 오케스트레이션
│   ├── DismManagedWrapper.cs     Microsoft.Dism NuGet 경유
│   ├── SignatureVerifier.cs      고수준 서명 검증
│   ├── IntegrityChecker.cs       40개 이상의 중요 파일 + 전체 System32 검사
│   └── Modules/                  특화 모듈 (14)
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
├── Resources/                    🌐 현지화 (포함 리소스)
│   ├── strings.ru.json           Русский (기본값)
│   ├── strings.en.json           English
│   └── strings.zh.json           简体中文
│
├── Utils/
│   ├── ConsoleHelper.cs          UI 헬퍼 (메뉴, 권한, 입력)
│   ├── Logger.cs                 파일 + 컬러 콘솔 로거
│   └── Localizer.cs              언어 로딩 및 전환
│
├── docs/
│   ├── api-reference.md          상세 Win32 API 참조
│   └── screenshots/              프로그램 스크린샷 (PNG)
│
├── .github/
│   ├── repo-metadata.json        Topics 및 저장소 설명
│   └── workflows/
│       └── build-release.yml     CI: x86/x64/ARM64 빌드 + Release
│
├── .gitignore                    표준 .NET gitignore
├── .editorconfig                 코드 스타일
├── LICENSE                       MIT
├── CHANGELOG.md                  변경 이력
├── CONTRIBUTING.md               기여자 규칙
├── SECURITY.md                   보안 정책
├── README.md                     러시아어 문서 (기본값)
├── README.en.md                  영어 문서
└── README.zh.md                  중국어 문서
```

### 코드 통계

- **~7200줄의 C#** 코드
- **6개 디렉토리에 37개 파일**
- **P/Invoke를 통한 17개 네이티브 DLL**
- **14개 복구 모듈**
- **67개 메뉴 항목**
- **3가지 인터페이스 언어** (RU/EN/ZH)

---

## 🔌 사용된 Win32 API

모든 작업은 **직접 P/Invoke 호출**을 사용합니다 (셸 명령어 없음):

| DLL | 목적 |
|-----|-----------|
| `dismapi.dll` | DISM: CheckHealth / ScanHealth / RestoreHealth / StartComponentCleanup |
| `sfc.dll` | SfcIsFileProtected / SfcGetNextProtectedFile |
| `sfc_os.dll` | SfcSynchronousScan (= Sfc /ScanNow) |
| `wintrust.dll` | WinVerifyTrust (Authenticode 서명) |
| `crypt32.dll` | CryptQueryObject / CertGetNameString (게시자) |
| `srclient.dll` | SRSetRestorePoint (복원 지점) |
| `advapi32.dll` | SCM (서비스), 레지스트리, 권한 |
| `kernel32.dll` | 파일, 재부팅, App Recovery/Restart |
| `wevtapi.dll` | 이벤트 로그 (EvtClearLog) |
| `vssapi.dll` | Volume Shadow Copy |
| `wer.dll` | Windows Error Reporting |
| `setupapi.dll` | 장치 설치 관리자 |
| `cfgmgr32.dll` | CM_Reenumerate_DevNode |
| `powrprof.dll` | 전원 구성표, 배터리 |
| `winhttp.dll` | HTTP 서버 검사 |
| `wininet.dll` | InternetGetConnectedState |
| `ws2_32.dll` | Winsock (WSAStartup) |
| **Microsoft.Dism NuGet** | 관리형 DISM API 래퍼 |
| **COM WUA API** | Windows Update Agent (`Microsoft.Update.Session`) |

자세한 내용은 [docs/api-reference.md](docs/api-reference.md)를 참조하세요.

---

## 🛠 소스에서 빌드

### 요구 사항

- **Windows 10** (빌드 19041+) 또는 **Windows 11**
- **.NET 6.0 SDK** 이상 — https://dotnet.microsoft.com/download
- (선택) Visual Studio 2022 / JetBrains Rider / VS Code

### 단계

```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

결과: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 단일 파일 게시

```bash
# 컴팩트 (대상 머신에 .NET 6 필요, ~27 MB)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 자체 포함 (의존성 없음, ~45 MB)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### 지원 아키텍처

```bash
# x64 (Intel/AMD 64-bit) — 주력
dotnet publish -c Release -r win-x64 ...

# x86 (32-bit) — 구형 시스템용
dotnet publish -c Release -r win-x86 ...

# ARM64 (Surface Pro X, Snapdragon 랩탑)
dotnet publish -c Release -r win-arm64 -p:PublishReadyToRun=false ...
```

---

## 📋 검사된 파일 목록

### 40개 이상의 중요 파일 (`IntegrityChecker.CriticalFiles`)

- **기본 DLL**: kernel32.dll, ntdll.dll, user32.dll, advapi32.dll, shell32.dll, ole32.dll, gdi32.dll, msvcrt.dll, ws2_32.dll, wininet.dll, urlmon.dll, combase.dll, sechost.dll, rpcrt4.dll, setupapi.dll
- **로더 및 프로세스**: winload.exe, winresume.exe, winlogon.exe, csrss.exe, services.exe, lsass.exe, lsaiso.exe, smss.exe, svchost.exe, spoolsv.exe, wininit.exe, dwm.exe
- **암호화**: bcrypt.dll, ncrypt.dll, schannel.dll, crypt32.dll, cryptbase.dll, cryptsp.dll, wintrust.dll, msasn1.dll
- **DISM/SFC**: sfc.dll, sfc_os.dll, dismapi.dll, dism.exe, sfc.exe
- **관리**: mmc.exe, powershell.exe, cmd.exe, regedit.exe, taskmgr.exe, eventvwr.exe
- **셸**: explorer.exe, shdocvw.dll, shellstyle.dll, themecpl.dll, themeui.dll
- **커널 드라이버**: tcpip.sys, ntfs.sys, volmgr.sys, volmgrx.sys, disk.sys, partmgr.sys, ACPI.sys, hal.dll, ndis.sys, http.sys, Wdf01000.sys, ksecdd.sys, ksecpkg.sys, msisadrv.sys, pci.sys, mountmgr.sys, fltMgr.sys, luafv.sys, mrxsmb.sys, mup.sys
- **.NET Framework**: clr.dll, mscorlib.dll, System.dll
- **WinRT**: Windows.Foundation.winmd, WinRTTraceLogger.dll
- **WSL**: lxss.dll, wslapi.dll

### 심층 검사 (System32 + 드라이버)

- `C:\Windows\System32`의 모든 `.dll`, `.exe`, `.sys`, `.cpl`
- `C:\Windows\System32\drivers`의 모든 `.sys`
- UMDF 드라이버: `C:\Windows\System32\drivers\UMDF`
- Windows Defender 드라이버: `C:\Windows\System32\drivers\wd`
- `C:\Windows`의 모든 `.exe`, `.dll`

### 전체 검사

추가: `C:\Windows\SysWOW64` (시스템 라이브러리의 32-bit 버전)

---

## 📊 샘플 보고서

```
[i] Files checked:                40
[i] WFP-protected:                38
[i] Without WFP protection:       2
[+] Valid Microsoft signature:    37
[!] Signed by third party:        0
[!] Without signature:            0
[X] With bad signature:           0
[X] Missing:                      1
[i] Check duration:               18.5 sec

All critical files are intact — no tampering detected.
```

---

## 🔒 보안

- ✅ 이 프로그램은 `cmd.exe`, `dism.exe`, `sfc.exe`, `powershell.exe`를 **실행하지 않습니다** (P/Invoke 동등 기능이 없는 작업의 경우 `bcdedit.exe` 및 `netsh.exe` 제외)
- ✅ 모든 작업은 네이티브 Win32 API를 사용
- ✅ 관리자 권한 필요 (매니페스트 `requireAdministrator`)
- ✅ 로그는 `%LOCALAPPDATA%\SystemRestoreTool\`에 **로컬로** 기록됩니다
- ✅ 네트워크를 통해 데이터를 전송하지 않습니다
- ✅ MIT 라이선스의 오픈소스 — 코드를 감사할 수 있습니다

## ⚠️ 제한 사항

1. **Windows 10/11 전용** — Windows 7/8의 경우 `TargetFramework`를 변경해야 합니다
2. **RestoreHealth**는 Windows Update 또는 설치 미디어에 대한 접근이 필요할 수 있습니다
3. **SfcSynchronousScan**은 문서화되지 않았습니다 — 일부 빌드에서는 추가 플래그가 필요할 수 있습니다
4. **ResetBase**는 되돌릴 수 없습니다 — 적용 후 설치된 업데이트를 제거할 수 없습니다
5. **bcdedit / bootrec**는 P/Invoke 동등 기능이 없습니다 — `Process.Start`로 직접 호출됩니다 (cmd.exe 없음)
6. **RegSaveKey**는 활성 하이브에 대해 실패할 수 있습니다 (시스템이 파일을 잠급니다) — 이 경우 직접 파일 복사가 사용됩니다

---

## 📈 로드맵

- [ ] 그래픽이 포함된 GUI 버전 (WPF)
- [ ] 인터페이스 현지화 (Deutsch / Français / Español / 日本語)
- [ ] CBS.log를 통한 "손상된" 패키지 자동 감지
- [ ] 별도 프로필로 Windows Server 2022/2025 지원
- [ ] 작업 스케줄러 (예: 주간 무결성 검사)
- [ ] 보고서를 HTML/PDF로 내보내기

전체 목록은 [열린 이슈](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues)를 참조하세요.

---

## 🤝 기여

풀 리퀘스트를 환영합니다! 규칙은 [CONTRIBUTING.md](CONTRIBUTING.md)를 참조하세요.

특히 필요한 사항:
- 🌍 인터페이스를 다른 언어로 번역
- 🐛 실제 충돌 로그가 포함된 버그 보고
- 📚 다양한 Windows 빌드의 엣지 케이스 문서화
- 🧪 ARM64 장치에서의 테스트 (Surface Pro X 등)

---

## 📄 라이선스

MIT 라이선스 — [LICENSE](LICENSE)를 참조하세요.

이 코드는 자유롭게 사용, 수정 및 배포할 수 있습니다.

---

## 🙏 감사의 말

- [Microsoft.Dism NuGet](https://github.com/jeffkl/ManagedDism) — Jeff Kluge의 관리형 DISM API 래퍼
- [Microsoft Learn Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list) — Windows API 문서
- [pinvoke.net](https://www.pinvoke.net/) — P/Invoke 시그니처 참조

---

## ⭐ 스타 히스토리

이 프로그램이 유용하다고 생각되면 — 저장소에 ⭐를 주세요!

<div align="center">

**[⬆ 맨 위로 돌아가기](#-windows-시스템-복구-도구)**

Windows 파워 유저를 위해 ❤️로 제작

</div>
