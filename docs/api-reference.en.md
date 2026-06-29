# Win32 API Reference — Used Functions

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

Detailed reference for all native Win32 APIs used by System Restore Tool. All calls are made via P/Invoke from C#.

## Contents

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
Initializes the DISM API. Must be called before any other DISM functions.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
Opens a DISM session. For Online (current system), all parameters = `NULL`.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
Quick integrity check of the image. Returns:
- `DismImageHealthy` (0) — image is healthy
- `DismImageRepairable` (1) — corruption present, repairable
- `DismImageNonRepairable` (2) — critical corruption

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
Full scan of the component store. Takes 5–15 minutes.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Repairs corrupted components from Windows Update or a specified source. Takes 10–30 minutes.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Cleans up the component store (WinSxS). With `ResetBase=true` — irreversibly removes all previous versions of components.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
Analyzes the size and state of the WinSxS store.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
Checks whether a file is protected by the WFP (Windows File Protection) mechanism.

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
Enumerates all protected files. Called in a loop until it returns `FALSE`.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
**Undocumented** function invoked by `sfc.exe /scannow`. Available in Windows 7–11.

Scan types:
- `VerifyOnly` (0) — verification only
- `ScanAndRepair` (1) — verify and repair
- `ScanAtBoot` (2) — scan at next boot

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
Temporarily disables WFP protection for a file. Used by installers.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
Verifies the Authenticode signature of a file. Returns:
- `S_OK` (0) — signature is valid
- `TRUST_E_NOSIGNATURE` (0x800B0100) — no signature present
- `TRUST_E_BAD_DIGEST` (0x80096010) — signature invalid (file modified!)
- `CERT_E_EXPIRED` (0x800B0101) — certificate expired
- `CERT_E_REVOKED` (0x800B010C) — certificate revoked
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — untrusted root

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
Extracts the certificate and signature from a file for further analysis.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
Enumerates certificates in a signature store.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
Retrieves the Subject (owner) or Issuer name of a certificate. Used to verify that `kernel32.dll` is signed specifically by "Microsoft Windows".

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
Creates a system restore point. Full creation requires two calls:
1. `BEGIN_SYSTEM_CHANGE` — starts the creation
2. `END_SYSTEM_CHANGE` — completes it (using the `llSequenceNumber` from the first call)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
Opens a connection to the SCM.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
Opens an existing service by name.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
Start / control a service. `SERVICE_CONTROL_STOP` (1) — stop.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
Retrieves the current state of the service (Running / Stopped / Paused / ...).

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
With `MOVEFILE_DELAY_UNTIL_REBOOT` — defers the operation until the next reboot. Used to replace locked files.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
Reboot / shutdown / logout. Requires the `SeShutdownPrivilege` privilege.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
Save and restore registry hives to a file. Requires `SeBackupPrivilege` and `SeRestorePrivilege`.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
Register the application for automatic recovery/restart on crash.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
Clears an event log channel, optionally saving the contents to an `.evtx` file.

---

## VSS API (vssapi.dll)

VSS is implemented as the COM interface `IVssBackupComponents`. Creating a volume shadow copy:
1. `CreateVssBackupComponents` — creates the object
2. `InitializeForBackup` — initialization
3. `SetBackupState` — set the type
4. `GatherWriterMetadata` — gather metadata
5. `StartSnapshotSet` / `AddToSnapshotSet` — add volumes
6. `PrepareForBackup` / `DoSnapshotSet` — create the snapshot

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
Create and submit an error report.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
Creates a device information set. With `DIGCF_PRESENT | DIGCF_ALLCLASSES` — all present devices.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
Enumerates devices in the set.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
Retrieves device properties (description, class, service, etc.).

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
Triggers a scan for hardware changes (Device Manager → "Scan for hardware changes").

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
Get/set the active power scheme.

Predefined schemes:
- `GUID_MAX_POWER_SAVINGS` — Power saver
- `GUID_TYPICAL_POWER_SAVINGS` — Balanced
- `GUID_MIN_POWER_SAVINGS` — High performance

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
Retrieves various power information. With `SystemBatteryState` (5) — battery state.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
System power capabilities (S1–S5, hibernation, power button, etc.).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
Puts the system into sleep/hibernate mode.

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
Used for HTTP availability checks of Windows Update servers.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
Check internet connectivity.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
Initialize Winsock for low-level network operations.

---

## Microsoft.Dism NuGet

A managed wrapper over the DISM API. Installed via NuGet:
```bash
dotnet add package Microsoft.Dism
```

Usage:
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

Version 3.2.0 does not provide `ScanImageHealth` and `CleanupImage` — for these we use our own P/Invoke.

---

## COM WUA API

Windows Update Agent via COM (`Microsoft.Update.Session`):

```csharp
dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
dynamic searcher = session.CreateUpdateSearcher();
var result = searcher.Search("IsInstalled=0 and Type='Software'");
foreach (var update in result.Updates)
    Console.WriteLine(update.Title);
```

---

## Links

- [Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list)
- [Windows apps API reference](https://learn.microsoft.com/windows/apps/api-reference/)
- [Microsoft.Dism on GitHub](https://github.com/jeffkl/ManagedDism)
- [pinvoke.net](https://www.pinvoke.net/)
