# Referencia API Win32 — Funciones utilizadas

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

Referencia detallada de todas las API Win32 nativas que utiliza System Restore Tool. Todas las llamadas se realizan mediante P/Invoke desde C#.

## Contenido

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
Inicializa la API de DISM. Debe llamarse antes que cualquier otra función de DISM.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
Abre una sesión de DISM. Para Online (sistema actual), todos los parámetros = `NULL`.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
Comprobación rápida de la integridad de la imagen. Devuelve:
- `DismImageHealthy` (0) — la imagen está sana
- `DismImageRepairable` (1) — hay corrupción, reparable
- `DismImageNonRepairable` (2) — corrupción crítica

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
Análisis completo del almacén de componentes. Tarda 5–15 minutos.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Repara componentes dañados desde Windows Update o un origen especificado. Tarda 10–30 minutos.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Limpia el almacén de componentes (WinSxS). Con `ResetBase=true` — elimina de forma irreversible todas las versiones anteriores de los componentes.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
Analiza el tamaño y el estado del almacén WinSxS.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
Comprueba si un archivo está protegido por el mecanismo WFP (Windows File Protection).

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
Enumera todos los archivos protegidos. Se llama en bucle hasta que devuelve `FALSE`.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
Función **no documentada** invocada por `sfc.exe /scannow`. Disponible en Windows 7–11.

Tipos de análisis:
- `VerifyOnly` (0) — solo verificación
- `ScanAndRepair` (1) — verificar y reparar
- `ScanAtBoot` (2) — analizar en el próximo arranque

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
Desactiva temporalmente la protección WFP para un archivo. La usan los instaladores.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
Verifica la firma Authenticode de un archivo. Devuelve:
- `S_OK` (0) — firma válida
- `TRUST_E_NOSIGNATURE` (0x800B0100) — sin firma
- `TRUST_E_BAD_DIGEST` (0x80096010) — firma inválida (¡archivo modificado!)
- `CERT_E_EXPIRED` (0x800B0101) — certificado caducado
- `CERT_E_REVOKED` (0x800B010C) — certificado revocado
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — raíz no confiable

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
Extrae el certificado y la firma de un archivo para su posterior análisis.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
Enumera certificados en un almacén de firmas.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
Obtiene el nombre del Subject (propietario) o del Issuer (emisor) de un certificado. Se utiliza para verificar que `kernel32.dll` esté firmado específicamente por "Microsoft Windows".

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
Crea un punto de restauración del sistema. La creación completa requiere dos llamadas:
1. `BEGIN_SYSTEM_CHANGE` — comienza la creación
2. `END_SYSTEM_CHANGE` — finaliza (usando el `llSequenceNumber` de la primera llamada)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
Abre una conexión con el SCM.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
Abre un servicio existente por su nombre.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
Iniciar / controlar un servicio. `SERVICE_CONTROL_STOP` (1) — detener.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
Obtiene el estado actual del servicio (Running / Stopped / Paused / ...).

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
Con `MOVEFILE_DELAY_UNTIL_REBOOT` — aplaza la operación hasta el próximo reinicio. Se usa para reemplazar archivos bloqueados.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
Reinicio / apagado / cierre de sesión. Requiere el privilegio `SeShutdownPrivilege`.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
Guarda y restaura secciones del registro en un archivo. Requiere `SeBackupPrivilege` y `SeRestorePrivilege`.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
Registran la aplicación para recuperación/reinicio automático tras un fallo.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
Borra un canal del registro de eventos, guardando opcionalmente el contenido en un archivo `.evtx`.

---

## VSS API (vssapi.dll)

VSS está implementado como la interfaz COM `IVssBackupComponents`. Creación de una instantánea de volumen:
1. `CreateVssBackupComponents` — crea el objeto
2. `InitializeForBackup` — inicialización
3. `SetBackupState` — establece el tipo
4. `GatherWriterMetadata` — recopila metadatos
5. `StartSnapshotSet` / `AddToSnapshotSet` — añade volúmenes
6. `PrepareForBackup` / `DoSnapshotSet` — crea la instantánea

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
Creación y envío de un informe de errores.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
Crea un conjunto de información de dispositivos. Con `DIGCF_PRESENT | DIGCF_ALLCLASSES` — todos los dispositivos presentes.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
Enumera los dispositivos del conjunto.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
Obtiene propiedades del dispositivo (descripción, clase, servicio, etc.).

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
Inicia un análisis de cambios de hardware (Administrador de dispositivos → "Buscar cambios de hardware").

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
Obtener/establecer el plan de energía activo.

Planes predefinidos:
- `GUID_MAX_POWER_SAVINGS` — Ahorro de energía
- `GUID_TYPICAL_POWER_SAVINGS` — Equilibrado
- `GUID_MIN_POWER_SAVINGS` — Alto rendimiento

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
Obtiene diversa información de energía. Con `SystemBatteryState` (5) — estado de la batería.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
Capacidades de energía del sistema (S1–S5, hibernación, botón de encendido, etc.).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
Pone el sistema en modo de suspensión/hibernación.

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
Se usa para la verificación HTTP de la disponibilidad de los servidores de Windows Update.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
Comprueba la conexión a Internet.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
Inicializa Winsock para operaciones de red de bajo nivel.

---

## Microsoft.Dism NuGet

Un contenedor administrado sobre la API de DISM. Se instala mediante NuGet:
```bash
dotnet add package Microsoft.Dism
```

Uso:
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

La versión 3.2.0 no proporciona `ScanImageHealth` ni `CleanupImage` — para ellos usamos nuestro propio P/Invoke.

---

## COM WUA API

Windows Update Agent mediante COM (`Microsoft.Update.Session`):

```csharp
dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
dynamic searcher = session.CreateUpdateSearcher();
var result = searcher.Search("IsInstalled=0 and Type='Software'");
foreach (var update in result.Updates)
    Console.WriteLine(update.Title);
```

---

## Enlaces

- [Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list)
- [Windows apps API reference](https://learn.microsoft.com/windows/apps/api-reference/)
- [Microsoft.Dism on GitHub](https://github.com/jeffkl/ManagedDism)
- [pinvoke.net](https://www.pinvoke.net/)
