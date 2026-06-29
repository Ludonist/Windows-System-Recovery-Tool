# Référence API Win32 — Fonctions utilisées

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

Référence détaillée de toutes les API Win32 natives utilisées par System Restore Tool. Tous les appels sont effectués via P/Invoke depuis C#.

## Sommaire

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
Initialise l'API DISM. Doit être appelée avant toute autre fonction DISM.

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
Ouvre une session DISM. Pour Online (système actuel), tous les paramètres = `NULL`.

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
Vérification rapide de l'intégrité de l'image. Retourne :
- `DismImageHealthy` (0) — image saine
- `DismImageRepairable` (1) — corruptions présentes, réparable
- `DismImageNonRepairable` (2) — corruptions critiques

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
Analyse complète du magasin de composants. Prend 5–15 minutes.

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Répare les composants corrompus depuis Windows Update ou une source spécifiée. Prend 10–30 minutes.

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Nettoie le magasin de composants (WinSxS). Avec `ResetBase=true` — supprime de façon irréversible toutes les versions précédentes des composants.

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
Analyse la taille et l'état du magasin WinSxS.

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
Vérifie si un fichier est protégé par le mécanisme WFP (Windows File Protection).

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
Énumère tous les fichiers protégés. Appelée en boucle jusqu'à ce qu'elle retourne `FALSE`.

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
Fonction **non documentée** appelée par `sfc.exe /scannow`. Disponible sous Windows 7–11.

Types d'analyse :
- `VerifyOnly` (0) — vérification uniquement
- `ScanAndRepair` (1) — vérification et réparation
- `ScanAtBoot` (2) — analyse au prochain démarrage

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
Désactive temporairement la protection WFP pour un fichier. Utilisée par les programmes d'installation.

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
Vérifie la signature Authenticode d'un fichier. Retourne :
- `S_OK` (0) — signature valide
- `TRUST_E_NOSIGNATURE` (0x800B0100) — aucune signature présente
- `TRUST_E_BAD_DIGEST` (0x80096010) — signature invalide (fichier modifié !)
- `CERT_E_EXPIRED` (0x800B0101) — certificat expiré
- `CERT_E_REVOKED` (0x800B010C) — certificat révoqué
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — racine non approuvée

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
Extrait le certificat et la signature d'un fichier pour une analyse ultérieure.

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
Énumère les certificats dans un magasin de signatures.

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
Récupère le nom du Subject (propriétaire) ou de l'Issuer (émetteur) d'un certificat. Utilisé pour vérifier que `kernel32.dll` est bien signé par « Microsoft Windows ».

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
Crée un point de restauration système. La création complète nécessite deux appels :
1. `BEGIN_SYSTEM_CHANGE` — commence la création
2. `END_SYSTEM_CHANGE` — termine (en utilisant le `llSequenceNumber` du premier appel)

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
Ouvre une connexion au SCM.

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
Ouvre un service existant par son nom.

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
Démarrer / contrôler un service. `SERVICE_CONTROL_STOP` (1) — arrêt.

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
Récupère l'état actuel du service (Running / Stopped / Paused / ...).

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
Avec `MOVEFILE_DELAY_UNTIL_REBOOT` — reporte l'opération au prochain redémarrage. Utilisé pour remplacer les fichiers verrouillés.

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
Redémarrage / arrêt / déconnexion. Nécessite le privilège `SeShutdownPrivilege`.

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
Sauvegarde et restauration des ruches du registre dans un fichier. Nécessite `SeBackupPrivilege` et `SeRestorePrivilege`.

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
Inscrit l'application pour une récupération/redémarrage automatique en cas de crash.

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
Efface un canal du journal d'événements, en sauvegardant optionnellement le contenu dans un fichier `.evtx`.

---

## VSS API (vssapi.dll)

VSS est implémenté comme l'interface COM `IVssBackupComponents`. Création d'un cliché instantané :
1. `CreateVssBackupComponents` — crée l'objet
2. `InitializeForBackup` — initialisation
3. `SetBackupState` — définition du type
4. `GatherWriterMetadata` — collecte des métadonnées
5. `StartSnapshotSet` / `AddToSnapshotSet` — ajout des volumes
6. `PrepareForBackup` / `DoSnapshotSet` — création du cliché

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
Création et envoi d'un rapport d'erreur.

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
Crée un ensemble d'informations sur les périphériques. Avec `DIGCF_PRESENT | DIGCF_ALLCLASSES` — tous les périphériques présents.

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
Énumère les périphériques de l'ensemble.

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
Récupère les propriétés du périphérique (description, classe, service, etc.).

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
Déclenche une analyse des modifications matérielles (Gestionnaire de périphériques → « Rechercher les modifications matérielles »).

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
Obtenir/définir le schéma d'alimentation actif.

Schémas prédéfinis :
- `GUID_MAX_POWER_SAVINGS` — Économie d'énergie
- `GUID_TYPICAL_POWER_SAVINGS` — Équilibré
- `GUID_MIN_POWER_SAVINGS` — Performances élevées

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
Récupère diverses informations sur l'alimentation. Avec `SystemBatteryState` (5) — état de la batterie.

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
Capacités d'alimentation du système (S1–S5, mise en veille prolongée, bouton d'alimentation, etc.).

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
Met le système en mode veille/hibernation.

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
Utilisé pour la vérification HTTP de la disponibilité des serveurs Windows Update.

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
Vérifie la connexion Internet.

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
Initialise Winsock pour les opérations réseau de bas niveau.

---

## Microsoft.Dism NuGet

Un wrapper managé sur l'API DISM. Installé via NuGet :
```bash
dotnet add package Microsoft.Dism
```

Utilisation :
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

La version 3.2.0 ne fournit pas `ScanImageHealth` et `CleanupImage` — pour ceux-ci, nous utilisons notre propre P/Invoke.

---

## COM WUA API

Windows Update Agent via COM (`Microsoft.Update.Session`) :

```csharp
dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
dynamic searcher = session.CreateUpdateSearcher();
var result = searcher.Search("IsInstalled=0 and Type='Software'");
foreach (var update in result.Updates)
    Console.WriteLine(update.Title);
```

---

## Liens

- [Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list)
- [Windows apps API reference](https://learn.microsoft.com/windows/apps/api-reference/)
- [Microsoft.Dism on GitHub](https://github.com/jeffkl/ManagedDism)
- [pinvoke.net](https://www.pinvoke.net/)
