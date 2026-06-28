# Contributing to System Restore Tool

Спасибо, что хотите внести вклад! 🎉

Этот проект — open-source инструмент для восстановления системных файлов Windows 10/11 через прямые вызовы Win32 API. Любой вклад приветствуется: баг-репорты, фичи, перевод, документация.

## 📋 Содержание

- [Как сообщить о баге](#-как-сообщить-о-баге)
- [Как предложить фичу](#-как-предложить-фичу)
- [Настройка окружения для разработки](#-настройка-окружения)
- [Стиль кода](#-стиль-кода)
- [Процесс Pull Request](#-процесс-pull-request)
- [Правила безопасности](#-правила-безопасности)

## 🐛 Как сообщить о баге

Перед созданием issue:
1. Проверьте [существующие issues](https://github.com/Ludonist/Windows-System-Recovery-Tool/issues) — возможно, баг уже известен.
2. Обновитесь до последней версии с [Releases](https://github.com/Ludonist/Windows-System-Recovery-Tool/releases).
3. Соберите диагностическую информацию.

В issue укажите:
- **Версия программы** (из баннера при запуске)
- **Версия Windows** (Win+R → `winver`)
- **Архитектура** (x64 / ARM64)
- **Шаги воспроизведения**
- **Ожидаемое поведение**
- **Фактическое поведение**
- **Лог программы** (файл `%LOCALAPPDATA%\SystemRestoreTool\srt_*.log` — прикрепите к issue)

## 💡 Как предложить фичу

1. Создайте issue с тегом `enhancement`.
2. Опишите сценарий использования: зачем это нужно, какую проблему решает.
3. Предложите API/DLL, через которые это можно реализовать (если знаете).

## 🛠 Настройка окружения

### Требования
- **Windows 10** (build 19041+) или **Windows 11**
- **.NET 6.0 SDK** или новее — https://dotnet.microsoft.com/download
- (Опционально) Visual Studio 2022 / Rider / VS Code

### Сборка
```bash
git clone https://github.com/Ludonist/Windows-System-Recovery-Tool.git
cd Windows-System-Recovery-Tool
dotnet restore
dotnet build -c Release
```

Результат: `bin\Release\net6.0-windows10.0.19041.0\win-x64\SystemRestoreTool.exe`

### Запуск
Программа требует прав администратора (манифест уже это указывает).
Правый клик → **Запуск от имени администратора**.

## 🎨 Стиль кода

### Общие правила
- **C# 10+**, .NET 6, `ImplicitUsings=enable`, `Nullable=disable`
- 4 пробела для отступа, **без табов**
- Длина строки — до 120 символов
- `PascalCase` для публичных членов, `camelCase` для локальных переменных
- `private`-поля без префикса `_`

### Структура файла
```csharp
using System;
using System.Runtime.InteropServices;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Api
{
    /// <summary>
    /// Краткое описание класса (1-2 строки).
    /// </summary>
    public static class XxxNativeApi
    {
        private const string DLL = "xxx.dll";

        // Группируйте по назначению, отделяйте комментариями
        // --- Инициализация / завершение ---

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void XxxInitialize(...);
    }
}
```

### P/Invoke-обёртки
- Используйте `PreserveSig = false` для HRESULT-возвращающих функций
- Имена функций сохраняйте с суффиксом `W` (Unicode): `CreateFileW`, не `CreateFile`
- Константы называйте в стиле Win32: `GENERIC_READ`, `OPEN_EXISTING`

### Логирование
Используйте статический `Logger.Instance`:
```csharp
Logger.Instance.Info("Сообщение");
Logger.Instance.Success("Успешно");
Logger.Instance.Warn("Внимание");
Logger.Instance.Error("Ошибка");
Logger.Instance.Progress(current, total, "Метка");
```

## 🔄 Процесс Pull Request

1. **Fork** репозитория
2. Создайте ветку: `git checkout -b feature/my-feature`
3. **Commit** с понятным сообщением:
   ```
   feat: добавить проверку TPM через tbs.dll
   
   - Tbsi_Get_TCG_Log_Ex для получения PCR log
   - Интеграция в IntegrityChecker
   - Обновлён README
   ```
4. Запушьте: `git push origin feature/my-feature`
5. Откройте PR в `main` с описанием изменений

### Префиксы коммитов
- `feat:` — новая функциональность
- `fix:` — исправление бага
- `docs:` — только документация
- `refactor:` — рефакторинг без изменения поведения
- `test:` — добавление/правка тестов
- `chore:` — обслуживание проекта (зависимости, .gitignore и т.д.)

## ⚠️ Правила безопасности

Этот инструмент работает с критическими системными компонентами. Поэтому:

1. **Никогда не коммитьте код, который молча удаляет системные файлы** — каждое удаление должно быть явным и логироваться.
2. **Тестируйте в виртуальной машине** перед PR, особенно если меняете `RegistryRestoreManager`, `BootRecoveryManager` или `WinSxsRepairManager`.
3. **Не добавляйте вызовы**, которые нельзя отменить (например, `FormatEx`, `DeleteVolumeMountPoint`), без подтверждения пользователя.
4. **P/Invoke сигнатуры** — дважды проверяйте на соответствие [Win32 docs](https://learn.microsoft.com/windows/win32/api/). Ошибки в сигнатурах = краш программы или порча памяти.
5. **Приватные ключи** и любые учётные данные — запрещены в репозитории.

## 📜 Лицензия

Внося вклад, вы соглашаетесь, что ваш код будет опубликован под лицензией [MIT](LICENSE).

## 🙏 Признательность

Контрибьюторы будут перечислены в [README.md](README.md#-признательность) (раздел Acknowledgements).
