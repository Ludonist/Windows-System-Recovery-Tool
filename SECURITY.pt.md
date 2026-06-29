# Política de segurança / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# Política de segurança (Português)

## Versões suportadas

| Versão | Suportada | Atualizações de segurança |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ Ativa | Atual |
| 2.0.x  | ⚠️ Apenas críticas | Até ao lançamento da 2.6.0 |
| < 2.0  | ❌ Não     | — |

## Reportar uma vulnerabilidade

Se descobrir uma vulnerabilidade, **NÃO crie um issue público**.

Em vez disso:
1. Envie um e-mail para: `security@example.com` (substitua pelo seu e-mail)
2. Assunto: `[SECURITY] System Restore Tool — <breve descrição>`

### O que incluir no relatório
- Descrição da vulnerabilidade e impacto potencial
- Passos para reproduzir
- Versão do programa e versão do Windows
- Possíveis mitigações (se conhecidas)

### SLA
- **Confirmação de receção** — em até 48 horas
- **Avaliação inicial** — em até 5 dias úteis
- **Correção ou solução alternativa** — em até 30 dias (dependendo da gravidade)

## Segurança do programa

### O que o programa faz
- ✅ Funciona apenas localmente na sua máquina
- ✅ Não envia dados pela rede (exceto DISM RestoreHealth, que usa o Windows Update)
- ✅ Requer privilégios de administrador (via manifesto)
- ✅ Regista todas as operações em `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Cria um ponto de restauro antes de operações críticas

### O que o programa NÃO faz
- ❌ Não envia dados para servidores do programador
- ❌ Não descarrega binários adicionais (apenas através do DISM/WU)
- ❌ Não modifica o bootloader sem o seu consentimento
- ❌ Não elimina ficheiros sem confirmação

### Recomendações
1. **Crie um ponto de restauro** antes de utilizar (item de menu 17)
2. **Não execute operações desconhecidas** — cada uma tem uma descrição
3. **Verifique o registo** após a conclusão do trabalho
4. **Descarregue o EXE apenas de [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)** — não de fontes de terceiros

### Cadeia de fornecimento
- Código-fonte — open source sob MIT
- Dependências: `Microsoft.Dism 3.2.0` (NuGet, assinado pela Microsoft)
- Build — a partir do código-fonte via `dotnet publish`
- Lançamentos — construídos no GitHub Actions (de forma transparente)
