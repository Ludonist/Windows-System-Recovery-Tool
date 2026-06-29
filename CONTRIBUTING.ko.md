# 기여 / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# System Restore Tool에 기여하기 (한국어)

기여해 주셔서 감사합니다! 🎉

이 프로젝트는 직접 Win32 API 호출을 통해 Windows 10/11 시스템 파일을 복구하는 오픈 소스 도구입니다. 버그 보고, 기능, 번역, 문서화 등 모든 기여를 환영합니다.

## 📋 목차

- [버그 보고 방법](#-버그-보고-방법)
- [기능 제안 방법](#-기능-제안-방법)
- [개발 환경 설정](#-개발-환경-설정)
- [코드 스타일](#-코드-스타일)
- [Pull Request 프로세스](#-pull-request-프로세스)
- [안전 규칙](#-안전-규칙)

## 🐛 버그 보고 방법

이슈를 생성하기 전에:
1. [기존 이슈](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues)를 확인 — 버그가 이미 알려져 있을 수 있습니다.
2. [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)에서 최신 버전으로 업데이트하세요.
3. 진단 정보를 수집하세요.

이슈에 다음을 명시하세요:
- **프로그램 버전** (시작 시 배너에서)
- **Windows 버전** (Win+R → `winver`)
- **아키텍처** (x64 / ARM64)
- **재현 단계**
- **예상 동작**
- **실제 동작**
- **프로그램 로그** (파일 `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — 이슈에 첨부)

## 💡 기능 제안 방법

1. `enhancement` 라벨을 붙여 이슈를 생성하세요.
2. 사용 사례를 설명하세요: 왜 필요한지, 어떤 문제를 해결하는지.
3. 이 기능을 구현할 수 있는 API/DLL을 제안하세요 (알고 있는 경우).

## 🛠 개발 환경 설정

### 요구 사항
- **Windows 10** (빌드 19041+) 또는 **Windows 11**
- **.NET 6.0 SDK** 이상 — https://dotnet.microsoft.com/download
- (선택) Visual Studio 2022 / Rider / VS Code

### 빌드
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

결과: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 실행
프로그램은 관리자 권한이 필요합니다 (매니페스트에 이미 지정됨).
우클릭 → **관리자 권한으로 실행**.

## 🎨 코드 스타일

### 일반 규칙
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 들여쓰기에 4개의 공백 사용, **탭 사용 금지**
- 줄 길이 최대 120자
- public 멤버는 `PascalCase`, 지역 변수는 `camelCase`

### P/Invoke 래퍼
- HRESULT를 반환하는 함수에 `PreserveSig = false` 사용
- 함수 이름에 `W` 접미사(Unicode) 유지: `CreateFile`이 아닌 `CreateFileW`
- 상수는 Win32 스타일로 명명: `GENERIC_READ`, `OPEN_EXISTING`

### 로깅
정적 `Logger.Instance`를 사용:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### 현지화
모든 UI 문자열은 `Resources/strings.{ru,en,zh}.json`에 있어야 하며 다음으로 접근:
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Pull Request 프로세스

1. 저장소를 **포크**
2. 브랜치 생성: `git checkout -b feature/my-feature`
3. 명확한 메시지와 함께 **커밋**:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. 푸시: `git push origin feature/my-feature`
5. 변경 사항 설명과 함께 `main`으로 PR 열기

### 커밋 접두사
- `feat:` — 새로운 기능
- `fix:` — 버그 수정
- `docs:` — 문서화만
- `refactor:` — 동작 변경 없는 리팩토링
- `test:` — 테스트 추가/수정
- `chore:` — 프로젝트 유지 관리 (종속성, .gitignore 등)
- `i18n:` — 현지화 변경

## ⚠️ 안전 규칙

이 도구는 중요한 시스템 구성 요소를 다룹니다. 따라서:

1. **시스템 파일을 조용히 삭제하는 코드를 절대 커밋하지 마세요** — 모든 삭제는 명시적이고 로그에 기록되어야 합니다.
2. **PR 전에 가상 머신에서 테스트** — 특히 `RegistryRestoreManager`, `BootRecoveryManager` 또는 `WinSxsRepairManager`를 변경할 때.
3. **취소할 수 없는 호출**(예: `FormatEx`, `DeleteVolumeMountPoint`)을 사용자 확인 없이 추가하지 마세요.
4. **P/Invoke 서명** — [Win32 문서](https://learn.microsoft.com/windows/win32/api/)와 대조하여 다시 확인. 서명 오류 = 프로그램 충돌 또는 메모리 손상.
5. **개인 키**와 모든 자격 증명은 저장소에 금지됩니다.

## 📜 라이선스

기여함으로써, 귀하의 코드가 [MIT](LICENSE) 라이선스로 게시되는 것에 동의하게 됩니다.

## 🙏 감사의 말

기여자는 [README.md](README.md)에 기재됩니다.
