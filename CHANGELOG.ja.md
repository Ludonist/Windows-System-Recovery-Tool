# 更新履歴 / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **注意:** 本更新履歴は日本語です。他の言語は [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) ページをご覧ください。

このプロジェクトの重要な変更はすべてこのファイルに記録されます。

形式は [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/) に基づいており、
プロジェクトは [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html) に従っています。

## [Unreleased]

### 予定
- コンソールの代わりにグラフィカルな GUI バージョン（WPF）
- Windows Server 2022/2025 を個別のプロファイルとしてサポート
- CBS.log を介した「破損した」パッケージの自動検出
- インターフェースのローカライズ（Deutsch、Français、Español、日本語）

## [2.5.1] — 2026-06-28

### 変更
- **すべてのファイルでバージョンを 2.5.1 に更新**（README、Program.cs、.csproj、app.manifest、Resources/strings.*.json）
- **README の直接ダウンロードリンク**が GitHub Releases v2.5.1 を指すように変更（releases/v2.5.0 ブランチの代わり）
- **GitHub Actions ワークフロー**を修正:
  - ARM64 に対して `PublishReadyToRun=false`（以前 crossgen2 がクラッシュしていた）
  - `fail-fast: false` — 一つのアーキテクチャの失敗が他をキャンセルしない
  - リリース作成のための明示的な `permissions: contents: write`
  - actions のバージョンを更新: checkout v5、setup-dotnet v5、action-gh-release v3

### 削除
- 4 つの一時的な Dependabot ブランチ（クリーンアップ）
- `github-actions` エコシステム向けの Dependabot（NuGet のみ残存）— 以前は存在しないバージョンの PR を作成していた

### 変更なし（v2.5.0 に対して）
- 17 のネイティブ Win32 API
- 67 のメニュー操作
- 14 の回復モジュール
- 3 つのインターフェース言語（RU/EN/ZH）
- `docs/screenshots/` 内の 5 枚のスクリーンショット

## [2.5.0] — 2026-06-28

### 追加
- **🌐 多言語インターフェース（3 言語）**:
  - ロシア語（デフォルト）
  - English
  - 简体中文（中国語 簡体字）
  - 翻訳ファイル: `Resources/strings.{ru,en,zh}.json`（埋め込みリソースとして組み込み）
  - 言語管理用 `Localizer` クラス
  - 選択をレジストリに保存（`HKCU\SOFTWARE\SystemRestoreTool\Language`）
  - メニュー項目 64「🌐 インターフェース言語の変更」
  - バナーが現在の言語を表示

- **📸 プログラムのスクリーンショット** `docs/screenshots/` 内:
  - `01-main-menu-ru.png` — メインメニュー（ロシア語）
  - `02-main-menu-en.png` — Main Menu（英語）
  - `03-main-menu-zh.png` — 主菜单（简体中文）
  - `04-integrity-check.png` — 整合性チェックレポート
  - `05-language-switch.png` — 言語切り替え

- **5 つの新しいネイティブ Windows API**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — デバイスとドライバの管理
  - `powrprof.dll` — 電源スキーム、バッテリー、休止状態
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — ネットワーク診断
  - COM WUA API（`Microsoft.Update.Session`）— 更新プログラムの検索

- **6 つの新しいモジュール**:
  - `DeviceManager` — デバイス一覧、ドライバ署名の検証、ハードウェア変更のスキャン
  - `PowerOptionsManager` — 電源スキームと休止状態の管理
  - `NetworkDiagnosticManager` — WU サーバーの確認、Winsock/TCP/IP/DNS/ファイアウォールのリセット
  - `WerManager` — Windows エラー報告の統計とクリーンアップ
  - `WindowsUpdateAgentManager` — COM API 経由での更新プログラム検索

- **メニューを 44 項目から 67 項目に拡張**（言語切り替え項目を追加）
- **自己完結型ビルド**（45 MB）— .NET のインストール不要
- すべてのビット数をサポート: x86、x64、ARM64
- 単一ファイル EXE の圧縮（`EnableCompressionInSingleFile`）
- **`--help` / `--version`** コマンド
- 枠付きの分かりやすいエラーメッセージ
- リリースビルドを自動化する **GitHub Actions ワークフロー**
- **`docs/api-reference.md`** — 詳細な Win32 API リファレンス
- **`SECURITY.md`** および **`.editorconfig`**

### 変更
- プログラムバナーを更新: 使用中の 17 の API すべて + 現在の言語を表示
- ロガーが `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` にタイムスタンプ付きで書き込み
- `SignatureVerifier` が署名から Subject/Issuer を正しく抽出
- `Microsoft.Dism` NuGet ラッパーを実際の 3.2.0 API 向けに書き直し
- `app.manifest` を DPI 設定で更新

### 修正
- `RegSaveKeyW`/`RegRestoreKeyW` が正しいシグネチャを持つように修正
- `DismProgressCallback` が `DismProgress` を受け取るように（3 つではなく 1 つのパラメータ）
- `BootRecoveryManager` — 変数 `se` の使用を修正

## [2.0.0] — 2026-06-28

### 追加
- **アーキテクチャの全面的な再設計** — モジュラー構造
- **9 つの回復モジュール**:
  - `SystemRestorePointManager` — `srclient.dll` 経由の復元ポイント
  - `ServicesRepairManager` — 40 以上の重要なサービスの再起動
  - `BootRecoveryManager` — BCD、bootmgr、winload
  - `RegistryRestoreManager` — レジストリハイブのバックアップ
  - `WindowsUpdateRepairManager` — SoftwareDistribution/BITS/Catroot2 のリセット
  - `WinSxsRepairManager` — コンポーネントストアの分析とクリーンアップ
  - `EventLogManager` — `wevtapi.dll` 経由のログのバックアップ/クリーンアップ
  - `UserEnvRestoreManager` — ユーザープロファイル、アイコン/フォントキャッシュ
  - `FileHashDatabaseManager` — 比較用 SHA256/SHA1 スナップショット

- **拡張された整合性チェック**:
  - 40 以上の重要なファイル（DLL、EXE、ドライバ）
  - System32 + drivers の詳細スキャン（約 5000 ファイル）
  - System32 + SysWOW64 の完全スキャン（約 10000 ファイル）

- **新しいネイティブ API**:
  - `kernel32.dll` — ファイル、特権、再起動、モジュール
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — イベントログ
  - `vssapi.dll` — Volume Shadow Copy

- **44 のメニュー項目**
- **自動モード**: `--super-full`、`--full`、`--verify`、`--scan-all`

## [1.0.0] — 2026-06-28

### 追加
- プログラムの初期バージョン
- DISM API（`dismapi.dll`）: CheckHealth、ScanHealth、RestoreHealth、StartComponentCleanup
- SFC API（`sfc.dll`、`sfc_os.dll`）: SfcIsFileProtected、SfcGetNextProtectedFile、SfcSynchronousScan
- WinTrust API（`wintrust.dll`、`crypt32.dll`）: Authenticode 署名の検証
- 代替パスとしての Microsoft.Dism NuGet パッケージ
- 15 項目の基本メニュー
- `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` へのログ出力

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
