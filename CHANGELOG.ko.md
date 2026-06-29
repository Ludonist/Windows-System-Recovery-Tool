# 변경 내역 / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **참고:** 이 변경 내역은 한국어입니다. 다른 언어는 [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) 페이지를 참조하세요.

이 프로젝트의 모든 주목할 만한 변경 사항은 이 파일에 문서화됩니다.

형식은 [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/)를 기반으로 하며,
프로젝트는 [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html)을 따릅니다.

## [Unreleased]

### 계획 중
- 콘솔 대신 그래픽 기반의 GUI 버전(WPF)
- Windows Server 2022/2025를 별도의 프로필로 지원
- CBS.log를 통한 "손상된" 패키지 자동 감지
- 인터페이스 현지화(Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### 변경됨
- **모든 파일의 버전이 2.5.1로 업데이트됨**(README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **README의 직접 다운로드 링크**가 GitHub Releases v2.5.1을 가리키도록 변경(releases/v2.5.0 브랜치 대신)
- **GitHub Actions 워크플로** 수정:
  - ARM64에 대해 `PublishReadyToRun=false`(이전에는 crossgen2가 충돌함)
  - `fail-fast: false` — 하나의 아키텍처 실패가 다른 것들을 취소하지 않음
  - 릴리스 생성을 위한 명시적 `permissions: contents: write`
  - actions 버전 업데이트: checkout v5, setup-dotnet v5, action-gh-release v3

### 제거됨
- 4개의 임시 Dependabot 브랜치(정리)
- `github-actions` 생태계용 Dependabot(NuGet만 남김) — 이전에 존재하지 않는 버전의 PR을 생성했음

### 변경 없음(v2.5.0 기준)
- 17개의 네이티브 Win32 API
- 67개의 메뉴 작업
- 14개의 복구 모듈
- 3개의 인터페이스 언어(RU/EN/ZH)
- `docs/screenshots/`의 5개 스크린샷

## [2.5.0] — 2026-06-28

### 추가됨
- **🌐 다국어 인터페이스(3개 언어)**:
  - 러시아어(기본값)
  - English
  - 简体中文(중국어 간체)
  - 번역 파일: `Resources/strings.{ru,en,zh}.json`(임베디드 리소스로 내장)
  - 언어 관리를 위한 `Localizer` 클래스
  - 레지스트리에 선택 저장(`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - 메뉴 항목 64 "🌐 인터페이스 언어 변경"
  - 배너가 현재 언어를 표시

- **📸 프로그램 스크린샷** `docs/screenshots/`에 위치:
  - `01-main-menu-ru.png` — 메인 메뉴(러시아어)
  - `02-main-menu-en.png` — Main Menu(영어)
  - `03-main-menu-zh.png` — 主菜单(简体中文)
  - `04-integrity-check.png` — 무결성 검사 보고서
  - `05-language-switch.png` — 언어 전환

- **5개의 새로운 네이티브 Windows API**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — 장치 및 드라이버 관리
  - `powrprof.dll` — 전원 스키마, 배터리, 최대 절전 모드
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — 네트워크 진단
  - COM WUA API(`Microsoft.Update.Session`) — 업데이트 검색

- **6개의 새로운 모듈**:
  - `DeviceManager` — 장치 목록, 드라이버 서명 검증, 하드웨어 변경 사항 스캔
  - `PowerOptionsManager` — 전원 스키마 및 최대 절전 모드 관리
  - `NetworkDiagnosticManager` — WU 서버 확인, Winsock/TCP/IP/DNS/방화벽 재설정
  - `WerManager` — Windows 오류 보고서 통계 및 정리
  - `WindowsUpdateAgentManager` — COM API를 통한 업데이트 검색

- **메뉴가 44개에서 67개 항목으로 확장**(언어 전환 항목 추가)
- **자체 포함 빌드**(45 MB) — .NET 설치 불필요
- 모든 비트 수 지원: x86, x64, ARM64
- 단일 파일 EXE 압축(`EnableCompressionInSingleFile`)
- **`--help` / `--version`** 명령
- 테두리가 있는 친절한 오류 메시지
- 자동화된 릴리스 빌드를 위한 **GitHub Actions 워크플로**
- **`docs/api-reference.md`** — 상세한 Win32 API 참조
- **`SECURITY.md`** 및 **`.editorconfig`**

### 변경됨
- 프로그램 배너 업데이트: 사용 중인 17개 API + 현재 언어 표시
- 로거가 이제 타임스탬프와 함께 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`에 기록
- `SignatureVerifier`가 서명에서 Subject/Issuer를 올바르게 추출
- `Microsoft.Dism` NuGet 래퍼가 실제 3.2.0 API에 맞게 재작성
- `app.manifest`가 DPI 설정으로 업데이트

### 수정됨
- `RegSaveKeyW`/`RegRestoreKeyW`가 올바른 서명을 갖도록 수정
- `DismProgressCallback`이 `DismProgress`를 받도록 수정(세 개가 아닌 한 개의 매개변수)
- `BootRecoveryManager` — `se` 변수 사용 수정

## [2.0.0] — 2026-06-28

### 추가됨
- **아키텍처 전면 재설계** — 모듈식 구조
- **9개의 복구 모듈**:
  - `SystemRestorePointManager` — `srclient.dll`을 통한 복원 지점
  - `ServicesRepairManager` — 40개 이상의 중요 서비스 재시작
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — 레지스트리 하이브 백업
  - `WindowsUpdateRepairManager` — SoftwareDistribution/BITS/Catroot2 재설정
  - `WinSxsRepairManager` — 컴포넌트 저장소 분석 및 정리
  - `EventLogManager` — `wevtapi.dll`을 통한 로그 백업/정리
  - `UserEnvRestoreManager` — 사용자 프로필, 아이콘/글꼴 캐시
  - `FileHashDatabaseManager` — 비교용 SHA256/SHA1 스냅샷

- **확장된 무결성 검사**:
  - 40개 이상의 중요 파일(DLL, EXE, 드라이버)
  - System32 + drivers 심층 검사(약 5000개 파일)
  - System32 + SysWOW64 전체 검사(약 10000개 파일)

- **새로운 네이티브 API**:
  - `kernel32.dll` — 파일, 권한, 재부팅, 모듈
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — 이벤트 로그
  - `vssapi.dll` — Volume Shadow Copy

- **44개의 메뉴 항목**
- **자동 모드**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### 추가됨
- 프로그램의 초기 버전
- DISM API(`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- SFC API(`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- WinTrust API(`wintrust.dll`, `crypt32.dll`): Authenticode 서명 검증
- 대체 경로로서의 Microsoft.Dism NuGet 패키지
- 15개 항목의 기본 메뉴
- `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`에 로깅

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
