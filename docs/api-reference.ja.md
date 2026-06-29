# Win32 API リファレンス — 使用される関数

🌐 [🇷🇺 Русский](api-reference.md) · [🇬🇧 English](api-reference.en.md) · [🇨🇳 简体中文](api-reference.zh.md) · [🇩🇪 Deutsch](api-reference.de.md) · [🇫🇷 Français](api-reference.fr.md) · [🇪🇸 Español](api-reference.es.md) · [🇯🇵 日本語](api-reference.ja.md) · [🇰🇷 한국어](api-reference.ko.md) · [🇵🇹 Português](api-reference.pt.md)

System Restore Tool が使用するすべてのネイティブ Win32 API の詳細リファレンス。すべての呼び出しは C# から P/Invoke 経由で行われます。

## 目次

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
DISM API を初期化します。他の DISM 関数より前に呼び出す必要があります。

### DismOpenSession
```c
HRESULT DismOpenSession(LPCWSTR ImagePath, LPCWSTR WindowsDirectory, LPCWSTR SystemDrive, DismSession* Session);
```
DISM セッションを開きます。Online（現在のシステム）の場合、すべてのパラメータ = `NULL`。

### DismCheckImageHealth
```c
HRESULT DismCheckImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, DismImageHealthState* ImageHealthState, HANDLE* CancelEvent);
```
イメージの整合性を簡易チェックします。戻り値:
- `DismImageHealthy` (0) — イメージは正常
- `DismImageRepairable` (1) — 破損あり、修復可能
- `DismImageNonRepairable` (2) — 重大な破損

### DismScanImageHealth
```c
HRESULT DismScanImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent);
```
コンポーネントストアのフルスキャン。5〜15分かかります。

### DismRestoreImageHealth
```c
HRESULT DismRestoreImageHealth(DismSession Session, LPCWSTR SourcePath, BOOL LimitAccess, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
Windows Update または指定したソースから破損コンポーネントを修復します。10〜30分かかります。

### DismStartComponentCleanup
```c
HRESULT DismStartComponentCleanup(DismSession Session, BOOL ResetBase, HANDLE* CancelEvent, DISM_PROGRESS_CALLBACK Progress, PVOID UserData);
```
コンポーネントストア（WinSxS）をクリーンアップします。`ResetBase=true` の場合 — すべての以前のコンポーネントバージョンを不可逆的に削除します。

### DismAnalyzeComponentStore
```c
HRESULT DismAnalyzeComponentStore(DismSession Session, DismComponentStoreInfo** Info);
```
WinSxS ストアのサイズと状態を分析します。

---

## SFC API (sfc.dll, sfc_os.dll)

### SfcIsFileProtected
```c
BOOL SfcIsFileProtected(HANDLE RpcHandle, LPCWSTR ProtFileName);
```
ファイルが WFP（Windows File Protection）機構で保護されているかを確認します。

### SfcGetNextProtectedFile
```c
BOOL SfcGetNextProtectedFile(HANDLE Handle, PPROTECTED_FILE_DATA ProtFileData);
```
すべての保護されたファイルを列挙します。`FALSE` を返すまでループで呼び出します。

### SfcSynchronousScan (sfc_os.dll)
```c
BOOL SfcSynchronousScan(HWND hWnd, SfcScanType ScanType, PVOID Reserved);
```
`sfc.exe /scannow` が呼び出す **非公式** 関数。Windows 7〜11 で利用可能。

スキャンの種類:
- `VerifyOnly` (0) — 検証のみ
- `ScanAndRepair` (1) — 検証と修復
- `ScanAtBoot` (2) — 次回起動時にスキャン

### SfcFileException (sfc_os.dll)
```c
BOOL SfcFileException(HANDLE RpcHandle, LPCWSTR FileName, DWORD Reserved);
```
ファイルの WFP 保護を一時的に無効にします。インストーラーが使用します。

---

## WinTrust API (wintrust.dll, crypt32.dll)

### WinVerifyTrust
```c
LONG WinVerifyTrust(HWND hwnd, GUID* pgActionID, LPVOID pWVTData);
```
ファイルの Authenticode 署名を検証します。戻り値:
- `S_OK` (0) — 署名は有効
- `TRUST_E_NOSIGNATURE` (0x800B0100) — 署名なし
- `TRUST_E_BAD_DIGEST` (0x80096010) — 署名が無効（ファイルが変更されています！）
- `CERT_E_EXPIRED` (0x800B0101) — 証明書の有効期限切れ
- `CERT_E_REVOKED` (0x800B010C) — 証明書が失効
- `CERT_E_UNTRUSTEDROOT` (0x800B0109) — 信頼されないルート

### CryptQueryObject
```c
BOOL CryptQueryObject(DWORD dwObjectType, LPCVOID pvObject, DWORD dwExpectedContentTypeFlags,
    DWORD dwExpectedFormatTypeFlags, DWORD dwFlags, DWORD* pdwMsgAndCertEncodingType,
    DWORD* pdwContentType, DWORD* pdwFormatType, HCERTSTORE* phCertStore,
    HCRYPTMSG* phMsg, const void** ppvContext);
