# Registro de alterações / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **Nota:** Este registro de alterações está em português. Para outros idiomas, consulte as páginas de [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).

Todas as alterações notáveis deste projeto são documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
e o projeto segue [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Planeado
- Versão GUI (WPF) com gráficos em vez de consola
- Suporte para Windows Server 2022/2025 como perfis separados
- Deteção automática de pacotes "corrompidos" via CBS.log
- Localização da interface (Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### Alterado
- **Versão atualizada para 2.5.1 em todos os ficheiros** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Links diretos de download** no README apontam para GitHub Releases v2.5.1 (em vez do ramo releases/v2.5.0)
- **Workflow do GitHub Actions** corrigido:
  - `PublishReadyToRun=false` para ARM64 (anteriormente crossgen2 crashava)
  - `fail-fast: false` — uma arquitetura com falha não cancela as outras
  - `permissions: contents: write` explícitas para criar o lançamento
  - Versões das actions atualizadas: checkout v5, setup-dotnet v5, action-gh-release v3

### Removido
- 4 ramos temporários do Dependabot (limpeza)
- Dependabot para o ecossistema `github-actions` (apenas NuGet permanece) — anteriormente criava PRs com versões inexistentes

### Sem alterações (relativamente a v2.5.0)
- 17 APIs Win32 nativas
- 67 operações de menu
- 14 módulos de recuperação
- 3 idiomas de interface (RU/EN/ZH)
- 5 capturas de ecrã em `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Adicionado
- **🌐 Interface multilingue (3 idiomas)**:
  - Russo (predefinido)
  - English
  - 简体中文 (Chinês simplificado)
  - Ficheiros de tradução: `Resources/strings.{ru,en,zh}.json` (incorporados como recursos embebidos)
  - Classe `Localizer` para gestão de idiomas
  - Escolha guardada no registo (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Item de menu 64 «🌐 Alterar idioma da interface»
  - O banner mostra o idioma atual

- **📸 Capturas de ecrã do programa** em `docs/screenshots/`:
  - `01-main-menu-ru.png` — menu principal (Russo)
  - `02-main-menu-en.png` — Main Menu (Inglês)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — relatório de verificação de integridade
  - `05-language-switch.png` — alteração de idioma

- **5 novas APIs nativas do Windows**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — gestão de dispositivos e controladores
  - `powrprof.dll` — esquemas de energia, bateria, hibernação
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — diagnóstico de rede
  - API COM WUA (`Microsoft.Update.Session`) — pesquisa de atualizações

- **6 novos módulos**:
  - `DeviceManager` — lista de dispositivos, verificação de assinaturas de controladores, deteção de alterações de hardware
  - `PowerOptionsManager` — gestão de esquemas de energia e hibernação
  - `NetworkDiagnosticManager` — verificação de servidores WU, reposição de Winsock/TCP/IP/DNS/Firewall
  - `WerManager` — estatísticas e limpeza de relatórios de erros do Windows
  - `WindowsUpdateAgentManager` — pesquisa de atualizações via API COM

- **Menu ampliado de 44 para 67 itens** (adicionado item de alteração de idioma)
- **Build autónomo** (45 MB) — não requer instalação do .NET
- Suporte para todas as arquiteturas: x86, x64, ARM64
- Compressão de EXE em ficheiro único (`EnableCompressionInSingleFile`)
- **Comandos `--help` / `--version`**
- Mensagens de erro amigáveis com molduras
- **Workflow do GitHub Actions** para builds de lançamento automatizados
- **`docs/api-reference.md`** — referência detalhada da API Win32
- **`SECURITY.md`** e **`.editorconfig`**

### Alterado
- Banner do programa atualizado: mostra todas as 17 APIs usadas + idioma atual
- O logger agora escreve em `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` com carimbo de data/hora
- `SignatureVerifier` extrai corretamente Subject/Issuer da assinatura
- Wrapper NuGet do `Microsoft.Dism` reescrito para a API real 3.2.0
- `app.manifest` atualizado com configurações de DPI

### Corrigido
- `RegSaveKeyW`/`RegRestoreKeyW` agora têm assinaturas corretas
- `DismProgressCallback` aceita `DismProgress` (um parâmetro, não três)
- `BootRecoveryManager` — corrigido o uso da variável `se`

## [2.0.0] — 2026-06-28

### Adicionado
- **Redesenho completo da arquitetura** — estrutura modular
- **9 módulos de recuperação**:
  - `SystemRestorePointManager` — pontos de restauro via `srclient.dll`
  - `ServicesRepairManager` — reinício de 40+ serviços críticos
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — backup de hives do registo
  - `WindowsUpdateRepairManager` — reposição de SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — análise e limpeza do arquivo de componentes
  - `EventLogManager` — backup/limpeza de registos via `wevtapi.dll`
  - `UserEnvRestoreManager` — perfis de utilizador, cache de ícones/fontes
  - `FileHashDatabaseManager` — snapshot SHA256/SHA1 para comparação

- **Verificação de integridade alargada**:
  - 40+ ficheiros críticos (DLL, EXE, controladores)
  - Análise profunda de System32 + drivers (~5000 ficheiros)
  - Análise completa de System32 + SysWOW64 (~10000 ficheiros)

- **Novas APIs nativas**:
  - `kernel32.dll` — ficheiros, privilégios, reinício, módulos
  - `srclient.dll` — pontos de restauro do sistema
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — registos de eventos
  - `vssapi.dll` — Volume Shadow Copy

- **44 itens de menu**
- **Modos automáticos**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Adicionado
- Versão inicial do programa
- API DISM (`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- API SFC (`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- API WinTrust (`wintrust.dll`, `crypt32.dll`): verificação de assinaturas Authenticode
- Pacote NuGet Microsoft.Dism como caminho alternativo
- Menu básico com 15 itens
- Registo em `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
