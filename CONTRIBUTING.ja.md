# 貢献 / Участие / Contributing

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# System Restore Tool への貢献（日本語）

貢献をご検討いただきありがとうございます！🎉

本プロジェクトは、Win32 API を直接呼び出すことで Windows 10/11 のシステムファイルを回復するオープンソースツールです。バグ報告、機能、翻訳、ドキュメントなど、あらゆる貢献を歓迎します。

## 📋 目次

- [バグの報告方法](#-バグの報告方法)
- [機能提案の方法](#-機能提案の方法)
- [開発環境のセットアップ](#-開発環境のセットアップ)
- [コードスタイル](#-コードスタイル)
- [Pull Request のプロセス](#-pull-request-のプロセス)
- [安全ルール](#-安全ルール)

## 🐛 バグの報告方法

issue を作成する前に:
1. [既存の issue](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) を確認 — バグが既知の可能性があります。
2. [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) から最新バージョンに更新してください。
3. 診断情報を収集してください。

issue には以下を明記してください:
- **プログラムのバージョン**（起動時のバナーより）
- **Windows のバージョン**（Win+R → `winver`）
- **アーキテクチャ**（x64 / ARM64）
- **再現手順**
- **期待される動作**
- **実際の動作**
- **プログラムログ**（ファイル `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — issue に添付）

## 💡 機能提案の方法

1. `enhancement` ラベルを付けて issue を作成してください。
2. ユースケースを説明してください: なぜ必要か、どのような問題を解決するか。
3. 実装に使用できる API/DLL を提案してください（分かる場合）。

## 🛠 開発環境のセットアップ

### 要件
- **Windows 10**（build 19041+）または **Windows 11**
- **.NET 6.0 SDK** 以上 — https://dotnet.microsoft.com/download
- （任意）Visual Studio 2022 / Rider / VS Code

### ビルド
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

結果: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### 実行
プログラムには管理者権限が必要です（マニフェストで既に指定されています）。
右クリック → **管理者として実行**。

## 🎨 コードスタイル

### 一般ルール
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- インデントには 4 つのスペース、**タブは使用しない**
- 1 行の長さは最大 120 文字
- 公開メンバーには `PascalCase`、ローカル変数には `camelCase`

### P/Invoke ラッパー
- HRESULT を返す関数には `PreserveSig = false` を使用
- 関数名に `W` 接尾辞（Unicode）を維持: `CreateFile` ではなく `CreateFileW`
- 定数は Win32 スタイルで命名: `GENERIC_READ`, `OPEN_EXISTING`

### ログ記録
静的な `Logger.Instance` を使用:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### ローカライズ
すべての UI 文字列は `Resources/strings.{ru,en,zh}.json` に配置し、以下でアクセス:
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Pull Request のプロセス

1. リポジトリを **フォーク**
2. ブランチを作成: `git checkout -b feature/my-feature`
3. 明確なメッセージを付けて **コミット**:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. プッシュ: `git push origin feature/my-feature`
5. 変更内容の説明と共に `main` へ PR を作成

### コミットプレフィックス
- `feat:` — 新機能
- `fix:` — バグ修正
- `docs:` — ドキュメントのみ
- `refactor:` — 動作変更のないリファクタリング
- `test:` — テストの追加/修正
- `chore:` — プロジェクト管理（依存関係、.gitignore など）
- `i18n:` — ローカライズ変更

## ⚠️ 安全ルール

このツールは重要なシステムコンポーネントを操作します。したがって:

1. **システムファイルを暗黙的に削除するコードを決してコミットしない** — すべての削除は明示的で、ログに記録される必要があります。
2. **PR の前に仮想マシンでテスト** — 特に `RegistryRestoreManager`、`BootRecoveryManager`、`WinSxsRepairManager` を変更する場合。
3. **元に戻せない呼び出し**（例: `FormatEx`、`DeleteVolumeMountPoint`）をユーザー確認なしに追加しない。
4. **P/Invoke シグネチャ** — [Win32 ドキュメント](https://learn.microsoft.com/windows/win32/api/) と照合して再確認。シグネチャのエラー = プログラムのクラッシュまたはメモリ破損。
5. **秘密鍵**およびあらゆる認証情報はリポジトリに含めることを禁じます。

## 📜 ライセンス

貢献することで、あなたのコードが [MIT](LICENSE) ライセンスで公開されることに同意したことになります。

## 🙏 謝辞

貢献者は [README.md](README.md) に記載されます。
