using System;
using System.IO;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  WER MANAGER  (Windows Error Reporting)
    //  Управление отчётами об ошибках Windows. Включение/отключение, очистка
    //  очереди, проверка журналов сбоев.
    // ===========================================================================

    public static class WerManager
    {
        /// <summary>Включает Windows Error Reporting.</summary>
        public static void EnableWer()
        {
            Logger.Instance.Info("Включение Windows Error Reporting...");
            bool ok = WerNativeApi.EnableWer();
            if (ok) Logger.Instance.Success("WER включён.");
            else Logger.Instance.Error("Не удалось включить WER.");
        }

        /// <summary>Отключает Windows Error Reporting.</summary>
        public static void DisableWer()
        {
            Logger.Instance.Info("Отключение Windows Error Reporting...");
            bool ok = WerNativeApi.SetWerDisabled(true);
            if (ok) Logger.Instance.Success("WER отключён.");
            else Logger.Instance.Error("Не удалось отключить WER.");
        }

        /// <summary>Очищает очередь и архив отчётов об ошибках.</summary>
        public static void ClearWerReports()
        {
            Logger.Instance.Header(">>> Очистка очереди WER");
            WerNativeApi.ClearWerQueue();

            // Дополнительно — очистка локальных очередей пользователя
            string[] userWerDirs =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "ReportQueue"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "ReportArchive"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "ERC"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "TMP")
            };

            foreach (var dir in userWerDirs)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    foreach (var d in Directory.GetDirectories(dir))
                    {
                        try { Directory.Delete(d, recursive: true); } catch { }
                    }
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        try { File.Delete(f); } catch { }
                    }
                    Logger.Instance.Success($"Очищено: {dir}");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn($"Не удалось очистить {dir}: {ex.Message}");
                }
            }

            Logger.Instance.Success("Очистка WER завершена.");
        }

        /// <summary>Выводит статистику отчётов об ошибках.</summary>
        public static void ShowWerStatistics()
        {
            Logger.Instance.Header(">>> Статистика Windows Error Reporting");

            bool disabled = WerNativeApi.IsWerDisabled();
            Logger.Instance.Info($"WER {(disabled ? "ОТКЛЮЧЁН" : "ВКЛЮЧЁН")}");

            // Системная очередь
            string[] systemDirs =
            {
                @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue",
                @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive",
                @"C:\ProgramData\Microsoft\Windows\WER\Temp"
            };

            // Пользовательская очередь
            string[] userDirs =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "ReportQueue"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER", "ReportArchive")
            };

            Logger.Instance.Info("");
            Logger.Instance.Info("=== Системные отчёты ===");
            foreach (var dir in systemDirs)
            {
                ShowDirStats(dir);
            }

            Logger.Instance.Info("");
            Logger.Instance.Info("=== Пользовательские отчёты ===");
            foreach (var dir in userDirs)
            {
                ShowDirStats(dir);
            }
        }

        private static void ShowDirStats(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Logger.Instance.Info($"  {dir} — не существует");
                return;
            }
            try
            {
                int dirCount = 0;
                long totalSize = 0;
                foreach (var d in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    dirCount++;
                    foreach (var f in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
                    {
                        try { totalSize += new FileInfo(f).Length; } catch { }
                    }
                }
                Logger.Instance.Info($"  {dir}");
                Logger.Instance.Info($"    Отчётов: {dirCount}, размер: {totalSize / 1024.0 / 1024.0:F1} MB");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"  {dir} — ошибка: {ex.Message}");
            }
        }

        /// <summary>Показывает список последних сбоев приложений из WER-журнала.</summary>
        public static void ShowRecentCrashes()
        {
            Logger.Instance.Header(">>> Последние сбои приложений (из WER-архива)");

            // Читаем из реестра список недавних падений приложений
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting\ReportingArchive");
                if (key == null)
                {
                    Logger.Instance.Warn("Не найден раздел ReportingArchive.");
                    return;
                }

                int count = 0;
                foreach (var subName in key.GetSubKeyNames())
                {
                    if (count++ >= 30) break;
                    using var sub = key.OpenSubKey(subName);
                    if (sub == null) continue;

                    string appName = sub.GetValue("AppName") as string ?? subName;
                    string eventType = sub.GetValue("EventType") as string ?? "?";
                    string startTime = sub.GetValue("StartTime") as string ?? "?";
                    Logger.Instance.Info($"  {appName,-30} {eventType,-20} {startTime}");
                }
                Logger.Instance.Info($"Показано {count} записей.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Не удалось прочитать ReportingArchive: {ex.Message}");
            }
        }
    }
}
