# Sicherheitsrichtlinie / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# Sicherheitsrichtlinie (Deutsch)

## Unterstützte Versionen

| Version | Unterstützt | Sicherheitsupdates |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ Aktiv | Aktuell |
| 2.0.x  | ⚠️ Nur kritische | Bis zum Release von 2.6.0 |
| < 2.0  | ❌ Nein   | — |

## Melden einer Schwachstelle

Wenn Sie eine Schwachstelle entdecken, **erstellen Sie KEIN öffentliches Issue**.

Stattdessen:
1. Senden Sie eine E-Mail an: `security@example.com` (durch Ihre E-Mail-Adresse ersetzen)
2. Betreff: `[SECURITY] System Restore Tool — <kurze Beschreibung>`

### Was der Bericht enthalten sollte
- Beschreibung der Schwachstelle und potenzielle Auswirkungen
- Schritte zur Reproduktion
- Programmversion und Windows-Version
- Mögliche Gegenmaßnahmen (falls bekannt)

### SLA
- **Empfangsbestätigung** — innerhalb von 48 Stunden
- **Ersteinschätzung** — innerhalb von 5 Werktagen
- **Fix oder Workaround** — innerhalb von 30 Tagen (je nach Schweregrad)

## Programmsicherheit

### Was das Programm tut
- ✅ Funktioniert ausschließlich lokal auf Ihrem Rechner
- ✅ Sendet keine Daten über das Netzwerk (außer DISM RestoreHealth, das Windows Update verwendet)
- ✅ Erfordert Administratorrechte (über das Manifest)
- ✅ Protokolliert alle Vorgänge in `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Erstellt vor kritischen Operationen einen Wiederherstellungspunkt

### Was das Programm NICHT tut
- ❌ Sendet keine Daten an Server des Entwicklers
- ❌ Lädt keine zusätzlichen Binärdateien herunter (nur über DISM/WU)
- ❌ Modifiziert den Bootloader nicht ohne Ihre Zustimmung
- ❌ Löscht keine Dateien ohne Bestätigung

### Empfehlungen
1. **Wiederherstellungspunkt erstellen** vor der Nutzung (Menüpunkt 17)
2. **Keine unbekannten Operationen ausführen** — jede hat eine Beschreibung
3. **Protokoll prüfen** nach Abschluss der Arbeiten
4. **EXE nur von [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases) herunterladen** — nicht von Drittanbietern

### Lieferkette
- Quellcode — Open Source unter MIT
- Abhängigkeiten: `Microsoft.Dism 3.2.0` (NuGet, von Microsoft signiert)
- Build — aus dem Quellcode via `dotnet publish`
- Releases — auf GitHub Actions erstellt (transparent)
