# 보안 정책 / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# 보안 정책 (한국어)

## 지원되는 버전

| 버전 | 지원 | 보안 업데이트 |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ 활성 | 현재 |
| 2.0.x  | ⚠️ 중요만 | 2.6.0 출시까지 |
| < 2.0  | ❌ 없음     | — |

## 취약점 신고

취약점을 발견하신 경우, **공개 issue를 생성하지 마세요**.

대신:
1. 이메일 전송: `security@example.com` (본인의 이메일로 교체)
2. 제목: `[SECURITY] System Restore Tool — <간단한 설명>`

### 보고서에 포함할 내용
- 취약점 설명 및 잠재적 영향
- 재현 단계
- 프로그램 버전 및 Windows 버전
- 가능한 완화 조치(알려진 경우)

### SLA
- **수신 확인** — 48시간 이내
- **초기 평가** — 영업일 기준 5일 이내
- **수정 또는 우회 방법** — 30일 이내(심각도에 따라)

## 프로그램 보안

### 프로그램이 수행하는 작업
- ✅ 오직 사용자의 머신에서 로컬로만 작동
- ✅ 네트워크를 통해 데이터를 전송하지 않음(DISM RestoreHealth는 Windows Update를 사용하므로 예외)
- ✅ 관리자 권한 필요(매니페스트를 통해 지정)
- ✅ 모든 작업을 `%LOCALAPPDATA%\SystemRestoreTool\`에 기록
- ✅ 중요 작업 전에 복원 지점 생성

### 프로그램이 수행하지 않는 작업
- ❌ 개발자 서버로 데이터를 전송하지 않음
- ❌ 추가 바이너리를 다운로드하지 않음(DISM/WU를 통해서만)
- ❌ 사용자 동의 없이 부트로더를 수정하지 않음
- ❌ 확인 없이 파일을 삭제하지 않음

### 권장 사항
1. 사용 전 **복원 지점 생성**(메뉴 항목 17)
2. **익숙하지 않은 작업을 실행하지 마세요** — 각 작업에는 설명이 있습니다
3. 작업 완료 후 **로그 확인**
4. **[Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)에서만 EXE 다운로드** — 제3자 소스에서는 불가

### 공급망
- 소스 코드 — MIT 하에 오픈 소스
- 종속성: `Microsoft.Dism 3.2.0`(NuGet, Microsoft 서명)
- 빌드 — 소스에서 `dotnet publish`를 통해
- 릴리스 — GitHub Actions에서 빌드(투명하게)