```
ファイルから証明書と署名を抽出し、さらなる分析に供します。

### CertFindCertificateInStore
```c
PCCERT_CONTEXT CertFindCertificateInStore(HCERTSTORE hCertStore, DWORD dwCertEncodingType,
    DWORD dwFindFlags, DWORD dwFindType, const void* pvFindPara, PCCERT_CONTEXT pPrevCertContext);
```
署名ストア内の証明書を列挙します。

### CertGetNameString
```c
BOOL CertGetNameString(PCCERT_CONTEXT pCertContext, DWORD dwType, DWORD dwFlags,
    void* pvTypePara, LPTSTR pszNameString, DWORD cchNameString);
```
証明書の Subject（所有者）または Issuer（発行者）名を取得します。`kernel32.dll` が「Microsoft Windows」によって署名されていることを確認するために使用します。

---

## System Restore (srclient.dll)

### SRSetRestorePoint
```c
BOOL SRSetRestorePointW(PRESTOREPOINTINFOW RestorePtInfo, PSTATEMGRSTATUS SMgrStatus);
```
システム復元ポイントを作成します。完全な作成には2回の呼び出しが必要です:
1. `BEGIN_SYSTEM_CHANGE` — 作成を開始
2. `END_SYSTEM_CHANGE` — 完了（最初の呼び出しの `llSequenceNumber` を使用）

---

## Service Control Manager (advapi32.dll)

### OpenSCManager
```c
SC_HANDLE OpenSCManager(LPCWSTR lpMachineName, LPCWSTR lpDatabaseName, DWORD dwDesiredAccess);
```
SCM への接続を開きます。

### OpenService
```c
SC_HANDLE OpenService(SC_HANDLE hSCManager, LPCWSTR lpServiceName, DWORD dwDesiredAccess);
```
名前で既存のサービスを開きます。

### StartService / ControlService
```c
BOOL StartService(SC_HANDLE hService, DWORD dwNumServiceArgs, LPCWSTR* lpServiceArgVectors);
BOOL ControlService(SC_HANDLE hService, DWORD dwControl, LPSERVICE_STATUS lpServiceStatus);
```
サービスの開始 / 制御。`SERVICE_CONTROL_STOP` (1) — 停止。

### QueryServiceStatus
```c
BOOL QueryServiceStatus(SC_HANDLE hService, LPSERVICE_STATUS lpServiceStatus);
```
サービスの現在の状態（Running / Stopped / Paused / ...）を取得します。

---

## Kernel32 API

### MoveFileEx
```c
BOOL MoveFileEx(LPCWSTR lpExistingFileName, LPCWSTR lpNewFileName, DWORD dwFlags);
```
`MOVEFILE_DELAY_UNTIL_REBOOT` を指定すると — 操作を次回再起動まで遅らせます。ロックされたファイルの置き換えに使用します。

### ExitWindowsEx
```c
BOOL ExitWindowsEx(UINT uFlags, DWORD dwReason);
```
再起動 / シャットダウン / ログオフ。`SeShutdownPrivilege` 特権が必要です。

### RegSaveKey / RegRestoreKey
```c
BOOL RegSaveKey(HKEY hKey, LPCWSTR lpFile, PSECURITY_ATTRIBUTES lpSecurityAttributes);
LONG RegRestoreKey(HKEY hKey, LPCWSTR lpFile, DWORD dwFlags);
```
レジストリハイブをファイルに保存・復元します。`SeBackupPrivilege` および `SeRestorePrivilege` が必要です。

### RegisterApplicationRecoveryCallback / RegisterApplicationRestart
```c
BOOL RegisterApplicationRecoveryCallback(RECOVERY_CALLBACK pRecoveyCallback, PVOID pvParameter, DWORD dwPingInterval, DWORD dwFlags);
BOOL RegisterApplicationRestart(LPCWSTR pwzCommandline, DWORD dwFlags);
```
クラッシュ時の自動回復/再起動に向けてアプリケーションを登録します。

---

## EventLog API (wevtapi.dll)

### EvtClearLog
```c
HRESULT EvtClearLog(EVT_HANDLE Session, LPCWSTR ChannelPath, LPCWSTR TargetFilePath, LPCWSTR Query);
```
イベントログチャネルをクリアし、必要に応じて内容を `.evtx` ファイルに保存します。

---

## VSS API (vssapi.dll)

VSS は COM インターフェース `IVssBackupComponents` として実装されています。ボリュームシャドウコピーの作成:
1. `CreateVssBackupComponents` — オブジェクトを作成
2. `InitializeForBackup` — 初期化
3. `SetBackupState` — タイプを設定
4. `GatherWriterMetadata` — メタデータを収集
5. `StartSnapshotSet` / `AddToSnapshotSet` — ボリュームを追加
6. `PrepareForBackup` / `DoSnapshotSet` — スナップショットを作成

---

## WER API (wer.dll)

### WerReportCreate / WerReportSubmit
```c
HRESULT WerReportCreate(PCWSTR pwzEventType, WER_REPORT_TYPE repType, PWER_REPORT_INFORMATION pReportInformation, HREPORT* phReportHandle);
HRESULT WerReportSubmit(HREPORT hReportHandle, WER_CONSENT consent, DWORD dwFlags, PWER_SUBMIT_RESULT pSubmitResult);
```
エラーレポートを作成して送信します。

---

## SetupAPI (setupapi.dll)

### SetupDiGetClassDevs
```c
HDEVINFO SetupDiGetClassDevs(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
```
デバイス情報セットを作成します。`DIGCF_PRESENT | DIGCF_ALLCLASSES` で — すべての存在するデバイス。

### SetupDiEnumDeviceInfo
```c
BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, PSP_DEVINFO_DATA DeviceInfoData);
```
セット内のデバイスを列挙します。

### SetupDiGetDeviceRegistryProperty
```c
BOOL SetupDiGetDeviceRegistryProperty(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, DWORD* PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
```
デバイスのプロパティ（説明、クラス、サービスなど）を取得します。

### CM_Reenumerate_DevNode (cfgmgr32.dll)
```c
DWORD CM_Reenumerate_DevNode(DEVINST dnDevInst, ULONG ulFlags);
```
ハードウェア変更のスキャンを開始します（デバイス マネージャー →「ハードウェア変更のスキャン」）。

---

## Power API (powrprof.dll)

### PowerGetActiveScheme / PowerSetActiveScheme
```c
DWORD PowerGetActiveScheme(HKEY UserRootPowerKey, GUID** ActivePolicyGuid);
DWORD PowerSetActiveScheme(HKEY UserRootPowerKey, const GUID* SchemeGuid);
```
アクティブな電源プランを取得/設定します。

定義済みプラン:
- `GUID_MAX_POWER_SAVINGS` — 省電力
- `GUID_TYPICAL_POWER_SAVINGS` — バランス
- `GUID_MIN_POWER_SAVINGS` — 高パフォーマンス

### CallNtPowerInformation
```c
DWORD CallNtPowerInformation(POWER_INFORMATION_LEVEL InformationLevel, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
```
各種電源情報を取得します。`SystemBatteryState` (5) で — バッテリー状態。

### GetPwrCapabilities
```c
BOOL GetPwrCapabilities(PSYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);
```
システムの電源機能（S1〜S5、休止状態、電源ボタンなど）。

### SetSuspendState
```c
BOOL SetSuspendState(BOOL Hibernate, BOOL ForceCritical, BOOL DisableWakeEvent);
```
システムをスリープ/休止状態にします。

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
Windows Update サーバーの可用性を HTTP で確認するために使用します。

### WinINet
```c
BOOL InternetGetConnectedState(LPDWORD lpdwFlags, DWORD dwReserved);
```
インターネット接続を確認します。

### Winsock (ws2_32.dll)
```c
int WSAStartup(WORD wVersionRequested, LPWSADATA lpWSAData);
int WSACleanup();
```
低レベルのネットワーク操作のために Winsock を初期化します。

---

## Microsoft.Dism NuGet

DISM API のマネージドラッパー。NuGet でインストール:
```bash
dotnet add package Microsoft.Dism
```

使用方法:
```csharp
DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo, logPath);
using var session = DismApi.OpenOnlineSession();
var health = DismApi.CheckImageHealth(session, false);
DismApi.RestoreImageHealth(session, false, null, progress => { ... });
DismApi.Shutdown();
```

バージョン 3.2.0 は `ScanImageHealth` と `CleanupImage` を提供していません — これらには独自の P/Invoke を使用します。

---

## COM WUA API

COM 経由の Windows Update Agent（`Microsoft.Update.Session`）:

```csharp
dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
dynamic searcher = session.CreateUpdateSearcher();
var result = searcher.Search("IsInstalled=0 and Type='Software'");
foreach (var update in result.Updates)
    Console.WriteLine(update.Title);
```

---

## リンク

- [Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list)
- [Windows apps API reference](https://learn.microsoft.com/windows/apps/api-reference/)
- [Microsoft.Dism on GitHub](https://github.com/jeffkl/ManagedDism)
- [pinvoke.net](https://www.pinvoke.net/)
