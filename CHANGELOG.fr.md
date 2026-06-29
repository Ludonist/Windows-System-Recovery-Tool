# Journal des modifications / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **Note :** Ce journal des modifications est en français. Pour les autres langues, consultez les pages [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).

Tous les changements notables de ce projet sont documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
et le projet suit [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Prévu
- Version GUI (WPF) avec interface graphique au lieu d'une console
- Prise en charge de Windows Server 2022/2025 en tant que profils distincts
- Détection automatique des paquets « cassés » via CBS.log
- Localisation de l'interface (Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### Modifié
- **Version mise à jour à 2.5.1 dans tous les fichiers** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Les liens de téléchargement directs** dans le README pointent vers GitHub Releases v2.5.1 (au lieu de la branche releases/v2.5.0)
- **Workflow GitHub Actions** corrigé :
  - `PublishReadyToRun=false` pour ARM64 (auparavant crossgen2 plantait)
  - `fail-fast: false` — une architecture en échec n'annule pas les autres
  - `permissions: contents: write` explicites pour la création de la release
  - Versions des actions mises à jour : checkout v5, setup-dotnet v5, action-gh-release v3

### Supprimé
- 4 branches Dependabot temporaires (nettoyage)
- Dependabot pour l'écosystème `github-actions` (seul NuGet reste) — créait auparavant des PR avec des versions inexistantes

### Inchangé (par rapport à v2.5.0)
- 17 API Win32 natives
- 67 opérations de menu
- 14 modules de récupération
- 3 langues d'interface (RU/EN/ZH)
- 5 captures d'écran dans `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Ajouté
- **🌐 Interface multilingue (3 langues)** :
  - Russe (par défaut)
  - English
  - 简体中文 (Chinois simplifié)
  - Fichiers de traduction : `Resources/strings.{ru,en,zh}.json` (intégrés en tant que ressources embarquées)
  - Classe `Localizer` pour la gestion des langues
  - Choix conservé dans le registre (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Élément de menu 64 «🌐 Changer la langue de l'interface »
  - La bannière affiche la langue courante

- **📸 Captures d'écran du programme** dans `docs/screenshots/` :
  - `01-main-menu-ru.png` — menu principal (Russe)
  - `02-main-menu-en.png` — Main Menu (Anglais)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — rapport de vérification d'intégrité
  - `05-language-switch.png` — changement de langue

- **5 nouvelles API Windows natives** :
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — gestion des périphériques et des pilotes
  - `powrprof.dll` — schémas d'alimentation, batterie, mise en veille prolongée
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — diagnostics réseau
  - API COM WUA (`Microsoft.Update.Session`) — recherche de mises à jour

- **6 nouveaux modules** :
  - `DeviceManager` — liste des périphériques, vérification des signatures de pilotes, scan des modifications matérielles
  - `PowerOptionsManager` — gestion des schémas d'alimentation et de la mise en veille prolongée
  - `NetworkDiagnosticManager` — vérification des serveurs WU, réinitialisation Winsock/TCP/IP/DNS/Pare-feu
  - `WerManager` — statistiques et nettoyage des rapports d'erreurs Windows
  - `WindowsUpdateAgentManager` — recherche de mises à jour via l'API COM

- **Menu étendu de 44 à 67 éléments** (ajout de l'élément de changement de langue)
- **Build autonome** (45 Mo) — ne nécessite pas l'installation de .NET
- Prise en charge de toutes les architectures : x86, x64, ARM64
- Compression EXE en fichier unique (`EnableCompressionInSingleFile`)
- **Commandes `--help` / `--version`**
- Messages d'erreur conviviaux avec encadrement
- **Workflow GitHub Actions** pour les builds de release automatisés
- **`docs/api-reference.md`** — référence détaillée de l'API Win32
- **`SECURITY.md`** et **`.editorconfig`**

### Modifié
- Bannière du programme mise à jour : affiche les 17 API utilisées + langue courante
- Le logger écrit désormais dans `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` avec un horodatage
- `SignatureVerifier` extrait correctement Subject/Issuer de la signature
- Wrapper NuGet `Microsoft.Dism` réécrit pour l'API réelle 3.2.0
- `app.manifest` mis à jour avec les paramètres DPI

### Corrigé
- `RegSaveKeyW`/`RegRestoreKeyW` ont désormais des signatures correctes
- `DismProgressCallback` accepte `DismProgress` (un paramètre, pas trois)
- `BootRecoveryManager` — utilisation de la variable `se` corrigée

## [2.0.0] — 2026-06-28

### Ajouté
- **Refonte complète de l'architecture** — structure modulaire
- **9 modules de récupération** :
  - `SystemRestorePointManager` — points de restauration via `srclient.dll`
  - `ServicesRepairManager` — redémarrage de 40+ services critiques
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — sauvegarde des ruches du registre
  - `WindowsUpdateRepairManager` — réinitialisation de SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — analyse et nettoyage du magasin de composants
  - `EventLogManager` — sauvegarde/nettoyage des journaux via `wevtapi.dll`
  - `UserEnvRestoreManager` — profils utilisateur, cache d'icônes/polices
  - `FileHashDatabaseManager` — instantané SHA256/SHA1 pour comparaison

- **Vérification d'intégrité étendue** :
  - 40+ fichiers critiques (DLL, EXE, pilotes)
  - Analyse approfondie de System32 + drivers (~5000 fichiers)
  - Analyse complète de System32 + SysWOW64 (~10000 fichiers)

- **Nouvelles API natives** :
  - `kernel32.dll` — fichiers, privilèges, redémarrage, modules
  - `srclient.dll` — points de restauration système
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — journaux d'événements
  - `vssapi.dll` — Volume Shadow Copy

- **44 éléments de menu**
- **Modes automatiques** : `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Ajouté
- Version initiale du programme
- API DISM (`dismapi.dll`) : CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- API SFC (`sfc.dll`, `sfc_os.dll`) : SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- API WinTrust (`wintrust.dll`, `crypt32.dll`) : vérification des signatures Authenticode
- Paquet NuGet Microsoft.Dism comme alternative
- Menu de base avec 15 éléments
- Journalisation dans `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
