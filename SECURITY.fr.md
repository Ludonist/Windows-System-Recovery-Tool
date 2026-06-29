# Politique de sécurité / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# Politique de sécurité (Français)

## Versions prises en charge

| Version | Prise en charge | Mises à jour de sécurité |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ Active | Actuelle |
| 2.0.x  | ⚠️ Critique uniquement | Jusqu'à la sortie de 2.6.0 |
| < 2.0  | ❌ Non     | — |

## Signaler une vulnérabilité

Si vous découvrez une vulnérabilité, **NE créez PAS de ticket public**.

À la place :
1. Envoyez un e-mail à : `security@example.com` (remplacez par votre e-mail)
2. Objet : `[SECURITY] System Restore Tool — <brève description>`

### Ce qu'il faut inclure dans le rapport
- Description de la vulnérabilité et impact potentiel
- Étapes pour reproduire
- Version du programme et version de Windows
- Mesures d'atténuation possibles (si connues)

### SLA
- **Accusé de réception** — sous 48 heures
- **Évaluation initiale** — sous 5 jours ouvrés
- **Correctif ou contournement** — sous 30 jours (selon la gravité)

## Sécurité du programme

### Ce que le programme fait
- ✅ Fonctionne uniquement localement sur votre machine
- ✅ N'envoie pas de données sur le réseau (sauf DISM RestoreHealth, qui utilise Windows Update)
- ✅ Nécessite des privilèges d'administrateur (via le manifeste)
- ✅ Journalise toutes les opérations dans `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Crée un point de restauration avant les opérations critiques

### Ce que le programme NE fait PAS
- ❌ N'envoie pas de données aux serveurs du développeur
- ❌ Ne télécharge pas de binaires supplémentaires (uniquement via DISM/WU)
- ❌ Ne modifie pas le chargeur d'amorçage sans votre consentement
- ❌ Ne supprime pas de fichiers sans confirmation

### Recommandations
1. **Créer un point de restauration** avant utilisation (élément de menu 17)
2. **Ne pas exécuter d'opérations inconnues** — chacune possède une description
3. **Vérifier le journal** une fois le travail terminé
4. **Télécharger l'EXE uniquement depuis [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)** — pas depuis des sources tierces

### Chaîne d'approvisionnement
- Code source — open source sous MIT
- Dépendances : `Microsoft.Dism 3.2.0` (NuGet, signé par Microsoft)
- Build — depuis les sources via `dotnet publish`
- Releases — construites sur GitHub Actions (de manière transparente)
