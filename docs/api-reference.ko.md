# Win32 API 참조 — 사용된 함수

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

System Restore Tool이 사용하는 모든 네이티브 Win32 API에 대한 상세 참조입니다. 모든 호출은 C#에서 P/Invoke를 통해 수행됩니다.

## 목차

- [DISM API (dismapi.dll)](#dism-api-dismapidll)
- [SFC API (sfc.dll, sfc_os.dll)](#sfc-api-sfcdll-sfc_osdll)
- [WinTrust API (wintrust.dll, crypt32.dll)](#wintrust-api-wintrustdll-crypt32dll)
- [System Restore (srclient.dll)](#system-restore-srclientdll)
- [Service Control Manager (advapi32.dll)](#service-control-manager-advapi32dll)
- [Kernel32 API](#kernel32-api)
- [EventLog API (wevtapi.dll)](#eventlog-api-wevtapidll)
- [VSS API (vssapi.dll)](#vss-api-vssapidll)
- [WER API (wer.dll)](#wer-api-werdll)
- [SetupAPI (setupapi.dll)](#setupapi-setupapidll)
- [Power API (powrprof.dll)](#power-api-powrprofdll)
- [Network API (winhttp/wininet/ws2_32)](#network-api)
- [Microsoft.Dism NuGet](#microsoftdism-nuget)
- [COM WUA API](#com-wua-api)

---

## DISM API (dismapi.dll)

### DismInitialize
```c
void DismInitialize(DismLogLevel LogLevel, LPCWSTR LogFilePath, LPCWSTR ScratchDirectory);
```
DISM API를 초기화합니다. 다른 모든 DISM 함수보다 먼저 호출해야 합니다.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
DISM 세션을 엽니다. Online(현재 시스템)의 경우 모든 매개변수 = `NULL`입니다.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
이미지 무결성에 대한 빠른 검사입니다. 반환값:
- `DismImageHealthy` (0) — 이미지가 정상
- `DismImageRepairable` (1) — 손상이 있음, 복구 가능
- `DismImageNonRepairable` (2) — 심각한 손상

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
구성 요소 저장소 전체 검사. 5–15분 소요됩니다.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Windows Update 또는 지정된 소스에서 손상된 구성 요소를 복구합니다. 10–30분 소요됩니다.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
구성 요소 저장소(WinSxS)를 정리합니다. `ResetBase=true`이면 — 모든 이전 버전의 구성 요소를 되돌릴 수 없게 제거합니다.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
WinSxS 저장소의 크기와 상태를 분석합니다.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
파일이 WFP(Windows File Protection) 메커니즘으로 보호되는지 확인합니다.

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
모든 보호된 파일을 열거합니다. `FALSE`를 반환할 때까지 루프에서 호출됩니다.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
`sfc.exe /scannow`가 호출하는 **문서화되지 않은** 함수. Windows 7–11에서 사용 가능합니다.

검사 유형:
- `VerifyOnly` (0) — 확인만
- `ScanAndRepair` (1) — 확인 및 복구
- `ScanAtBoot` (2) — 다음 부팅 시 검사

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
파일에 대한 WFP 보호를 일시적으로 비활성화합니다. 설치 프로그램이 사용합니다.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
파일의 Authenticode 서명을 검증합니다. 반환값:
- `S_OK` (0) — 서명이 유효함
- `TRUST_E_NOSIGNATURE` (0x800B0100) — 서명 없음
- `TRUST_E_BAD_DIGEST` (0x80096010) — 서명이 유효하지 않음(파일이 수정되었습니다!)
- `CERT_E_EXPIRED` (0x800B0101) — 인증서 만료
- `CERT_E_REVOKED` (0x800B010C) — 인증서 해지됨
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — 신뢰할 수 없는 루트

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
파일에서 인증서와 서명을 추출하여 추가 분석에 사용합니다.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
서명 저장소의 인증서를 열거합니다.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
인증서의 Subject(소유자) 또는 Issuer(발급자) 이름을 가져옵니다. `kernel32.dll`이 "Microsoft Windows"에 의해 서명되었는지 확인하는 데 사용됩니다.

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
시스템 복원 지점을 만듭니다. 완전한 생성에는 두 번의 호출이 필요합니다:
1. `BEGIN_SYSTEM_CHANGE` — 생성 시작
2. `END_SYSTEM_CHANGE` — 완료(첫 번째 호출의 `llSequenceNumber` 사용)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
SCM에 대한 연결을 엽니다.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
이름으로 기존 서비스를 엽니다.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
서비스 시작 / 제어. `SERVICE_CONTROL_STOP` (1) — 중지.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
서비스의 현재 상태(Running / Stopped / Paused / ...)를 가져옵니다.

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
`MOVEFILE_DELAY_UNTIL_REBOOT`를 사용하면 — 작업을 다음 재부팅까지 지연시킵니다. 잠긴 파일을 교체하는 데 사용됩니다.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
재부팅 / 종료 / 로그오프. `SeShutdownPrivilege` 권한이 필요합니다.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
레지스트리 하이브를 파일에 저장하고 복원합니다. `SeBackupPrivilege` 및 `SeRestorePrivilege`가 필요합니다.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
충돌 시 자동 복구/재시작을 위해 응용 프로그램을 등록합니다.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
이벤트 로그 채널을 지우며, 선택적으로 내용을 `.evtx` 파일에 저장합니다.

---

## VSS API (vssapi.dll)

VSS는 COM 인터페이스 `IVssBackupComponents`로 구현됩니다. 볼륨 섀도 복사본 생성:
1. `CreateVssBackupComponents` — 객체 생성
2. `InitializeForBackup` — 초기화
3. `SetBackupState` — 유형 설정
4. `GatherWriterMetadata` — 메타데이터 수집
5. `StartSnapshotSet` / `AddToSnapshotSet` — 볼륨 추가
6. `PrepareForBackup` / `DoSnapshotSet` — 스냅샷 생성

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
오류 보고서를 작성하고 제출합니다.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
장치 정보 집합을 만듭니다. `DIGCF_PRESENT | DIGCF_ALLCLASSES`를 사용하면 — 모든 존재하는 장치.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
집합의 장치를 열거합니다.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
장치 속성(설명, 클래스, 서비스 등)을 가져옵니다.

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
하드웨어 변경 사항 검사를 시작합니다(장치 관리자 →"하드웨어 변경 사항 검색").

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
활성 전원 계획을 가져오거나 설정합니다.

미리 정의된 계획:
- `GUID_MAX_POWER_SAVINGS` — 절전
- `GUID_TYPICAL_POWER_SAVINGS` — 균형
- `GUID_MIN_POWER_SAVINGS` — 고성능

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
다양한 전원 정보를 가져옵니다. `SystemBatteryState` (5)로 — 배터리 상태.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
시스템 전원 기능(S1–S5, 최대 절전 모드, 전원 버튼 등).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
시스템을 대기/최대 절전 모드로 전환합니다.

---

## Network API

### WinHTTP
```c
HINTERNET WinHttpOpen(LPCWSTR pszAgentW, DWORD dwAccessType, LPCWSTR pszProxyW, LPCWSTR pszProxyBypassW, DWORD dwFlags);
HINTERNET WinHttpConnect(HINTERNET hSession, LPCWSTR pswzServerName, INTERNET_PORT nServerPort, DWORD dwReserved);
HINTERNET WinHttpOpenRequest(HINTERNET hConnect, LPCWSTR pwszVerb, ...);
BOOL WinHttpSendRequest(...);
BOOL WinHttpReceiveResponse(...);
BOOL WinHttpQueryHeaders(...);
```
Windows Update 서버의 가용성을 HTTP로 확인하는 데 사용됩니다.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
인터넷 연결을 확인합니다.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
저수준 네트워크 작업을 위해 Winsock을 초기화합니다.

---

## Microsoft.Dism NuGet

DISM API에 대한 관리 래퍼. NuGet으로 설치:
```bash
dotnet add package Microsoft.Dism
```

사용법:
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

버전 3.2.0은 `ScanImageHealth`와 `CleanupImage`를 제공하지 않습니다 — 이를 위해 자체 P/Invoke를 사용합니다.

---

## COM WUA API

COM을 통한 Windows Update Agent(`Microsoft.Update.Session`):

```csharp
dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
dynamic searcher = session.CreateUpdateSearcher();
var result = searcher.Search("IsInstalled=0 and Type='Software'");
foreach (var update in result.Updates)
    Console.WriteLine(update.Title);
```

---

## 링크

- [Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list)
- [Windows apps API reference](https://learn.microsoft.com/windows/apps/api-reference/)
- [Microsoft.Dism on GitHub](https://github.com/jeffkl/ManagedDism)
- [pinvoke.net](https://www.pinvoke.net/)
