# Security Policy / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md)

---

# Security Policy (English)

## Supported versions

| Version | Supported | Security updates |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ Active | Current |
| 2.0.x  | ⚠️ Critical only | Until 2.6.0 release |
| < 2.0  | ❌ No     | — |

## Reporting a vulnerability

If you discover a vulnerability, **DO NOT create a public issue**.

Instead:
1. Send email to: `security@example.com` (replace with your email)
2. Subject: `[SECURITY] System Restore Tool — <brief description>`

### What to include in the report
- Description of the vulnerability and potential impact
- Steps to reproduce
- Program version and Windows version
- Possible mitigations (if known)

### SLA
- **Acknowledgment** — within 48 hours
- **Initial assessment** — within 5 business days
- **Fix or workaround** — within 30 days (depending on severity)

## Program security

### What the program does
- ✅ Works only locally on your machine
- ✅ Does not send data over the network (except DISM RestoreHealth, which uses Windows Update)
- ✅ Requires administrator privileges (via manifest)
- ✅ Logs all operations to `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Creates a restore point before critical operations

### What the program does NOT do
- ❌ Does not send data to developer servers
- ❌ Does not download additional binaries (only through DISM/WU)
- ❌ Does not modify the bootloader without your consent
- ❌ Does not delete files without confirmation

### Recommendations
1. **Create a restore point** before using (menu item 17)
2. **Don't run unfamiliar operations** — each has a description
3. **Check the log** after work is complete
4. **Download EXE only from [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)** — not from third-party sources

### Supply chain
- Source code — open-source under MIT
- Dependencies: `Microsoft.Dism 3.2.0` (NuGet, signed by Microsoft)
- Build — from source via `dotnet publish`
- Releases — built on GitHub Actions (transparently)
