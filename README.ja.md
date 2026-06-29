# 🔧 Windows システム回復ツール

<div align="center">

🌐 **利用可能な言語 / Available languages / 可用语言 / Unterstützte Sprachen / Langues disponibles / Idiomas disponibles / 利用可能な言語 / 사용 가능한 언어 / Idiomas disponíveis:**

[🇷🇺 Русский](README.md) · [🇬🇧 English](README.en.md) · [🇨🇳 简体中文](README.zh.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇵🇹 Português](README.pt.md)

🌐 **言語自動検出ドキュメント**: https://ludonist.github.io/Windows-System-Recovery-Tool/

---

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64%20%7C%20ARM64-orange)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Version](https://img.shields.io/badge/version-2.6.0-brightgreen)
![Language](https://img.shields.io/badge/lang-C%23%2010-success)
![Languages](https://img.shields.io/badge/UI%20languages-RU%20%7C%20EN%20%7C%20中文-red)

**Win32 API の直接呼び出しによる Windows 10/11 システムファイル復元のプロフェッショナルツール**

🌐 **多言語UI**: Русский · English · 简体中文 · Deutsch · Français · Español · 日本語 · 한국어 · Português

[機能](#-features) ·
[スクリーンショット](#-screenshots) ·
[インストール](#-installation) ·
[使い方](#-usage) ·
[アーキテクチャ](#-architecture) ·
[API](#-win32-apis-used) ·
[ビルド](#-building-from-source) ·
[貢献](#-contributing)

</div>

---

## 📖 概要

**Windows システム回復ツール** は、Windows 10/11 向けのコンソールアプリケーションで、外部コマンド `cmd.exe` / `dism.exe` / `sfc.exe` を使わず、**Windows API の直接呼び出し**（`dismapi.dll`、`sfc.dll`、`wintrust.dll` など）を通じてシステムファイルの復元と整合性検証を行います。

### 主な機能

- 🎯 **67 の操作** を一つのメニューに集約した復元・診断機能
- 🔌 **17 のネイティブ Windows API** を P/Invoke 経由で使用（シェルコマンド不要）
- 🛡️ **ファイル改ざん検出**: WinVerifyTrust + 証明書の発行者
- 📊 **SHA256/SHA1 スナップショット** でシステム状態を時系列比較
- 🧩 **Microsoft.Dism NuGet** を代替のマネージド実装として使用
- 📋 **詳細なログ** を `%LOCALAPPDATA%\SystemRestoreTool\` に出力
- ⚡ **自己完結型ビルド** — .NET のインストール不要
- 🌐 **多言語UI**: Русский / English / 简体中文 / Deutsch / Français / Español / 日本語 / 한국어 / Português

### このツールが解決する問題

| 症状 | 解決策 |
|---------|----------|
| `sfc /scannow` が破損を検出 | DISM RestoreHealth + SFC /ScanNow |
| DLLのウイルスによる改ざんが疑われる | 全クリティカルファイルに対して WinVerifyTrust を実行 |
| Windows Update が壊れている | SoftwareDistribution のリセット + DLL の再登録 |
| サービスが起動しない | SCM 経由で 40+ の重要サービスを再起動 |
| ブートローダーの障害 | BCD のチェックと回復 |
| 復元ポイントがない | `SRSetRestorePoint` で作成 |
| アイコン/フォントキャッシュの破損 | IconCache.db / FNTCACHE.DAT の再構築 |
| システム整合性への懸念 | SHA256 スナップショット + ベースラインとの比較 |

---

## ✨ 機能

### 1. DISM API による復元 (`dismapi.dll`)

| 操作 | コンソールでの同等コマンド |
|----------|---------------------|
| CheckHealth | `Dism /Online /Cleanup-Image /CheckHealth` |
| ScanHealth | `Dism /Online /Cleanup-Image /ScanHealth` |
| RestoreHealth | `Dism /Online /Cleanup-Image /RestoreHealth` |
| StartComponentCleanup | `Dism /Online /Cleanup-Image /StartComponentCleanup` |
| StartComponentCleanup + ResetBase | `... /StartComponentCleanup /ResetBase` |

### 2. ネイティブ `sfc_os.dll` による SFC

| 操作 | 同等コマンド |
|----------|------------|
| SfcSynchronousScan (ScanAndRepair) | `Sfc /ScanNow` |
| SfcSynchronousScan (VerifyOnly) | `Sfc /VerifyOnly` |
| SfcIsFileProtected | (同等コマンドなし) |
| SfcGetNextProtectedFile | (同等コマンドなし) |

### 3. 整合性チェック

- **40 以上のクリティカルファイル** — kernel32.dll、ntdll.dll、winlogon.exe、lsass.exe、csrss.exe、smss.exe、ドライバ (tcpip.sys、ntfs.sys、volmgr.sys、ACPI.sys、hal.dll、...) など
- **System32 + ドライバのディープチェック** — 全 .dll/.exe/.sys（約 5000 ファイル）
- **System32 + SysWOW64 のフルチェック** — 約 10000 ファイル
- 各ファイルについて: WFP 保護、Authenticode 署名、発行者 (Microsoft かサードパーティか)

### 4. 復元モジュール（14 モジュール）

| モジュール | 目的 | API |
|--------|---------|-----|
| `SystemRestorePointManager` | 復元ポイント | `srclient.dll!SRSetRestorePoint` |
| `ServicesRepairManager` | 40 以上のサービスを再起動 | `advapi32.dll` SCM |
| `BootRecoveryManager` | BCD、bootmgr、winload | WMI + bcdedit |
| `RegistryRestoreManager` | レジストリハイブのバックアップ | `advapi32.dll!RegSaveKey` |
| `WindowsUpdateRepairManager` | WU のリセット | SCM + File API |
| `WinSxsRepairManager` | WinSxS のクリーンアップ | `dismapi.dll!DismAnalyzeComponentStore` |
| `EventLogManager` | イベントログ | `wevtapi.dll!EvtClearLog` |
| `UserEnvRestoreManager` | プロファイル、キャッシュ | advapi32 + File API |
| `FileHashDatabaseManager` | SHA256 スナップショット | `System.Security.Cryptography` |
| `DeviceManager` | デバイス | `setupapi.dll` |
| `PowerOptionsManager` | 電源プラン | `powrprof.dll` |
| `NetworkDiagnosticManager` | ネットワーク | `winhttp.dll` + `ws2_32.dll` |
| `WerManager` | WER レポート | `wer.dll` |
| `WindowsUpdateAgentManager` | 更新プログラムの検索 | COM WUA API |

---

## 📸 スクリーンショット

### メインメニュー（ロシア語）

![Main Menu RU](docs/screenshots/01-main-menu-ru.png)

### メインメニュー（英語）

![Main Menu EN](docs/screenshots/02-main-menu-en.png)

### 主菜单 (简体中文)

![Main Menu ZH](docs/screenshots/03-main-menu-zh.png)

### システムファイルの整合性チェック

![Integrity Check](docs/screenshots/04-integrity-check.png)

### インターフェース言語の切り替え

![Language Switch](docs/screenshots/05-language-switch.png)

---

## 📥 インストール

### オプション 1: ビルド済み EXE のダウンロード（推奨）

**直接ダウンロードリンク**（最新ビルド、依存関係なし）:

| アーキテクチャ | スタンドアロン（.NET 同梱） | コンパクト（.NET 6 必要） |
|--------------|----------------------------:|--------------------------:|
| **x64** (Intel/AMD) | [standalone-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64.zip) (~40 MB) | [compact-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64-compact.zip) (~7 MB) |
| **x86** (32-bit) | [standalone-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86.zip) (~37 MB) | [compact-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86-compact.zip) (~7 MB) |
| **ARM64** (Surface/Qualcomm) | [standalone-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64.zip) (~33 MB) | [compact-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64-compact.zip) (~6 MB) |

**スタンドアロン版**（推奨）— .NET 6 Runtime を同梱、他には何も不要です。

**コンパクト版** — [.NET Desktop Runtime 6.0+](https://dotnet.microsoft.com/download/dotnet/6.0) が必要です。

### オプション 2: GitHub Releases

すべてのバイナリは [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) ページでも公開されており、整合性検証用の SHA256 チェックサムも同梱されています。

### オプション 3: ソースからビルド

[ソースからビルド](#-building-from-source) を参照してください。

### 要件

- **OS**: Windows 10（ビルド 19041+、May 2020 Update）または Windows 11
- **アーキテクチャ**: x64 / x86 / ARM64
- **権限**: 管理者（マニフェストで既に指定済み）
- **コンパクトビルドの場合**: .NET Desktop Runtime 6.0+

### 整合性の検証

ダウンロード後、SHA256 を検証してください:
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```
[Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1) ページの各アーカイブの横にある `.sha256` ファイルと比較してください。

---

## 🚀 使い方

### インタラクティブモード

`SystemRestoreTool.exe` を管理者として実行すると、67 項目のメニューが開きます:

```
╔══════════════════════════════════════════════════════════════════════╗
║                                                                      ║
║   WINDOWS SYSTEM RECOVERY TOOL  v2.5.1                               ║
║   Direct recovery of Windows 10/11 system files                      ║
║   ...                                                                ║
╚══════════════════════════════════════════════════════════════════════╝
[+] Administrator privileges: CONFIRMED
[i] Log file: C:\Users\...\AppData\Local\SystemRestoreTool\srt_20260628_193015.log

MAIN MENU — System Restore Tool v2.5

  [ 1] ★ SUPER-FULL CYCLE: restore point + DISM + SFC + services + WU + caches
  [ 2]   FULL CYCLE (native API)
  [ 3]   FULL CYCLE (Microsoft.Dism NuGet)
  ...
  [67]   Exit

Select action: _
```

### 自動モード（スクリプト用）

```cmd
:: Super-full cycle: restore point + DISM + SFC + services + WU + caches
SystemRestoreTool.exe --super-full

:: Same + ResetBase WinSxS
SystemRestoreTool.exe --super-full --reset-base

:: Full DISM + SFC cycle
SystemRestoreTool.exe --full

:: Check 40+ critical files
SystemRestoreTool.exe --verify

:: Deep check System32 + drivers
SystemRestoreTool.exe --scan-all
```

### ログ

すべての操作は、コンソール（カラー付き）とファイルに同時に書き込まれます:
```
%LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log
```

### 🌐 多言語UI

このプログラムは **3 つのインターフェース言語** をサポートしています:

| コード | ネイティブ名 | 英語名 |
|------|-------------|--------------|
| `ru` | Русский | Russian |
| `en` | English | English |
| `zh` | 简体中文 | Chinese (Simplified) |

**言語の切り替え:**
1. メインメニューで項目 **64**（"🌐 Switch interface language"）を選択
2. 希望する言語（1-3）を選択
3. 選択は **レジストリに保存** され（`HKCU\SOFTWARE\SystemRestoreTool\Language`）、次回起動時に適用されます

翻訳ファイル: `Resources/strings.{ru,en,zh}.json`。これらは埋め込みリソースとして EXE に組み込まれているため、他にコピーする必要はありません。

**新しい言語の追加:**
1. `Resources/strings.en.json` を `Resources/strings.xx.json` にコピー（`xx` = 言語コード）
2. すべての値を翻訳
3. `Localizer.SupportedLanguages` と `LanguageNames` にコードを追加
4. `.csproj` に `<EmbeddedResource>` としてエントリを追加

---

## 🏗 アーキテクチャ

```
Windows-System-Recovery-Tool/
├── SystemRestoreTool.csproj      .NET 6.0, x64/x86/ARM64, Microsoft.Dism NuGet
├── app.manifest                  requireAdministrator
├── Program.cs                    エントリポイント、67 項目のメニュー
│
├── Api/                          P/Invoke レイヤー（ネイティブ Windows DLL）
│   ├── DismNativeApi.cs          dismapi.dll (DISM API)
│   ├── SfcNativeApi.cs           sfc.dll + sfc_os.dll + srclient.dll
│   ├── WinTrustNativeApi.cs      wintrust.dll + crypt32.dll (署名)
│   ├── Kernel32NativeApi.cs      kernel32.dll + advapi32.dll
│   ├── SrclientNativeApi.cs      srclient.dll (復元ポイント)
│   ├── ServicesNativeApi.cs      advapi32.dll (Service Control Manager)
│   ├── EventLogNativeApi.cs      advapi32.dll + wevtapi.dll
│   ├── VssNativeApi.cs           vssapi.dll (Volume Shadow Copy)
│   ├── WerNativeApi.cs           wer.dll (Windows Error Reporting)
│   ├── SetupApiNativeApi.cs      setupapi.dll (デバイス)
│   ├── PowerNativeApi.cs         powrprof.dll (電源)
│   └── NetNativeApi.cs           winhttp.dll + wininet.dll + ws2_32.dll
│
├── Engine/                       高レベルロジック
│   ├── SystemRestoreEngine.cs    復元サイクルのオーケストレーション
│   ├── DismManagedWrapper.cs     Microsoft.Dism NuGet 経由
│   ├── SignatureVerifier.cs      高レベル署名検証
│   ├── IntegrityChecker.cs       40+ のクリティカル + 全 System32 をチェック
│   └── Modules/                  特化モジュール (14)
│       ├── SystemRestorePointManager.cs
│       ├── ServicesRepairManager.cs
│       ├── BootRecoveryManager.cs
│       ├── RegistryRestoreManager.cs
│       ├── WindowsUpdateRepairManager.cs
│       ├── WinSxsRepairManager.cs
│       ├── EventLogManager.cs
│       ├── UserEnvRestoreManager.cs
│       ├── FileHashDatabaseManager.cs
│       ├── DeviceManager.cs
│       ├── PowerOptionsManager.cs
│       ├── NetworkDiagnosticManager.cs
│       ├── WerManager.cs
│       └── WindowsUpdateAgentManager.cs
│
├── Resources/                    🌐 ローカライズ（埋め込みリソース）
│   ├── strings.ru.json           Русский (デフォルト)
│   ├── strings.en.json           English
│   └── strings.zh.json           简体中文
│
├── Utils/
│   ├── ConsoleHelper.cs          UI ヘルパー (メニュー、権限、入力)
│   ├── Logger.cs                 ファイル + カラーコンソールロガー
│   └── Localizer.cs              言語の読み込みと切り替え
│
├── docs/
│   ├── api-reference.md          詳細な Win32 API リファレンス
│   └── screenshots/              プログラムのスクリーンショット (PNG)
│
├── .github/
│   ├── repo-metadata.json        Topics とリポジトリ説明
│   └── workflows/
│       └── build-release.yml     CI: x86/x64/ARM64 のビルド + Release
│
├── .gitignore                    標準 .NET gitignore
├── .editorconfig                 コードスタイル
├── LICENSE                       MIT
├── CHANGELOG.md                  変更履歴
├── CONTRIBUTING.md               貢献者向けルール
├── SECURITY.md                   セキュリティポリシー
├── README.md                     ロシア語ドキュメント (デフォルト)
├── README.en.md                  英語ドキュメント
└── README.zh.md                  中国語ドキュメント
```

### コード統計

- **~7200 行の C#** コード
- **6 ディレクトリに 37 ファイル**
- **P/Invoke 経由で 17 のネイティブ DLL**
- **14 の復元モジュール**
- **67 のメニュー項目**
- **3 つのインターフェース言語** (RU/EN/ZH)

---

## 🔌 使用する Win32 API

すべての操作は **直接 P/Invoke 呼び出し** を使用します（シェルコマンドなし）:

| DLL | 目的 |
|-----|-----------|
| `dismapi.dll` | DISM: CheckHealth / ScanHealth / RestoreHealth / StartComponentCleanup |
| `sfc.dll` | SfcIsFileProtected / SfcGetNextProtectedFile |
| `sfc_os.dll` | SfcSynchronousScan (= Sfc /ScanNow) |
| `wintrust.dll` | WinVerifyTrust (Authenticode 署名) |
| `crypt32.dll` | CryptQueryObject / CertGetNameString (発行者) |
| `srclient.dll` | SRSetRestorePoint (復元ポイント) |
| `advapi32.dll` | SCM (サービス)、レジストリ、権限 |
| `kernel32.dll` | ファイル、再起動、App Recovery/Restart |
| `wevtapi.dll` | イベントログ (EvtClearLog) |
| `vssapi.dll` | Volume Shadow Copy |
| `wer.dll` | Windows Error Reporting |
| `setupapi.dll` | デバイスインストーラー |
| `cfgmgr32.dll` | CM_Reenumerate_DevNode |
| `powrprof.dll` | 電源プラン、バッテリー |
| `winhttp.dll` | HTTP サーバーのチェック |
| `wininet.dll` | InternetGetConnectedState |
| `ws2_32.dll` | Winsock (WSAStartup) |
| **Microsoft.Dism NuGet** | マネージド DISM API ラッパー |
| **COM WUA API** | Windows Update Agent (`Microsoft.Update.Session`) |

詳細は [docs/api-reference.md](docs/api-reference.md) を参照してください。

---

## 🛠 ソースからビルド

### 要件

- **Windows 10**（ビルド 19041+）または **Windows 11**
- **.NET 6.0 SDK** 以降 — https://dotnet.microsoft.com/download
- (オプション) Visual Studio 2022 / JetBrains Rider / VS Code

### 手順

```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

結果: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 単一ファイル公開

```bash
# コンパクト (ターゲットマシンに .NET 6 が必要、~27 MB)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 自己完結 (依存関係なし、~45 MB)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### サポートされるアーキテクチャ

```bash
# x64 (Intel/AMD 64-bit) — 主要
dotnet publish -c Release -r win-x64 ...

# x86 (32-bit) — 古いシステム向け
dotnet publish -c Release -r win-x86 ...

# ARM64 (Surface Pro X、Snapdragon 搭載ノート PC)
dotnet publish -c Release -r win-arm64 -p:PublishReadyToRun=false ...
```

---

## 📋 チェック対象ファイル一覧

### 40 以上のクリティカルファイル (`IntegrityChecker.CriticalFiles`)

- **基本 DLL**: kernel32.dll、ntdll.dll、user32.dll、advapi32.dll、shell32.dll、ole32.dll、gdi32.dll、msvcrt.dll、ws2_32.dll、wininet.dll、urlmon.dll、combase.dll、sechost.dll、rpcrt4.dll、setupapi.dll
- **ローダーとプロセス**: winload.exe、winresume.exe、winlogon.exe、csrss.exe、services.exe、lsass.exe、lsaiso.exe、smss.exe、svchost.exe、spoolsv.exe、wininit.exe、dwm.exe
- **暗号化**: bcrypt.dll、ncrypt.dll、schannel.dll、crypt32.dll、cryptbase.dll、cryptsp.dll、wintrust.dll、msasn1.dll
- **DISM/SFC**: sfc.dll、sfc_os.dll、dismapi.dll、dism.exe、sfc.exe
- **管理**: mmc.exe、powershell.exe、cmd.exe、regedit.exe、taskmgr.exe、eventvwr.exe
- **シェル**: explorer.exe、shdocvw.dll、shellstyle.dll、themecpl.dll、themeui.dll
- **カーネルドライバ**: tcpip.sys、ntfs.sys、volmgr.sys、volmgrx.sys、disk.sys、partmgr.sys、ACPI.sys、hal.dll、ndis.sys、http.sys、Wdf01000.sys、ksecdd.sys、ksecpkg.sys、msisadrv.sys、pci.sys、mountmgr.sys、fltMgr.sys、luafv.sys、mrxsmb.sys、mup.sys
- **.NET Framework**: clr.dll、mscorlib.dll、System.dll
- **WinRT**: Windows.Foundation.winmd、WinRTTraceLogger.dll
- **WSL**: lxss.dll、wslapi.dll

### ディープチェック (System32 + ドライバ)

- `C:\Windows\System32` 内のすべての `.dll`、`.exe`、`.sys`、`.cpl`
- `C:\Windows\System32\drivers` 内のすべての `.sys`
- UMDF ドライバ: `C:\Windows\System32\drivers\UMDF`
- Windows Defender ドライバ: `C:\Windows\System32\drivers\wd`
- `C:\Windows` 内のすべての `.exe`、`.dll`

### フルチェック

追加: `C:\Windows\SysWOW64` (システムライブラリの 32-bit 版)

---

## 📊 サンプルレポート

```
[i] Files checked:                40
[i] WFP-protected:                38
[i] Without WFP protection:       2
[+] Valid Microsoft signature:    37
[!] Signed by third party:        0
[!] Without signature:            0
[X] With bad signature:           0
[X] Missing:                      1
[i] Check duration:               18.5 sec

All critical files are intact — no tampering detected.
```

---

## 🔒 セキュリティ

- ✅ このプログラムは `cmd.exe`、`dism.exe`、`sfc.exe`、`powershell.exe` を **起動しません** (P/Invoke の同等機能がない操作のみ `bcdedit.exe` と `netsh.exe` を除く)
- ✅ すべての操作はネイティブ Win32 API を使用
- ✅ 管理者権限が必要 (マニフェスト `requireAdministrator`)
- ✅ ログは `%LOCALAPPDATA%\SystemRestoreTool\` に **ローカルに** 書き込まれます
- ✅ ネットワーク経由でデータを送信しません
- ✅ MIT ライセンスのオープンソース — コードを監査できます

## ⚠️ 制限事項

1. **Windows 10/11 のみ** — Windows 7/8 の場合は `TargetFramework` を変更する必要があります
2. **RestoreHealth** には Windows Update またはインストールメディアへのアクセスが必要な場合があります
3. **SfcSynchronousScan** は文書化されていません — 一部のビルドでは追加フラグが必要な場合があります
4. **ResetBase** は不可逆です — 適用後、インストール済みの更新プログラムをアンインストールできません
5. **bcdedit / bootrec** には P/Invoke の同等機能がありません — `Process.Start` で直接呼び出されます (cmd.exe なし)
6. **RegSaveKey** は稼働中のハイブに対しては失敗する場合があります (システムがファイルをロック) — その場合は直接ファイルコピーが使用されます

---

## 📈 ロードマップ

- [ ] GUI 版 (WPF) のグラフィカル実装
- [ ] インターフェースのローカライズ (Deutsch / Français / Español / 日本語)
- [ ] CBS.log 経由での「壊れた」パッケージの自動検出
- [ ] Windows Server 2022/2025 を個別プロファイルとしてサポート
- [ ] タスクスケジューラ (例: 週次の整合性チェック)
- [ ] レポートの HTML/PDF エクスポート

完全なリストは [オープン issues](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) を参照してください。

---

## 🤝 貢献

プルリクエストを歓迎します！ルールは [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

特に必要なもの:
- 🌍 インターフェースの他言語への翻訳
- 🐛 実際のクラッシュログを含むバグ報告
- 📚 様々な Windows ビルドでのエッジケースのドキュメント化
- 🧪 ARM64 デバイスでのテスト (Surface Pro X など)

---

## 📄 ライセンス

MIT ライセンス — [LICENSE](LICENSE) を参照してください。

このコードは自由に使用、改変、配布できます。

---

## 🙏 謝辞

- [Microsoft.Dism NuGet](https://github.com/jeffkl/ManagedDism) — Jeff Kluge によるマネージド DISM API ラッパー
- [Microsoft Learn Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list) — Windows API ドキュメント
- [pinvoke.net](https://www.pinvoke.net/) — P/Invoke シグネチャリファレンス

---

## ⭐ スター履歴

このプログラムが役に立ったなら — リポジトリに ⭐ をお願いします！

<div align="center">

**[⬆ トップへ戻る](#-windows-システム回復ツール)**

Windows パワーユーザーのために ❤️ で作成

</div>
