# Contribuir / Участие / 贡献

🌐 [🇷🇺 Русский](CONTRIBUTING.md) · [🇬🇧 English](CONTRIBUTING.en.md) · [🇨🇳 简体中文](CONTRIBUTING.zh.md) · [🇩🇪 Deutsch](CONTRIBUTING.de.md) · [🇫🇷 Français](CONTRIBUTING.fr.md) · [🇪🇸 Español](CONTRIBUTING.es.md) · [🇯🇵 日本語](CONTRIBUTING.ja.md) · [🇰🇷 한국어](CONTRIBUTING.ko.md) · [🇵🇹 Português](CONTRIBUTING.pt.md)

---

# Contribuir a System Restore Tool (Español)

¡Gracias por querer contribuir! 🎉

Este proyecto es una herramienta de código abierto para recuperar archivos del sistema de Windows 10/11 mediante llamadas directas a la API Win32. Cualquier contribución es bienvenida: reportes de bugs, funciones, traducciones, documentación.

## 📋 Tabla de contenidos

- [Cómo reportar un bug](#-cómo-reportar-un-bug)
- [Cómo proponer una función](#-cómo-proponer-una-función)
- [Configuración del entorno de desarrollo](#-configuración-del-entorno-de-desarrollo)
- [Estilo de código](#-estilo-de-código)
- [Proceso de Pull Request](#-proceso-de-pull-request)
- [Reglas de seguridad](#-reglas-de-seguridad)

## 🐛 Cómo reportar un bug

Antes de crear un issue:
1. Revisa los [issues existentes](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — el bug podría ser ya conocido.
2. Actualiza a la última versión desde [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Recopila información de diagnóstico.

En el issue, especifica:
- **Versión del programa** (del banner al inicio)
- **Versión de Windows** (Win+R → `winver`)
- **Arquitectura** (x64 / ARM64)
- **Pasos para reproducir**
- **Comportamiento esperado**
- **Comportamiento real**
- **Registro del programa** (archivo `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — adjuntar al issue)

## 💡 Cómo proponer una función

1. Crea un issue con la etiqueta `enhancement`.
2. Describe el caso de uso: por qué se necesita, qué problema resuelve.
3. Sugiere la API/DLL a través de la cual se puede implementar (si lo sabes).

## 🛠 Configuración del entorno de desarrollo

### Requisitos
- **Windows 10** (build 19041+) o **Windows 11**
- **.NET 6.0 SDK** o más reciente — https://dotnet.microsoft.com/download
- (Opcional) Visual Studio 2022 / Rider / VS Code

### Build
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Resultado: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Ejecución
El programa requiere privilegios de administrador (el manifiesto ya lo especifica).
Clic derecho → **Ejecutar como administrador**.

## 🎨 Estilo de código

### Reglas generales
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 espacios para la indentación, **sin tabuladores**
- Longitud de línea hasta 120 caracteres
- `PascalCase` para miembros públicos, `camelCase` para variables locales

### Wrappers P/Invoke
- Usa `PreserveSig = false` para funciones que devuelven HRESULT
- Mantén nombres de función con sufijo `W` (Unicode): `CreateFileW`, no `CreateFile`
- Nombra las constantes en estilo Win32: `GENERIC_READ`, `OPEN_EXISTING`

### Logging
Usa el `Logger.Instance` estático:
```csharp
Logger.Instance.Info("Message");
Logger.Instance.Success("Success");
Logger.Instance.Warn("Warning");
Logger.Instance.Error("Error");
Logger.Instance.Progress(current, total, "Label");
```

### Localización
Todas las cadenas de la interfaz deben estar en `Resources/strings.{ru,en,zh}.json` y accederse vía:
```csharp
Utils.Localizer.S("section.key")
```

## 🔄 Proceso de Pull Request

1. **Haz un fork** del repositorio
2. Crea una rama: `git checkout -b feature/my-feature`
3. **Haz commit** con un mensaje claro:
   ```
   feat: add TPM check via tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex for PCR log retrieval
   - Integrated into IntegrityChecker
   - Updated README
   ```
4. Push: `git push origin feature/my-feature`
5. Abre una PR a `main` con una descripción de los cambios

### Prefijos de commit
- `feat:` — nueva funcionalidad
- `fix:` — corrección de bug
- `docs:` — solo documentación
- `refactor:` — refactorización sin cambio de comportamiento
- `test:` — añadir/corregir tests
- `chore:` — mantenimiento del proyecto (dependencias, .gitignore, etc.)
- `i18n:` — cambios de localización

## ⚠️ Reglas de seguridad

Esta herramienta trabaja con componentes críticos del sistema. Por lo tanto:

1. **Nunca hagas commit de código que elimine archivos del sistema silenciosamente** — cada eliminación debe ser explícita y registrada.
2. **Prueba en una máquina virtual** antes de la PR, especialmente si cambias `RegistryRestoreManager`, `BootRecoveryManager` o `WinSxsRepairManager`.
3. **No añadas llamadas** que no se puedan deshacer (p. ej., `FormatEx`, `DeleteVolumeMountPoint`) sin confirmación del usuario.
4. **Firmas P/Invoke** — verifica dos veces contra la [documentación de Win32](https://learn.microsoft.com/windows/win32/api/). Errores en firmas = caída del programa o corrupción de memoria.
5. **Claves privadas** y cualquier credencial están prohibidos en el repositorio.

## 📜 Licencia

Al contribuir, aceptas que tu código se publique bajo la licencia [MIT](LICENSE).

## 🙏 Agradecimientos

Los colaboradores serán listados en [README.md](README.md).
