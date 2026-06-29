# Changelog / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **Hinweis:** Dieses Changelog ist auf Deutsch. Für andere Sprachen siehe die [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)-Seiten.

Alle nennenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
und das Projekt folgt [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Geplant
- GUI-Version (WPF) mit grafischer Oberfläche statt Konsole
- Unterstützung für Windows Server 2022/2025 als separate Profile
- Automatische Erkennung „kaputter" Pakete über CBS.log
- Lokalisierung der Oberfläche (Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### Geändert
- **Version in allen Dateien auf 2.5.1 aktualisiert** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Direkte Download-Links** in der README verweisen auf GitHub Releases v2.5.1 (statt auf den Branch releases/v2.5.0)
- **GitHub Actions workflow** korrigiert:
  - `PublishReadyToRun=false` für ARM64 (zuvor stürzte crossgen2 ab)
  - `fail-fast: false` — ein fehlschlagendes Architektur-Build bricht die anderen nicht ab
  - Explizite `permissions: contents: write` zum Erstellen des Releases
  - Aktualisierte Action-Versionen: checkout v5, setup-dotnet v5, action-gh-release v3

### Entfernt
- 4 temporäre Dependabot-Branches (Aufräumarbeiten)
- Dependabot für das `github-actions`-Ökosystem (nur NuGet verbleibt) — erstellte zuvor PRs mit nicht existierenden Versionen

### Unverändert (gegenüber v2.5.0)
- 17 native Win32-APIs
- 67 Menüoperationen
- 14 Wiederherstellungsmodule
- 3 Oberflächensprachen (RU/EN/ZH)
- 5 Screenshots in `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Hinzugefügt
- **🌐 Mehrsprachige Oberfläche (3 Sprachen)**:
  - Russisch (Standard)
  - English
  - 简体中文 (Chinesisch, vereinfacht)
  - Übersetzungsdateien: `Resources/strings.{ru,en,zh}.json` (als eingebettete Ressourcen)
  - `Localizer`-Klasse zur Sprachverwaltung
  - Auswahl wird in der Registrierung gespeichert (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Menüpunkt 64 „🌐 Sprache der Oberfläche ändern"
  - Banner zeigt die aktuelle Sprache

- **📸 Programmscreenshots** in `docs/screenshots/`:
  - `01-main-menu-ru.png` — Hauptmenü (Russisch)
  - `02-main-menu-en.png` — Main Menu (Englisch)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — Bericht zur Integritätsprüfung
  - `05-language-switch.png` — Sprachwechsel

- **5 neue native Windows-APIs**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — Geräte- und Treiberverwaltung
  - `powrprof.dll` — Energieschemata, Akku, Ruhezustand
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — Netzwerkdiagnose
  - COM WUA API (`Microsoft.Update.Session`) — Update-Suche

- **6 neue Module**:
  - `DeviceManager` — Geräteliste, Treibersignaturprüfung, Hardware-Änderungen erkennen
  - `PowerOptionsManager` — Verwaltung von Energieschemata und Ruhezustand
  - `NetworkDiagnosticManager` — WU-Server prüfen, Winsock/TCP/IP/DNS/Firewall zurücksetzen
  - `WerManager` — Statistik und Bereinigung von Windows-Fehlerberichten
  - `WindowsUpdateAgentManager` — Update-Suche über die COM-API

- **Menü von 44 auf 67 Einträge erweitert** (Sprachwechsel-Eintrag hinzugefügt)
- **Self-contained-Build** (45 MB) — erfordert keine .NET-Installation
- Unterstützung aller Bit-Breiten: x86, x64, ARM64
- Single-File-EXE-Komprimierung (`EnableCompressionInSingleFile`)
- **`--help` / `--version`**-Befehle
- Freundliche Fehlermeldungen mit Rahmen
- **GitHub Actions workflow** für automatisierte Release-Builds
- **`docs/api-reference.md`** — ausführliche Win32-API-Referenz
- **`SECURITY.md`** und **`.editorconfig`**

### Geändert
- Programm-Banner aktualisiert: zeigt alle 17 verwendeten APIs + aktuelle Sprache
- Logger schreibt nun nach `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` mit Zeitstempel
- `SignatureVerifier` extrahiert Subject/Issuer korrekt aus der Signatur
- `Microsoft.Dism`-NuGet-Wrapper für die tatsächliche 3.2.0-API neu geschrieben
- `app.manifest` mit DPI-Einstellungen aktualisiert

### Behoben
- `RegSaveKeyW`/`RegRestoreKeyW` haben jetzt korrekte Signaturen
- `DismProgressCallback` akzeptiert `DismProgress` (ein Parameter, nicht drei)
- `BootRecoveryManager` — Verwendung der Variable `se` korrigiert

## [2.0.0] — 2026-06-28

### Hinzugefügt
- **Vollständige Neuarchitektur** — modulare Struktur
- **9 Wiederherstellungsmodule**:
  - `SystemRestorePointManager` — Wiederherstellungspunkte über `srclient.dll`
  - `ServicesRepairManager` — Neustart von 40+ kritischen Diensten
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — Backup der Registrierungs-Hives
  - `WindowsUpdateRepairManager` — Zurücksetzen von SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — Analyse und Bereinigung des Komponentenspeichers
  - `EventLogManager` — Backup/Bereinigung von Ereignisprotokollen über `wevtapi.dll`
  - `UserEnvRestoreManager` — Benutzerprofile, Icon-/Font-Cache
  - `FileHashDatabaseManager` — SHA256/SHA1-Snapshot zum Vergleich

- **Erweiterte Integritätsprüfung**:
  - 40+ kritische Dateien (DLL, EXE, Treiber)
  - Tiefenprüfung von System32 + drivers (~5000 Dateien)
  - Vollständige Prüfung von System32 + SysWOW64 (~10000 Dateien)

- **Neue native APIs**:
  - `kernel32.dll` — Dateien, Berechtigungen, Neustart, Module
  - `srclient.dll` — System Restore points
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — Ereignisprotokolle
  - `vssapi.dll` — Volume Shadow Copy

- **44 Menüeinträge**
- **Auto-Modi**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Hinzugefügt
- Erste Version des Programms
- DISM-API (`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- SFC-API (`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- WinTrust-API (`wintrust.dll`, `crypt32.dll`): Authenticode-Signaturprüfung
- Microsoft.Dism-NuGet-Paket als alternativer Weg
- Grundmenü mit 15 Einträgen
- Protokollierung in `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
