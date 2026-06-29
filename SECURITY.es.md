# Política de seguridad / Политика безопасности / 安全策略

🌐 [🇷🇺 Русский](SECURITY.md) · [🇬🇧 English](SECURITY.en.md) · [🇨🇳 简体中文](SECURITY.zh.md) · [🇩🇪 Deutsch](SECURITY.de.md) · [🇫🇷 Français](SECURITY.fr.md) · [🇪🇸 Español](SECURITY.es.md) · [🇯🇵 日本語](SECURITY.ja.md) · [🇰🇷 한국어](SECURITY.ko.md) · [🇵🇹 Português](SECURITY.pt.md)

---

# Política de seguridad (Español)

## Versiones compatibles

| Versión | Compatible | Actualizaciones de seguridad |
|--------|-----------|-------------------------|
| 2.5.x  | ✅ Activa | Actual |
| 2.0.x  | ⚠️ Solo críticas | Hasta el lanzamiento de 2.6.0 |
| < 2.0  | ❌ No     | — |

## Reportar una vulnerabilidad

Si descubres una vulnerabilidad, **NO crees un issue público**.

En su lugar:
1. Envía un correo a: `security@example.com` (sustitúyelo por tu correo)
2. Asunto: `[SECURITY] System Restore Tool — <breve descripción>`

### Qué incluir en el reporte
- Descripción de la vulnerabilidad y el impacto potencial
- Pasos para reproducirlo
- Versión del programa y versión de Windows
- Posibles mitigaciones (si se conocen)

### SLA
- **Confirmación de recepción** — en 48 horas
- **Evaluación inicial** — en 5 días laborables
- **Corrección o solución alternativa** — en 30 días (dependiendo de la gravedad)

## Seguridad del programa

### Lo que hace el programa
- ✅ Funciona únicamente de forma local en tu máquina
- ✅ No envía datos por la red (excepto DISM RestoreHealth, que usa Windows Update)
- ✅ Requiere privilegios de administrador (vía manifiesto)
- ✅ Registra todas las operaciones en `%LOCALAPPDATA%\SystemRestoreTool\`
- ✅ Crea un punto de restauración antes de operaciones críticas

### Lo que el programa NO hace
- ❌ No envía datos a servidores del desarrollador
- ❌ No descarga binarios adicionales (solo a través de DISM/WU)
- ❌ No modifica el gestor de arranque sin tu consentimiento
- ❌ No elimina archivos sin confirmación

### Recomendaciones
1. **Crea un punto de restauración** antes de usar (elemento de menú 17)
2. **No ejecutes operaciones desconocidas** — cada una tiene una descripción
3. **Revisa el registro** después de completar el trabajo
4. **Descarga el EXE solo desde [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases)** — no desde fuentes de terceros

### Cadena de suministro
- Código fuente — de código abierto bajo MIT
- Dependencias: `Microsoft.Dism 3.2.0` (NuGet, firmado por Microsoft)
- Build — desde el código fuente vía `dotnet publish`
- Lanzamientos — construidos en GitHub Actions (de forma transparente)
