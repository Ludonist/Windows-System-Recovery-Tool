# Referência API Win32 — Funções utilizadas

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

Referência detalhada de todas as APIs Win32 nativas utilizadas pelo System Restore Tool. Todas as chamadas são feitas via P/Invoke a partir do C#.

## Conteúdo

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
Inicializa a API do DISM. Deve ser chamada antes de qualquer outra função do DISM.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
Abre uma sessão do DISM. Para Online (sistema atual), todos os parâmetros = `NULL`.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
Verificação rápida da integridade da imagem. Retorna:
- `DismImageHealthy` (0) — imagem saudável
- `DismImageRepairable` (1) — há corrupção, reparável
- `DismImageNonRepairable` (2) — corrupção crítica

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
Verificação completa do repositório de componentes. Leva de 5 a 15 minutos.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Repara componentes corrompidos a partir do Windows Update ou de uma fonte especificada. Leva de 10 a 30 minutos.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Limpa o repositório de componentes (WinSxS). Com `ResetBase=true` — remove de forma irreversível todas as versões anteriores dos componentes.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
Analisa o tamanho e o estado do repositório WinSxS.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
Verifica se um arquivo está protegido pelo mecanismo WFP (Windows File Protection).

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
Enumera todos os arquivos protegidos. Chamada em loop até retornar `FALSE`.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
Função **não documentada** invocada por `sfc.exe /scannow`. Disponível no Windows 7–11.

Tipos de verificação:
- `VerifyOnly` (0) — apenas verificação
- `ScanAndRepair` (1) — verificação e reparo
- `ScanAtBoot` (2) — verificar no próximo boot

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
Desativa temporariamente a proteção WFP para um arquivo. Usada por instaladores.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
Verifica a assinatura Authenticode de um arquivo. Retorna:
- `S_OK` (0) — assinatura válida
- `TRUST_E_NOSIGNATURE` (0x800B0100) — sem assinatura
- `TRUST_E_BAD_DIGEST` (0x80096010) — assinatura inválida (arquivo modificado!)
- `CERT_E_EXPIRED` (0x800B0101) — certificado expirado
- `CERT_E_REVOKED` (0x800B010C) — certificado revogado
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — raiz não confiável

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
Extrai o certificado e a assinatura de um arquivo para análise posterior.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
Enumera certificados em um repositório de assinaturas.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
Obtém o nome do Subject (proprietário) ou do Issuer (emissor) de um certificado. Usado para verificar se `kernel32.dll` está assinado especificamente pela "Microsoft Windows".

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
Cria um ponto de restauração do sistema. A criação completa requer duas chamadas:
1. `BEGIN_SYSTEM_CHANGE` — inicia a criação
2. `END_SYSTEM_CHANGE` — conclui (usando o `llSequenceNumber` da primeira chamada)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
Abre uma conexão com o SCM.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
Abre um serviço existente pelo nome.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
Iniciar / controlar um serviço. `SERVICE_CONTROL_STOP` (1) — parar.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
Obtém o estado atual do serviço (Running / Stopped / Paused / ...).

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
Com `MOVEFILE_DELAY_UNTIL_REBOOT` — adia a operação até o próximo reboot. Usado para substituir arquivos bloqueados.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
Reinício / desligamento / logout. Requer o privilégio `SeShutdownPrivilege`.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
Salva e restaura hives do registro em um arquivo. Requer `SeBackupPrivilege` e `SeRestorePrivilege`.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
Registram o aplicativo para recuperação/reinício automático em caso de falha.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
Limpa um canal do log de eventos, salvando opcionalmente o conteúdo em um arquivo `.evtx`.

---

## VSS API (vssapi.dll)

O VSS é implementado como a interface COM `IVssBackupComponents`. Criação de uma cópia de sombra do volume:
1. `CreateVssBackupComponents` — cria o objeto
2. `InitializeForBackup` — inicialização
3. `SetBackupState` — define o tipo
4. `GatherWriterMetadata` — coleta metadados
5. `StartSnapshotSet` / `AddToSnapshotSet` — adiciona volumes
6. `PrepareForBackup` / `DoSnapshotSet` — cria a cópia de sombra

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
Cria e envia um relatório de erros.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
Cria um conjunto de informações de dispositivos. Com `DIGCF_PRESENT | DIGCF_ALLCLASSES` — todos os dispositivos presentes.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
Enumera dispositivos no conjunto.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
Obtém propriedades do dispositivo (descrição, classe, serviço, etc.).

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
Dispara uma verificação de alterações de hardware (Gerenciador de Dispositivos →"Procurar alterações de hardware").

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
Obter/definir o esquema de energia ativo.

Esquemas predefinidos:
- `GUID_MAX_POWER_SAVINGS` — Economia de energia
- `GUID_TYPICAL_POWER_SAVINGS` — Equilibrado
- `GUID_MIN_POWER_SAVINGS` — Alto desempenho

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
Obtém várias informações de energia. Com `SystemBatteryState` (5) — estado da bateria.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
Capacidades de energia do sistema (S1–S5, hibernação, botão de energia, etc.).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
Coloca o sistema em modo de suspensão/hibernação.

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
Usado para verificação HTTP da disponibilidade dos servidores do Windows Update.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
Verifica a conexão com a Internet.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
Inicializa o Winsock para operações de rede de baixo nível.

---

## Microsoft.Dism NuGet

Um wrapper gerenciado sobre a API do DISM. Instalado via NuGet:
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

A versão 3.2.0 não fornece `ScanImageHealth` e `CleanupImage` — para esses usamos nosso próprio P/Invoke.

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
