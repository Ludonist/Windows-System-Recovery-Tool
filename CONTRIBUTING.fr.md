# Contribuer / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# Contribuer à System Restore Tool (Français)

Merci de vouloir contribuer ! 🎉

Ce projet est un outil open source pour la récupération des fichiers système de Windows 10/11 via des appels directs à l'API Win32. Toute contribution est la bienvenue : rapports de bugs, fonctionnalités, traductions, documentation.

## 📋 Table des matières

- [Comment signaler un bug](#-comment-signaler-un-bug)
- [Comment proposer une fonctionnalité](#-comment-proposer-une-fonctionnalité)
- [Configuration de l'environnement de développement](#-configuration-de-lenvironnement-de-développement)
- [Style de code](#-style-de-code)
- [Processus de Pull Request](#-processus-de-pull-request)
- [Règles de sécurité](#-règles-de-sécurité)

## 🐛 Comment signaler un bug

Avant de créer un ticket :
1. Vérifiez les [tickets existants](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — le bug est peut-être déjà connu.
2. Mettez à jour vers la dernière version depuis [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Recueillez les informations de diagnostic.

Dans le ticket, précisez :
- **Version du programme** (depuis la bannière au démarrage)
- **Version de Windows** (Win+R → `winver`)
- **Architecture** (x64 / ARM64)
- **Étapes pour reproduire**
- **Comportement attendu**
- **Comportement observé**
- **Journal du programme** (fichier `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — à joindre au ticket)

## 💡 Comment proposer une fonctionnalité

1. Créez un ticket avec le label `enhancement`.
2. Décrivez le cas d'usage : pourquoi c'est nécessaire, quel problème cela résout.
3. Suggérez l'API/DLL via laquelle cela peut être implémenté (si vous le savez).

## 🛠 Configuration de l'environnement de développement

### Prérequis
- **Windows 10** (build 19041+) ou **Windows 11**
- **.NET 6.0 SDK** ou plus récent — https://dotnet.microsoft.com/download
- (Optionnel) Visual Studio 2022 / Rider / VS Code

### Build
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Résultat : `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Exécution
Le programme nécessite des privilèges d'administrateur (le manifeste le spécifie déjà).
Clic droit → **Exécuter en tant qu'administrateur**.

## 🎨 Style de code

### Règles générales
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 espaces pour l'indentation, **pas de tabulations**
- Longueur de ligne jusqu'à 120 caractères
- `PascalCase` pour les membres publics, `camelCase` pour les variables locales

### Wrappers P/Invoke
- Utilisez `PreserveSig = false` pour les fonctions renvoyant un HRESULT
- Conservez le suffixe `W` (Unicode) dans les noms de fonctions : `CreateFileW`, et non `CreateFile`
- Nommez les constantes dans le style Win32 : `GENERIC_READ`, `OPEN_EXISTING`

### Journalisation
Utilisez le `Logger.Instance` statique :
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### Localisation
Toutes les chaînes de l'interface doivent se trouver dans `Resources/strings.{ru,en,zh}.json` et être accédées via :
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Processus de Pull Request

1. **Forkez** le dépôt
2. Créez une branche : `git checkout -b feature/my-feature`
3. **Commitez** avec un message clair :
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. Poussez : `git push origin feature/my-feature`
5. Ouvrez une PR vers `main` avec une description des changements

### Préfixes de commit
- `feat:` — nouvelle fonctionnalité
- `fix:` — correction de bug
- `docs:` — documentation uniquement
- `refactor:` — refactorisation sans changement de comportement
- `test:` — ajout/correction de tests
- `chore:` — maintenance du projet (dépendances, .gitignore, etc.)
- `i18n:` — changements de localisation

## ⚠️ Règles de sécurité

Cet outil travaille avec des composants système critiques. Par conséquent :

1. **Ne jamais committer de code qui supprime silencieusement des fichiers système** — chaque suppression doit être explicite et journalisée.
2. **Tester dans une machine virtuelle** avant la PR, en particulier si vous modifiez `RegistryRestoreManager`, `BootRecoveryManager` ou `WinSxsRepairManager`.
3. **Ne pas ajouter d'appels** qui ne peuvent pas être annulés (par ex. `FormatEx`, `DeleteVolumeMountPoint`) sans confirmation de l'utilisateur.
4. **Signatures P/Invoke** — vérifiez-les deux fois par rapport à la [doc Win32](https://learn.microsoft.com/windows/win32/api/). Les erreurs de signatures = crash du programme ou corruption de la mémoire.
5. **Clés privées** et identifiants sont interdits dans le dépôt.

## 📜 Licence

En contribuant, vous acceptez que votre code soit publié sous licence [MIT](LICENSE).

## 🙏 Remerciements

Les contributeurs seront listés dans [README.md](README.md).
