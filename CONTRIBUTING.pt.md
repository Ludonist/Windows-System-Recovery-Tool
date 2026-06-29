# Contribuir / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# Contribuir para o System Restore Tool (Português)

Obrigado por querer contribuir! 🎉

Este projeto é uma ferramenta de código aberto para recuperar ficheiros do sistema do Windows 10/11 através de chamadas diretas à API Win32. Qualquer contribuição é bem-vinda: relatórios de bugs, funcionalidades, traduções, documentação.

## 📋 Índice

- [Como reportar um bug](#-como-reportar-um-bug)
- [Como propor uma funcionalidade](#-como-propor-uma-funcionalidade)
- [Configuração do ambiente de desenvolvimento](#-configuração-do-ambiente-de-desenvolvimento)
- [Estilo de código](#-estilo-de-código)
- [Processo de Pull Request](#-processo-de-pull-request)
- [Regras de segurança](#-regras-de-segurança)

## 🐛 Como reportar um bug

Antes de criar um issue:
1. Verifique os [issues existentes](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — o bug pode já ser conhecido.
2. Atualize para a versão mais recente em [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Recolha informações de diagnóstico.

No issue, especifique:
- **Versão do programa** (do banner no arranque)
- **Versão do Windows** (Win+R → `winver`)
- **Arquitetura** (x64 / ARM64)
- **Passos para reproduzir**
- **Comportamento esperado**
- **Comportamento real**
- **Registo do programa** (ficheiro `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — anexar ao issue)

## 💡 Como propor uma funcionalidade

1. Crie um issue com a etiqueta `enhancement`.
2. Descreva o caso de uso: por que é necessário, que problema resolve.
3. Sugira a API/DLL através da qual isto pode ser implementado (se souber).

## 🛠 Configuração do ambiente de desenvolvimento

### Requisitos
- **Windows 10** (build 19041+) ou **Windows 11**
- **.NET 6.0 SDK** ou mais recente — https://dotnet.microsoft.com/download
- (Opcional) Visual Studio 2022 / Rider / VS Code

### Build
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Resultado: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Execução
O programa requer privilégios de administrador (o manifesto já o especifica).
Clique direito → **Executar como administrador**.

## 🎨 Estilo de código

### Regras gerais
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 espaços para indentação, **sem tabulações**
- Comprimento de linha até 120 caracteres
- `PascalCase` para membros públicos, `camelCase` para variáveis locais

### Wrappers P/Invoke
- Use `PreserveSig = false` para funções que devolvem HRESULT
- Mantenha nomes de funções com sufixo `W` (Unicode): `CreateFileW`, não `CreateFile`
- Nomeie constantes em estilo Win32: `GENERIC_READ`, `OPEN_EXISTING`

### Registo
Use o `Logger.Instance` estático:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### Localização
Todas as strings de UI devem estar em `Resources/strings.{ru,en,zh}.json` e acedidas via:
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Processo de Pull Request

1. **Fork** o repositório
2. Crie um ramo: `git checkout -b feature/my-feature`
3. **Commit** com uma mensagem clara:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. Push: `git push origin feature/my-feature`
5. Abra um PR para `main` com uma descrição das alterações

### Prefixos de commit
- `feat:` — nova funcionalidade
- `fix:` — correção de bug
- `docs:` — apenas documentação
- `refactor:` — refatoração sem alteração de comportamento
- `test:` — adicionar/corrigir testes
- `chore:` — manutenção do projeto (dependências, .gitignore, etc.)
- `i18n:` — alterações de localização

## ⚠️ Regras de segurança

Esta ferramenta trabalha com componentes críticos do sistema. Por isso:

1. **Nunca faça commit de código que elimine ficheiros do sistema silenciosamente** — cada eliminação deve ser explícita e registada.
2. **Teste numa máquina virtual** antes do PR, especialmente se alterar `RegistryRestoreManager`, `BootRecoveryManager` ou `WinSxsRepairManager`.
3. **Não adicione chamadas** que não possam ser desfeitas (ex.: `FormatEx`, `DeleteVolumeMountPoint`) sem confirmação do utilizador.
4. **Assinaturas P/Invoke** — verifique duas vezes contra a [documentação Win32](https://learn.microsoft.com/windows/win32/api/). Erros nas assinaturas = crash do programa ou corrupção de memória.
5. **Chaves privadas** e quaisquer credenciais são proibidos no repositório.

## 📜 Licença

Ao contribuir, concorda que o seu código será publicado sob a licença [MIT](LICENSE).

## 🙏 Agradecimentos

Os colaboradores serão listados em [README.md](README.md).
