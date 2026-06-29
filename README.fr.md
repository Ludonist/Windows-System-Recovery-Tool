# 🔧 Outil de Récupération Système Windows

<div align="center">

🌐 **Langues disponibles / Available languages / 可用语言 / Unterstützte Sprachen / Langues disponibles / Idiomas disponibles / 利用可能な言語 / 사용 가능한 언어 / Idiomas disponíveis:**

[🇷🇺 Русский](README.md) · [🇬🇧 English](README.en.md) · [🇨🇳 简体中文](README.zh.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇵🇹 Português](README.pt.md)

🌐 **Documentation avec détection automatique de la langue**: https://ludonist.github.io/Windows-System-Recovery-Tool/

---

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64%20%7C%20ARM64-orange)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Version](https://img.shields.io/badge/version-2.6.0-brightgreen)
![Language](https://img.shields.io/badge/lang-C%23%2010-success)
![Languages](https://img.shields.io/badge/UI%20languages-RU%20%7C%20EN%20%7C%20中文-red)

**Outil professionnel de récupération des fichiers système Windows 10/11 via appels directs à l'API Win32**

🌐 **Interface multilingue** : Русский · English · 简体中文 · Deutsch · Français · Español · 日本語 · 한국어 · Português

[Fonctionnalités](#-features) ·
[Captures d'écran](#-screenshots) ·
[Installation](#-installation) ·
[Utilisation](#-usage) ·
[Architecture](#-architecture) ·
[API](#-win32-apis-used) ·
[Compilation](#-building-from-source) ·
[Contribuer](#-contributing)

</div>

---

## 📖 À propos

**Outil de Récupération Système Windows** est une application console pour Windows 10/11 qui récupère et vérifie l'intégrité des fichiers système via **des appels directs à l'API Windows** (`dismapi.dll`, `sfc.dll`, `wintrust.dll`, etc.) — et non via des commandes externes `cmd.exe` / `dism.exe` / `sfc.exe`.

### Fonctionnalités principales

- 🎯 **67 opérations** de récupération et de diagnostic dans un seul menu
- 🔌 **17 API Windows natives** via P/Invoke (aucune commande shell)
- 🛡️ **Détection de falsification de fichiers** : WinVerifyTrust + éditeur du certificat
- 📊 **Instantanés SHA256/SHA1** pour comparer l'état du système dans le temps
- 🧩 **Microsoft.Dism NuGet** comme alternative gérée
- 📋 **Journalisation détaillée** dans `%LOCALAPPDATA%\SystemRestoreTool\`
- ⚡ **Build autonome** — aucune installation .NET requise
- 🌐 **Interface multilingue** : Русский / English / 简体中文 / Deutsch / Français / Español / 日本語 / 한국어 / Português

### Problèmes résolus par cet outil

| Symptôme | Solution |
|---------|----------|
| `sfc /scannow` détecte une corruption | DISM RestoreHealth + SFC /ScanNow |
| Soupçon de falsification virale de DLL | WinVerifyTrust sur tous les fichiers critiques |
| Windows Update cassé | Réinitialisation de SoftwareDistribution + réenregistrement des DLL |
| Les services ne démarrent pas | Redémarrage de 40+ services critiques via SCM |
| Échec du chargeur d'amorçage | Vérification et récupération BCD |
| Aucun point de restauration | Création via `SRSetRestorePoint` |
| Cache d'icônes/polices corrompu | Reconstruction de IconCache.db / FNTCACHE.DAT |
| Inquiétudes sur l'intégrité du système | Instantané SHA256 + comparaison avec la baseline |

---

## ✨ Fonctionnalités

### 1. Récupération via l'API DISM (`dismapi.dll`)

| Opération | Équivalent console |
|----------|---------------------|
| CheckHealth | `Dism /Online /Cleanup-Image /CheckHealth` |
| ScanHealth | `Dism /Online /Cleanup-Image /ScanHealth` |
| RestoreHealth | `Dism /Online /Cleanup-Image /RestoreHealth` |
| StartComponentCleanup | `Dism /Online /Cleanup-Image /StartComponentCleanup` |
| StartComponentCleanup + ResetBase | `... /StartComponentCleanup /ResetBase` |

### 2. SFC via `sfc_os.dll` natif

| Opération | Équivalent |
|----------|------------|
| SfcSynchronousScan (ScanAndRepair) | `Sfc /ScanNow` |
| SfcSynchronousScan (VerifyOnly) | `Sfc /VerifyOnly` |
| SfcIsFileProtected | (pas d'équivalent) |
| SfcGetNextProtectedFile | (pas d'équivalent) |

### 3. Vérification d'intégrité

- **40+ fichiers critiques** — kernel32.dll, ntdll.dll, winlogon.exe, lsass.exe, csrss.exe, smss.exe, pilotes (tcpip.sys, ntfs.sys, volmgr.sys, ACPI.sys, hal.dll, ...) et plus
- **Vérification approfondie System32 + pilotes** — tous les .dll/.exe/.sys (~5000 fichiers)
- **Vérification complète System32 + SysWOW64** — ~10000 fichiers
- Pour chaque fichier : protection WFP, signature Authenticode, éditeur (Microsoft vs tiers)

### 4. Modules de récupération (14 modules)

| Module | Objectif | API |
|--------|---------|-----|
| `SystemRestorePointManager` | Points de restauration | `srclient.dll!SRSetRestorePoint` |
| `ServicesRepairManager` | Redémarrage de 40+ services | `advapi32.dll` SCM |
| `BootRecoveryManager` | BCD, bootmgr, winload | WMI + bcdedit |
| `RegistryRestoreManager` | Sauvegarde des ruches du Registre | `advapi32.dll!RegSaveKey` |
| `WindowsUpdateRepairManager` | Réinitialisation WU | SCM + File API |
| `WinSxsRepairManager` | Nettoyage WinSxS | `dismapi.dll!DismAnalyzeComponentStore` |
| `EventLogManager` | Journaux d'événements | `wevtapi.dll!EvtClearLog` |
| `UserEnvRestoreManager` | Profils, caches | advapi32 + File API |
| `FileHashDatabaseManager` | Instantanés SHA256 | `System.Security.Cryptography` |
| `DeviceManager` | Périphériques | `setupapi.dll` |
| `PowerOptionsManager` | Schémas d'alimentation | `powrprof.dll` |
| `NetworkDiagnosticManager` | Réseau | `winhttp.dll` + `ws2_32.dll` |
| `WerManager` | Rapports WER | `wer.dll` |
| `WindowsUpdateAgentManager` | Recherche de mises à jour | COM WUA API |

---

## 📸 Captures d'écran

### Menu principal (Russe)

![Main Menu RU](docs/screenshots/01-main-menu-ru.png)

### Menu principal (Anglais)

![Main Menu EN](docs/screenshots/02-main-menu-en.png)

### 主菜单 (简体中文)

![Main Menu ZH](docs/screenshots/03-main-menu-zh.png)

### Vérification de l'intégrité des fichiers système

![Integrity Check](docs/screenshots/04-integrity-check.png)

### Changer la langue de l'interface

![Language Switch](docs/screenshots/05-language-switch.png)

---

## 📥 Installation

### Option 1 : Télécharger l'EXE précompilé (recommandé)

**Liens de téléchargement direct** (build récent, sans dépendances) :

| Architecture | Autonome (inclut .NET) | Compact (.NET 6 requis) |
|--------------|----------------------------:|--------------------------:|
| **x64** (Intel/AMD) | [standalone-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64.zip) (~40 MB) | [compact-win-x64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x64-compact.zip) (~7 MB) |
| **x86** (32-bit) | [standalone-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86.zip) (~37 MB) | [compact-win-x86.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-x86-compact.zip) (~7 MB) |
| **ARM64** (Surface/Qualcomm) | [standalone-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64.zip) (~33 MB) | [compact-win-arm64.zip](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/download/v2.5.1/SystemRestoreTool-standalone-win-arm64-compact.zip) (~6 MB) |

**Version autonome** (recommandée) — inclut .NET 6 Runtime, ne nécessite rien d'autre.

**Version compacte** — nécessite [.NET Desktop Runtime 6.0+](https://dotnet.microsoft.com/download/dotnet/6.0).

### Option 2 : GitHub Releases

Tous les binaires sont également disponibles sur la page [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1), avec les sommes de contrôle SHA256 pour vérifier l'intégrité.

### Option 3 : Compiler depuis les sources

Voir [Compiler depuis les sources](#-building-from-source).

### Prérequis

- **OS** : Windows 10 (build 19041+, May 2020 Update) ou Windows 11
- **Architecture** : x64 / x86 / ARM64
- **Privilèges** : Administrateur (le manifeste le spécifie déjà)
- **Pour le build compact** : .NET Desktop Runtime 6.0+

### Vérifier l'intégrité

Après le téléchargement, vérifiez SHA256 :
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```
Comparez avec les fichiers `.sha256` à côté de chaque archive sur la page [Releases v2.5.1](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1).

---

## 🚀 Utilisation

### Mode interactif

Exécutez `SystemRestoreTool.exe` en tant qu'administrateur. Un menu de 67 entrées s'ouvrira :

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

### Modes automatiques (pour les scripts)

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

### Journalisation

Toutes les opérations sont écrites simultanément dans la console (en couleur) et dans un fichier :
```
%LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log
```

### 🌐 Interface multilingue

Le programme prend en charge **3 langues d'interface** :

| Code | Nom natif | Nom anglais |
|------|-------------|--------------|
| `ru` | Русский | Russian |
| `en` | English | English |
| `zh` | 简体中文 | Chinese (Simplified) |

**Pour changer de langue :**
1. Dans le menu principal, sélectionnez l'élément **64** (« 🌐 Switch interface language »)
2. Choisissez la langue souhaitée (1-3)
3. Le choix est **enregistré dans le Registre** (`HKCU\SOFTWARE\SystemRestoreTool\Language`) et appliqué au prochain lancement

Fichiers de traduction : `Resources/strings.{ru,en,zh}.json`. Ils sont intégrés à l'EXE en tant que ressources embarquées, donc rien d'autre n'a besoin d'être copié.

**Pour ajouter une nouvelle langue :**
1. Copiez `Resources/strings.en.json` vers `Resources/strings.xx.json` (`xx` = code de langue)
2. Traduisez toutes les valeurs
3. Ajoutez le code à `Localizer.SupportedLanguages` et `LanguageNames`
4. Ajoutez une entrée dans `.csproj` comme `<EmbeddedResource>`

---

## 🏗 Architecture

```
Windows-System-Recovery-Tool/
├── SystemRestoreTool.csproj      .NET 6.0, x64/x86/ARM64, Microsoft.Dism NuGet
├── app.manifest                  requireAdministrator
├── Program.cs                    Point d'entrée, menu de 67 entrées
│
├── Api/                          Couche P/Invoke (DLL Windows natives)
│   ├── DismNativeApi.cs          dismapi.dll (API DISM)
│   ├── SfcNativeApi.cs           sfc.dll + sfc_os.dll + srclient.dll
│   ├── WinTrustNativeApi.cs      wintrust.dll + crypt32.dll (signatures)
│   ├── Kernel32NativeApi.cs      kernel32.dll + advapi32.dll
│   ├── SrclientNativeApi.cs      srclient.dll (points de restauration)
│   ├── ServicesNativeApi.cs      advapi32.dll (Service Control Manager)
│   ├── EventLogNativeApi.cs      advapi32.dll + wevtapi.dll
│   ├── VssNativeApi.cs           vssapi.dll (Volume Shadow Copy)
│   ├── WerNativeApi.cs           wer.dll (Windows Error Reporting)
│   ├── SetupApiNativeApi.cs      setupapi.dll (périphériques)
│   ├── PowerNativeApi.cs         powrprof.dll (alimentation)
│   └── NetNativeApi.cs           winhttp.dll + wininet.dll + ws2_32.dll
│
├── Engine/                       Logique de haut niveau
│   ├── SystemRestoreEngine.cs    Orchestration du cycle de récupération
│   ├── DismManagedWrapper.cs     Via Microsoft.Dism NuGet
│   ├── SignatureVerifier.cs      Vérification de signature de haut niveau
│   ├── IntegrityChecker.cs       Vérifie 40+ fichiers critiques + tout System32
│   └── Modules/                  Modules spécialisés (14)
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
├── Resources/                    🌐 Localisation (ressources embarquées)
│   ├── strings.ru.json           Русский (par défaut)
│   ├── strings.en.json           English
│   └── strings.zh.json           简体中文
│
├── Utils/
│   ├── ConsoleHelper.cs          Aide UI (menu, privilèges, saisie)
│   ├── Logger.cs                 Logger fichier + console colorée
│   └── Localizer.cs              Chargement et changement de langue
│
├── docs/
│   ├── api-reference.md          Référence détaillée de l'API Win32
│   └── screenshots/              Captures d'écran du programme (PNG)
│
├── .github/
│   ├── repo-metadata.json        Topics et description du dépôt
│   └── workflows/
│       └── build-release.yml     CI : build x86/x64/ARM64 + Release
│
├── .gitignore                    .gitignore .NET standard
├── .editorconfig                 Style de code
├── LICENSE                       MIT
├── CHANGELOG.md                  Historique des changements
├── CONTRIBUTING.md               Règles pour les contributeurs
├── SECURITY.md                   Politique de sécurité
├── README.md                     Documentation russe (par défaut)
├── README.en.md                  Documentation anglaise
└── README.zh.md                  Documentation chinoise
```

### Statistiques du code

- **~7200 lignes de code C#**
- **37 fichiers** dans 6 répertoires
- **17 DLL natives** via P/Invoke
- **14 modules de récupération**
- **67 éléments de menu**
- **3 langues d'interface** (RU/EN/ZH)

---

## 🔌 API Win32 utilisées

Toutes les opérations utilisent **des appels P/Invoke directs** (aucune commande shell) :

| DLL | Objectif |
|-----|-----------|
| `dismapi.dll` | DISM : CheckHealth / ScanHealth / RestoreHealth / StartComponentCleanup |
| `sfc.dll` | SfcIsFileProtected / SfcGetNextProtectedFile |
| `sfc_os.dll` | SfcSynchronousScan (= Sfc /ScanNow) |
| `wintrust.dll` | WinVerifyTrust (signatures Authenticode) |
| `crypt32.dll` | CryptQueryObject / CertGetNameString (éditeur) |
| `srclient.dll` | SRSetRestorePoint (points de restauration) |
| `advapi32.dll` | SCM (services), registre, privilèges |
| `kernel32.dll` | Fichiers, redémarrage, App Recovery/Restart |
| `wevtapi.dll` | Journaux d'événements (EvtClearLog) |
| `vssapi.dll` | Volume Shadow Copy |
| `wer.dll` | Windows Error Reporting |
| `setupapi.dll` | Programme d'installation de périphériques |
| `cfgmgr32.dll` | CM_Reenumerate_DevNode |
| `powrprof.dll` | Schémas d'alimentation, batterie |
| `winhttp.dll` | Vérifications de serveur HTTP |
| `wininet.dll` | InternetGetConnectedState |
| `ws2_32.dll` | Winsock (WSAStartup) |
| **Microsoft.Dism NuGet** | Wrapper d'API DISM gérée |
| **COM WUA API** | Windows Update Agent (`Microsoft.Update.Session`) |

Détails dans [docs/api-reference.md](docs/api-reference.md).

---

## 🛠 Compiler depuis les sources

### Prérequis

- **Windows 10** (build 19041+) ou **Windows 11**
- **.NET 6.0 SDK** ou plus récent — https://dotnet.microsoft.com/download
- (Optionnel) Visual Studio 2022 / JetBrains Rider / VS Code

### Étapes

```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Résultat : `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Publication en fichier unique

```bash
# Compact (.NET 6 requis sur la machine cible, ~27 MB)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Autonome (aucune dépendance, ~45 MB)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### Architectures prises en charge

```bash
# x64 (Intel/AMD 64-bit) — principal
dotnet publish -c Release -r win-x64 ...

# x86 (32-bit) — pour les systèmes plus anciens
dotnet publish -c Release -r win-x86 ...

# ARM64 (Surface Pro X, ordinateurs Snapdragon)
dotnet publish -c Release -r win-arm64 -p:PublishReadyToRun=false ...
```

---

## 📋 Liste des fichiers vérifiés

### 40+ fichiers critiques (`IntegrityChecker.CriticalFiles`)

- **DLL de base** : kernel32.dll, ntdll.dll, user32.dll, advapi32.dll, shell32.dll, ole32.dll, gdi32.dll, msvcrt.dll, ws2_32.dll, wininet.dll, urlmon.dll, combase.dll, sechost.dll, rpcrt4.dll, setupapi.dll
- **Chargeurs et processus** : winload.exe, winresume.exe, winlogon.exe, csrss.exe, services.exe, lsass.exe, lsaiso.exe, smss.exe, svchost.exe, spoolsv.exe, wininit.exe, dwm.exe
- **Cryptographie** : bcrypt.dll, ncrypt.dll, schannel.dll, crypt32.dll, cryptbase.dll, cryptsp.dll, wintrust.dll, msasn1.dll
- **DISM/SFC** : sfc.dll, sfc_os.dll, dismapi.dll, dism.exe, sfc.exe
- **Gestion** : mmc.exe, powershell.exe, cmd.exe, regedit.exe, taskmgr.exe, eventvwr.exe
- **Shell** : explorer.exe, shdocvw.dll, shellstyle.dll, themecpl.dll, themeui.dll
- **Pilotes noyau** : tcpip.sys, ntfs.sys, volmgr.sys, volmgrx.sys, disk.sys, partmgr.sys, ACPI.sys, hal.dll, ndis.sys, http.sys, Wdf01000.sys, ksecdd.sys, ksecpkg.sys, msisadrv.sys, pci.sys, mountmgr.sys, fltMgr.sys, luafv.sys, mrxsmb.sys, mup.sys
- **.NET Framework** : clr.dll, mscorlib.dll, System.dll
- **WinRT** : Windows.Foundation.winmd, WinRTTraceLogger.dll
- **WSL** : lxss.dll, wslapi.dll

### Vérification approfondie (System32 + pilotes)

- Tous les `.dll`, `.exe`, `.sys`, `.cpl` dans `C:\Windows\System32`
- Tous les `.sys` dans `C:\Windows\System32\drivers`
- Pilotes UMDF : `C:\Windows\System32\drivers\UMDF`
- Pilotes Windows Defender : `C:\Windows\System32\drivers\wd`
- Tous les `.exe`, `.dll` dans `C:\Windows`

### Vérification complète

En plus : `C:\Windows\SysWOW64` (versions 32 bits des bibliothèques système)

---

## 📊 Exemple de rapport

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

## 🔒 Sécurité

- ✅ Le programme **ne lance pas** `cmd.exe`, `dism.exe`, `sfc.exe`, `powershell.exe` (sauf `bcdedit.exe` et `netsh.exe` pour les opérations sans équivalent P/Invoke)
- ✅ Toutes les opérations utilisent les API Win32 natives
- ✅ Privilèges administrateur requis (manifeste `requireAdministrator`)
- ✅ Les journaux sont écrits **localement** dans `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Aucune donnée n'est envoyée sur le réseau
- ✅ Open-source sous licence MIT — vous pouvez auditer le code

## ⚠️ Limitations

1. **Windows 10/11 uniquement** — pour Windows 7/8, il faudrait changer `TargetFramework`
2. **RestoreHealth** peut nécessiter l'accès à Windows Update ou au support d'installation
3. **SfcSynchronousScan** est non documenté — sur certains builds, des indicateurs supplémentaires peuvent être nécessaires
4. **ResetBase** est irréversible — après cela, vous ne pouvez plus désinstaller les mises à jour installées
5. **bcdedit / bootrec** n'ont pas d'équivalents P/Invoke — appelés directement via `Process.Start` (sans cmd.exe)
6. **RegSaveKey** pour les ruches actives peut échouer (fichiers verrouillés par le système) — dans ce cas, une copie directe des fichiers est utilisée

---

## 📈 Feuille de route

- [ ] Version GUI (WPF) avec graphiques
- [ ] Localisation de l'interface (Deutsch / Français / Español / 日本語)
- [ ] Détection automatique des paquets « cassés » via CBS.log
- [ ] Prise en charge de Windows Server 2022/2025 en tant que profils séparés
- [ ] Planificateur de tâches (p. ex. vérification d'intégrité hebdomadaire)
- [ ] Export de rapports en HTML/PDF

Voir les [issues ouverts](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) pour la liste complète.

---

## 🤝 Contribuer

Les pull requests sont les bienvenues ! Voir [CONTRIBUTING.md](CONTRIBUTING.md) pour les règles.

Particulièrement recherché :
- 🌍 Traduction de l'interface dans d'autres langues
- 🐛 Rapports de bugs avec journaux de crash réels
- 📚 Documentation des cas limites sur différents builds Windows
- 🧪 Tests sur appareils ARM64 (Surface Pro X, etc.)

---

## 📄 Licence

Licence MIT — voir [LICENSE](LICENSE).

Vous êtes libre d'utiliser, modifier et distribuer ce code.

---

## 🙏 Remerciements

- [Microsoft.Dism NuGet](https://github.com/jeffkl/ManagedDism) — wrapper d'API DISM gérée par Jeff Kluge
- [Microsoft Learn Win32 API List](https://learn.microsoft.com/windows/win32/apiindex/windows-api-list) — documentation de l'API Windows
- [pinvoke.net](https://www.pinvoke.net/) — référence des signatures P/Invoke

---

## ⭐ Historique des étoiles

Si vous avez trouvé ce programme utile — donnez une ⭐ au dépôt !

<div align="center">

**[⬆ Retour en haut](#-outil-de-récupération-système-windows)**

Fait avec ❤️ pour les utilisateurs avancés de Windows

</div>
