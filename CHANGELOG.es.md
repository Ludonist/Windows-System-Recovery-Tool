# Registro de cambios / История изменений / 更新日志

🌐 [🇷🇺 Русский](CHANGELOG.md) · [🇬🇧 English](CHANGELOG.en.md) · [🇨🇳 简体中文](CHANGELOG.zh.md) · [🇩🇪 Deutsch](CHANGELOG.de.md) · [🇫🇷 Français](CHANGELOG.fr.md) · [🇪🇸 Español](CHANGELOG.es.md) · [🇯🇵 日本語](CHANGELOG.ja.md) · [🇰🇷 한국어](CHANGELOG.ko.md) · [🇵🇹 Português](CHANGELOG.pt.md)

> 🌐 **Nota:** Este registro de cambios está en español. Para otros idiomas, consulta las páginas de [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
y el proyecto sigue [Semantic Versioning](https://semver.org/lang/ru/spec/v2.0.0.html).

## [Unreleased]

### Planificado
- Versión GUI (WPF) con gráficos en lugar de consola
- Soporte para Windows Server 2022/2025 como perfiles independientes
- Detección automática de paquetes «rotos» a través de CBS.log
- Localización de la interfaz (Deutsch, Français, Español, 日本語)

## [2.5.1] — 2026-06-28

### Cambiado
- **Versión actualizada a 2.5.1 en todos los archivos** (README, Program.cs, .csproj, app.manifest, Resources/strings.*.json)
- **Los enlaces directos de descarga** en el README apuntan a GitHub Releases v2.5.1 (en lugar de la rama releases/v2.5.0)
- **Workflow de GitHub Actions** corregido:
  - `PublishReadyToRun=false` para ARM64 (antes crossgen2 fallaba)
  - `fail-fast: false` — una arquitectura con error no cancela las demás
  - `permissions: contents: write` explícitos para crear el lanzamiento
  - Versiones de actions actualizadas: checkout v5, setup-dotnet v5, action-gh-release v3

### Eliminado
- 4 ramas temporales de Dependabot (limpieza)
- Dependabot para el ecosistema `github-actions` (solo permanece NuGet) — antes creaba PRs con versiones inexistentes

### Sin cambios (respecto a v2.5.0)
- 17 API Win32 nativas
- 67 operaciones de menú
- 14 módulos de recuperación
- 3 idiomas de interfaz (RU/EN/ZH)
- 5 capturas de pantalla en `docs/screenshots/`

## [2.5.0] — 2026-06-28

### Añadido
- **🌐 Interfaz multilingüe (3 idiomas)**:
  - Ruso (por defecto)
  - English
  - 简体中文 (Chino simplificado)
  - Archivos de traducción: `Resources/strings.{ru,en,zh}.json` (integrados como recursos incrustados)
  - Clase `Localizer` para la gestión de idiomas
  - Selección guardada en el registro (`HKCU\SOFTWARE\SystemRestoreTool\Language`)
  - Elemento de menú 64 «🌐 Cambiar idioma de la interfaz»
  - El banner muestra el idioma actual

- **📸 Capturas de pantalla del programa** en `docs/screenshots/`:
  - `01-main-menu-ru.png` — menú principal (Ruso)
  - `02-main-menu-en.png` — Main Menu (Inglés)
  - `03-main-menu-zh.png` — 主菜单 (简体中文)
  - `04-integrity-check.png` — informe de verificación de integridad
  - `05-language-switch.png` — cambio de idioma

- **5 nuevas API nativas de Windows**:
  - `wer.dll` — Windows Error Reporting + Application Recovery/Restart
  - `setupapi.dll` + `cfgmgr32.dll` — gestión de dispositivos y controladores
  - `powrprof.dll` — esquemas de energía, batería, hibernación
  - `winhttp.dll` + `wininet.dll` + `ws2_32.dll` — diagnóstico de red
  - API COM WUA (`Microsoft.Update.Session`) — búsqueda de actualizaciones

- **6 nuevos módulos**:
  - `DeviceManager` — lista de dispositivos, verificación de firmas de controladores, escaneo de cambios de hardware
  - `PowerOptionsManager` — gestión de esquemas de energía e hibernación
  - `NetworkDiagnosticManager` — verificación de servidores WU, reinicio de Winsock/TCP/IP/DNS/Firewall
  - `WerManager` — estadísticas y limpieza de informes de errores de Windows
  - `WindowsUpdateAgentManager` — búsqueda de actualizaciones a través de la API COM

- **Menú ampliado de 44 a 67 elementos** (añadido el elemento de cambio de idioma)
- **Build autocontenido** (45 MB) — no requiere instalación de .NET
- Soporte para todas las arquitecturas: x86, x64, ARM64
- Compresión de EXE de archivo único (`EnableCompressionInSingleFile`)
- **Comandos `--help` / `--version`**
- Mensajes de error amigables con marcos
- **Workflow de GitHub Actions** para builds de lanzamiento automatizados
- **`docs/api-reference.md`** — referencia detallada de la API Win32
- **`SECURITY.md`** y **`.editorconfig`**

### Cambiado
- Banner del programa actualizado: muestra las 17 API usadas + idioma actual
- El logger ahora escribe en `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` con marca de tiempo
- `SignatureVerifier` extrae correctamente Subject/Issuer de la firma
- Wrapper NuGet de `Microsoft.Dism` reescrito para la API real 3.2.0
- `app.manifest` actualizado con configuración de DPI

### Corregido
- `RegSaveKeyW`/`RegRestoreKeyW` ahora tienen firmas correctas
- `DismProgressCallback` acepta `DismProgress` (un parámetro, no tres)
- `BootRecoveryManager` — corregido el uso de la variable `se`

## [2.0.0] — 2026-06-28

### Añadido
- **Rediseño completo de la arquitectura** — estructura modular
- **9 módulos de recuperación**:
  - `SystemRestorePointManager` — puntos de restauración a través de `srclient.dll`
  - `ServicesRepairManager` — reinicio de 40+ servicios críticos
  - `BootRecoveryManager` — BCD, bootmgr, winload
  - `RegistryRestoreManager` — copia de seguridad de los hives del registro
  - `WindowsUpdateRepairManager` — reinicio de SoftwareDistribution/BITS/Catroot2
  - `WinSxsRepairManager` — análisis y limpieza del almacén de componentes
  - `EventLogManager` — copia de seguridad/limpieza de registros a través de `wevtapi.dll`
  - `UserEnvRestoreManager` — perfiles de usuario, caché de iconos/fuentes
  - `FileHashDatabaseManager` — instantánea SHA256/SHA1 para comparación

- **Verificación de integridad ampliada**:
  - 40+ archivos críticos (DLL, EXE, controladores)
  - Análisis profundo de System32 + drivers (~5000 archivos)
  - Análisis completo de System32 + SysWOW64 (~10000 archivos)

- **Nuevas API nativas**:
  - `kernel32.dll` — archivos, privilegios, reinicio, módulos
  - `srclient.dll` — puntos de restauración del sistema
  - `advapi32.dll` — Service Control Manager
  - `wevtapi.dll` — registros de eventos
  - `vssapi.dll` — Volume Shadow Copy

- **44 elementos de menú**
- **Modos automáticos**: `--super-full`, `--full`, `--verify`, `--scan-all`

## [1.0.0] — 2026-06-28

### Añadido
- Versión inicial del programa
- API DISM (`dismapi.dll`): CheckHealth, ScanHealth, RestoreHealth, StartComponentCleanup
- API SFC (`sfc.dll`, `sfc_os.dll`): SfcIsFileProtected, SfcGetNextProtectedFile, SfcSynchronousScan
- API WinTrust (`wintrust.dll`, `crypt32.dll`): verificación de firmas Authenticode
- Paquete NuGet Microsoft.Dism como ruta alternativa
- Menú básico de 15 elementos
- Registro en `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log`

[Unreleased]: https://github.com/Ludonist/Windows-System-Recovery-Tool/compare/v2.5.1...HEAD
[2.5.1]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.1
[2.5.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.5.0
[2.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v2.0.0
[1.0.0]: https://github.com/Ludonist/Windows-System-Recovery-Tool/releases/tag/v1.0.0
