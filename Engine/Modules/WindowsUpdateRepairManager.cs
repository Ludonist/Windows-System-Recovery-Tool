using System;
using System.IO;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  WINDOWS UPDATE REPAIR MANAGER
    //  Восстановление компонентов Windows Update:
    //    • Сброс кэша SoftwareDistribution
    //    • Перезапуск служб wuauserv, BITS, cryptsvc
    //    • Перерегистрация DLL Windows Update
    //    • Удаление повреждённых загрузок
    //
    //  Эквивалент: PowerShell-скрипты Microsoft "Reset-WindowsUpdate.ps1".
    //  Здесь — через прямой SCM API и File.Delete (без cmd / net stop).
    // ===========================================================================

    public static class WindowsUpdateRepairManager
    {
        /// <summary>DLL-библиотеки Windows Update, которые нужно перерегистрировать.</summary>
        public static readonly string[] WuDlls =
        {
            "atl.dll", "urlmon.dll", "mshtml.dll", "shdocvw.dll", "browseui.dll",
            "jscript.dll", "vbscript.dll", "scrrun.dll", "msxml3.dll", "msxml6.dll",
            "actxprxy.dll", "softpub.dll", "wintrust.dll", "dssenh.dll", "rsaenh.dll",
            "gpkcsp.dll", "sccbase.dll", "slbcsp.dll", "cryptdlg.dll", "oleaut32.dll",
            "shell32.dll", "initpki.dll", "wuapi.dll", "wuaueng.dll", "wuaueng1.dll",
            "wucltui.dll", "wups.dll", "wups2.dll", "wuweb.dll", "qmgr.dll", "qmgrprxy.dll",
            "wucltux.dll", "wuwebv.dll"
        };

        /// <summary>Полный цикл восстановления Windows Update.</summary>
        public static void FullRepair()
        {
            Logger.Instance.Header(">>> ПОЛНОЕ ВОССТАНОВЛЕНИЕ WINDOWS UPDATE");

            // 1. Останавливаем службы через SCM API
            Logger.Instance.Header("[1/5] Остановка служб (SCM API)");
            StopServiceSafely("wuauserv");   // Windows Update
            StopServiceSafely("BITS");       // Background Intelligent Transfer
            StopServiceSafely("CryptSvc");   // Cryptographic Services
            StopServiceSafely("msiserver");  // Windows Installer

            // 2. Удаляем кэш SoftwareDistribution
            Logger.Instance.Header("[2/5] Сброс кэша SoftwareDistribution");
            string sd = @"C:\Windows\SoftwareDistribution";
            string sdBackup = sd + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            RenameOrDeleteFolder(sd, sdBackup);

            // 3. Сброс Catroot2 (хранилище криптографии)
            Logger.Instance.Header("[3/5] Сброс Catroot2");
            string catroot2 = @"C:\Windows\System32\catroot2";
            string catroot2Backup = catroot2 + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            RenameOrDeleteFolder(catroot2, catroot2Backup);

            // 4. Перерегистрация DLL
            Logger.Instance.Header("[4/5] Перерегистрация DLL Windows Update");
            ReregisterWuDlls();

            // 5. Запускаем службы обратно
            Logger.Instance.Header("[5/5] Запуск служб (SCM API)");
            StartServiceSafely("CryptSvc");
            StartServiceSafely("BITS");
            StartServiceSafely("wuauserv");
            StartServiceSafely("msiserver");

            Logger.Instance.Success("Восстановление Windows Update завершено.");
            Logger.Instance.Info("Рекомендуется проверить обновления: Параметры → Обновление и безопасность.");
        }

        /// <summary>Останавливает службу безопасно (без исключений).</summary>
        private static void StopServiceSafely(string svc)
        {
            if (ServicesNativeApi.StopService(svc))
                Logger.Instance.Success($"  {svc}: остановлена");
            else
                Logger.Instance.Warn($"  {svc}: не удалось остановить (возможно уже остановлена)");
        }

        private static void StartServiceSafely(string svc)
        {
            if (ServicesNativeApi.StartService(svc))
                Logger.Instance.Success($"  {svc}: запущена");
            else
                Logger.Instance.Warn($"  {svc}: не удалось запустить (DISABLED?)");
        }

        /// <summary>Переименовывает папку, если не получается — удаляет содержимое.</summary>
        private static void RenameOrDeleteFolder(string path, string backupPath)
        {
            if (!Directory.Exists(path))
            {
                Logger.Instance.Warn($"  Папка не существует: {path}");
                return;
            }

            // Сначала пробуем переименовать
            try
            {
                Directory.Move(path, backupPath);
                Logger.Instance.Success($"  Переименована: {path} → {Path.GetFileName(backupPath)}");
                return;
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"  Переименовать не удалось ({ex.Message}), удаляем содержимое...");
            }

            // Если не вышло — удаляем файлы по одному
            try
            {
                DeleteDirectoryContents(path);
                Logger.Instance.Success($"  Содержимое очищено: {path}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"  Не удалось очистить {path}: {ex.Message}");
            }
        }

        private static void DeleteDirectoryContents(string path)
        {
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(f); } catch { /* файл занят */ }
            }
            foreach (var d in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
            {
                try { if (Directory.Exists(d)) Directory.Delete(d, recursive: true); } catch { }
            }
        }

        /// <summary>Перерегистрирует все DLL Windows Update через regsvr32.</summary>
        private static void ReregisterWuDlls()
        {
            string sysDir = Environment.SystemDirectory;
            int okCount = 0, failCount = 0;

            foreach (var dll in WuDlls)
            {
                string full = Path.Combine(sysDir, dll);
                if (!File.Exists(full))
                {
                    Logger.Instance.Warn($"  {dll} — не найден");
                    continue;
                }

                // Прямой вызов regsvr32 (без cmd.exe) — это ещё нативно, не powershell
                int ec = ConsoleHelper.RunExternal("regsvr32.exe", $"/s \"{full}\"", out _, out _);
                if (ec == 0) { okCount++; Logger.Instance.Success($"  {dll} — зарегистрирован"); }
                else { failCount++; Logger.Instance.Warn($"  {dll} — ошибка {ec}"); }
            }

            Logger.Instance.Info($"Итого: зарегистрировано {okCount}, ошибок {failCount}");
        }

        /// <summary>Удаляет повреждённые загрузки BITS.</summary>
        public static void ClearBitsQueue()
        {
            Logger.Instance.Info("Очистка очереди BITS...");
            StopServiceSafely("BITS");
            try
            {
                // Удаляем файлы состояния BITS
                string bitsPath = @"C:\ProgramData\Microsoft\Network\Downloader";
                if (Directory.Exists(bitsPath))
                {
                    foreach (var f in Directory.EnumerateFiles(bitsPath))
                    {
                        try { File.Delete(f); Logger.Instance.Success($"  Удалён: {Path.GetFileName(f)}"); }
                        catch (Exception ex) { Logger.Instance.Warn($"  Не удалось: {Path.GetFileName(f)} ({ex.Message})"); }
                    }
                }
            }
            finally
            {
                StartServiceSafely("BITS");
            }
        }

        /// <summary>Проверяет наличие повреждённых пакетов CBS.</summary>
        public static void CheckCbsLogs()
        {
            string cbsLog = @"C:\Windows\Logs\CBS\CBS.log";
            if (!File.Exists(cbsLog))
            {
                Logger.Instance.Warn("CBS.log не найден — DISM/Windows Update не запускался.");
                return;
            }

            Logger.Instance.Info($"CBS.log: {cbsLog}");
            var info = new FileInfo(cbsLog);
            Logger.Instance.Info($"Размер: {info.Length:N0} байт, изменён: {info.LastWriteTime}");

            // Поиск ошибок в CBS.log
            try
            {
                int errors = 0;
                using var sr = new StreamReader(cbsLog);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("Error", StringComparison.OrdinalIgnoreCase) &&
                        !line.Contains("ErrorReporting", StringComparison.OrdinalIgnoreCase))
                    {
                        errors++;
                        if (errors <= 5)
                            Logger.Instance.Warn($"  {line.Trim()}");
                    }
                }
                Logger.Instance.Info($"Всего строк с 'Error': {errors}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Не удалось прочитать CBS.log: {ex.Message}");
            }
        }
    }
}
