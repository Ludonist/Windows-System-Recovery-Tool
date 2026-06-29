# Win32 API Referenz — Verwendete Funktionen

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

Ausführliche Referenz zu allen nativen Win32-APIs, die vom System Restore Tool verwendet werden. Alle Aufrufe erfolgen über P/Invoke aus C#.

## Inhaltsverzeichnis

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
Initialisiert die DISM-API. Muss vor allen anderen DISM-Funktionen aufgerufen werden.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
Öffnet eine DISM-Sitzung. Für Online (aktuelles System) sind alle Parameter = `NULL`.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
Schnelle Integritätsprüfung des Abbilds. Rückgabewerte:
- `DismImageHealthy` (0) — Abbild ist gesund
- `DismImageRepairable` (1) — Beschädigungen vorhanden, reparierbar
- `DismImageNonRepairable` (2) — kritische Beschädigungen

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
Vollständige Überprüfung des Komponentenspeichers. Dauert 5–15 Minuten.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Repariert beschädigte Komponenten aus Windows Update oder einer angegebenen Quelle. Dauert 10–30 Minuten.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Bereinigt den Komponentenspeicher (WinSxS). Mit `ResetBase=true` — entfernt unwiderruflich alle vorherigen Komponentenversionen.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
Analysiert Größe und Zustand des WinSxS-Speichers.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
Prüft, ob eine Datei durch den WFP-Mechanismus (Windows File Protection) geschützt ist.

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
Zählt alle geschützten Dateien auf. Wird in einer Schleife aufgerufen, bis `FALSE` zurückgegeben wird.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
**Undokumentierte** Funktion, die von `sfc.exe /scannow` aufgerufen wird. Verfügbar in Windows 7–11.

Scan-Typen:
- `VerifyOnly` (0) — nur Überprüfung
- `ScanAndRepair` (1) — Überprüfung und Reparatur
- `ScanAtBoot` (2) — Überprüfung beim nächsten Start

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
Deaktiviert temporär den WFP-Schutz für eine Datei. Wird von Installationsprogrammen verwendet.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
Überprüft die Authenticode-Signatur einer Datei. Rückgabewerte:
- `S_OK` (0) — Signatur gültig
- `TRUST_E_NOSIGNATURE` (0x800B0100) — keine Signatur vorhanden
- `TRUST_E_BAD_DIGEST` (0x80096010) — Signatur ungültig (Datei wurde verändert!)
- `CERT_E_EXPIRED` (0x800B0101) — Zertifikat abgelaufen
- `CERT_E_REVOKED` (0x800B010C) — Zertifikat widerrufen
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — nicht vertrauenswürdige Root

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
Extrahiert Zertifikat und Signatur aus einer Datei zur weiteren Analyse.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
Zählt Zertifikate in einem Signaturspeicher auf.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
Ruft den Subject-Namen (Eigentümer) oder Issuer-Namen (Aussteller) eines Zertifikats ab. Wird verwendet, um zu prüfen, dass `kernel32.dll` tatsächlich von "Microsoft Windows" signiert ist.

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
Erstellt einen Systemwiederherstellungspunkt. Für die vollständige Erstellung sind zwei Aufrufe erforderlich:
1. `BEGIN_SYSTEM_CHANGE` — beginnt die Erstellung
2. `END_SYSTEM_CHANGE` — schließt ab (unter Verwendung der `llSequenceNumber` aus dem ersten Aufruf)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
Öffnet eine Verbindung zum SCM.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
Öffnet einen vorhandenen Dienst anhand seines Namens.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
Starten / Steuern eines Dienstes. `SERVICE_CONTROL_STOP` (1) — Stopp.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
Ruft den aktuellen Status des Dienstes ab (Running / Stopped / Paused / ...).

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
Mit `MOVEFILE_DELAY_UNTIL_REBOOT` — verschiebt die Operation bis zum nächsten Neustart. Wird verwendet, um gesperrte Dateien zu ersetzen.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
Neustart / Herunterfahren / Abmelden. Erfordert die Berechtigung `SeShutdownPrivilege`.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
Speichern und Wiederherstellen von Registry-Hives in eine Datei. Erfordert `SeBackupPrivilege` und `SeRestorePrivilege`.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
Registrieren die Anwendung für automatische Wiederherstellung/Neustart bei einem Absturz.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
Leert einen Ereignisprotokoll-Kanal und speichert den Inhalt optional in einer `.evtx`-Datei.

---

## VSS API (vssapi.dll)

VSS ist als COM-Schnittstelle `IVssBackupComponents` implementiert. Erstellen einer Schattenkopie:
1. `CreateVssBackupComponents` — erstellt das Objekt
2. `InitializeForBackup` — Initialisierung
3. `SetBackupState` — Typ festlegen
4. `GatherWriterMetadata` — Metadaten sammeln
5. `StartSnapshotSet` / `AddToSnapshotSet` — Volumes hinzufügen
6. `PrepareForBackup` / `DoSnapshotSet` — Schattenkopie erstellen

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
Erstellt und sendet einen Fehlerbericht.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
Erstellt einen Geräteinformationssatz. Mit `DIGCF_PRESENT | DIGCF_ALLCLASSES` — alle vorhandenen Geräte.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
Zählt Geräte im Satz auf.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
Ruft Geräteeigenschaften ab (Beschreibung, Klasse, Dienst usw.).

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
Löst einen Scan auf Hardwareänderungen aus (Geräte-Manager →"Nach Hardwareänderungen suchen").

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
Abrufen/Festlegen des aktiven Energieschemas.

Vordefinierte Schemata:
- `GUID_MAX_POWER_SAVINGS` — Energiesparen
- `GUID_TYPICAL_POWER_SAVINGS` — Ausgeglichen
- `GUID_MIN_POWER_SAVINGS` — Hohe Leistung

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
Ruft verschiedene Energieinformationen ab. Mit `SystemBatteryState` (5) — Batteriestatus.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
Energieeigenschaften des Systems (S1–S5, Ruhezustand, Netzschalter usw.).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
Versetzt das System in den Standby-/Ruhezustand.

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
Wird für die HTTP-Verfügbarkeitsprüfung von Windows Update-Servern verwendet.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
Prüft die Internetverbindung.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
Initialisiert Winsock für Netzwerkoperationen auf niedriger Ebene.

---

## Microsoft.Dism NuGet

Ein verwalteter Wrapper über der DISM-API. Installation über NuGet:
```bash
dotnet add package Microsoft.Dism
```

Verwendung:
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

Version 3.2.0 bietet `ScanImageHealth` und `CleanupImage` nicht an — dafür verwenden wir unser eigenes P/Invoke.

---

## COM WUA API

Windows Update Agent über COM (`Microsoft.Update.Session`):

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
