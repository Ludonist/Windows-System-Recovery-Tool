# Mitwirken / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# Zu System Restore Tool beitragen (Deutsch)

Vielen Dank, dass Sie beitragen möchten! 🎉

Dieses Projekt ist ein Open-Source-Tool zur Wiederherstellung von Windows 10/11-Systemdateien über direkte Win32-API-Aufrufe. Jeder Beitrag ist willkommen: Bug-Reports, Features, Übersetzungen, Dokumentation.

## 📋 Inhaltsverzeichnis

- [Wie melde ich einen Bug](#-wie-melde-ich-einen-bug)
- [Wie schlage ich ein Feature vor](#-wie-schlage-ich-ein-feature-vor)
- [Einrichtung der Entwicklungsumgebung](#-einrichtung-der-entwicklungsumgebung)
- [Code-Stil](#-code-stil)
- [Pull-Request-Prozess](#-pull-request-prozess)
- [Sicherheitsregeln](#-sicherheitsregeln)

## 🐛 Wie melde ich einen Bug

Bevor Sie ein Issue erstellen:
1. Prüfen Sie die [vorhandenen Issues](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — der Bug könnte bereits bekannt sein.
2. Aktualisieren Sie auf die neueste Version aus [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Sammeln Sie Diagnoseinformationen.

Geben Sie im Issue Folgendes an:
- **Programmversion** (aus dem Banner beim Start)
- **Windows-Version** (Win+R → `winver`)
- **Architektur** (x64 / ARM64)
- **Schritte zur Reproduktion**
- **Erwartetes Verhalten**
- **Tatsächliches Verhalten**
- **Programmprotokoll** (Datei `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — an das Issue anhängen)

## 💡 Wie schlage ich ein Feature vor

1. Erstellen Sie ein Issue mit dem Label `enhancement`.
2. Beschreiben Sie den Anwendungsfall: warum es gebraucht wird, welches Problem es löst.
3. Schlagen Sie die API/DLL vor, über die dies umgesetzt werden kann (falls bekannt).

## 🛠 Einrichtung der Entwicklungsumgebung

### Voraussetzungen
- **Windows 10** (Build 19041+) oder **Windows 11**
- **.NET 6.0 SDK** oder neuer — https://dotnet.microsoft.com/download
- (Optional) Visual Studio 2022 / Rider / VS Code

### Build
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Ergebnis: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Ausführen
Das Programm erfordert Administratorrechte (das Manifest legt dies bereits fest).
Rechtsklick → **Als Administrator ausführen**.

## 🎨 Code-Stil

### Allgemeine Regeln
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 Leerzeichen für Einrückung, **keine Tabs**
- Zeilenlänge bis zu 120 Zeichen
- `PascalCase` für öffentliche Member, `camelCase` für lokale Variablen

### P/Invoke-Wrapper
- Verwenden Sie `PreserveSig = false` für Funktionen, die ein HRESULT zurückgeben
- Behalten Sie das `W`-Suffix (Unicode) in Funktionsnamen: `CreateFileW`, nicht `CreateFile`
- Benennen Sie Konstanten im Win32-Stil: `GENERIC_READ`, `OPEN_EXISTING`

### Protokollierung
Verwenden Sie die statische `Logger.Instance`:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### Lokalisierung
Alle UI-Strings müssen in `Resources/strings.{ru,en,zh}.json` stehen und über:
```csharp
Utils.Localizer.S("section.key")
```
abgerufen werden.

## 🔄 Pull-Request-Prozess

1. **Fork** des Repositorys
2. Branch erstellen: `git checkout -b feature/my-feature`
3. **Commit** mit einer klaren Nachricht:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. Push: `git push origin feature/my-feature`
5. PR an `main` mit einer Beschreibung der Änderungen öffnen

### Commit-Präfixe
- `feat:` — neue Funktionalität
- `fix:` — Bug-Fix
- `docs:` — nur Dokumentation
- `refactor:` — Refactoring ohne Verhaltensänderung
- `test:` — Tests hinzufügen/korrigieren
- `chore:` — Projektverwaltung (Abhängigkeiten, .gitignore usw.)
- `i18n:` — Lokalisierungsänderungen

## ⚠️ Sicherheitsregeln

Dieses Tool arbeitet mit kritischen Systemkomponenten. Daher:

1. ** Niemals Code committen, der Systemdateien stillschweigend löscht** — jede Löschung muss explizit und protokolliert sein.
2. **In einer virtuellen Maschine testen** vor dem PR, besonders bei Änderungen an `RegistryRestoreManager`, `BootRecoveryManager` oder `WinSxsRepairManager`.
3. **Keine Aufrufe hinzufügen**, die nicht rückgängig zu machen sind (z. B. `FormatEx`, `DeleteVolumeMountPoint`) ohne Benutzerbestätigung.
4. **P/Invoke-Signaturen** — gegen die [Win32-Dokumentation](https://learn.microsoft.com/windows/win32/api/) doppelt prüfen. Fehler in Signaturen = Programmabsturz oder Speicherbeschädigung.
5. **Private Schlüssel** und jegliche Zugangsdaten sind im Repository verboten.

## 📜 Lizenz

Durch Ihren Beitrag stimmen Sie zu, dass Ihr Code unter der [MIT](LICENSE)-Lizenz veröffentlicht wird.

## 🙏 Danksagung

Mitwirkende werden in [README.md](README.md) aufgeführt.
