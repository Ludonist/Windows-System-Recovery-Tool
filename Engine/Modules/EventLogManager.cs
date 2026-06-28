using System;
using System.IO;
using System.Linq;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  EVENT LOG MANAGER
    //  Резервное копирование, очистка и восстановление журналов событий
    //  Windows через advapi32.dll и wevtapi.dll.
    // ===========================================================================

    public static class EventLogManager
    {
        /// <summary>Ключевые журналы событий Windows.</summary>
        public static readonly string[] CriticalChannels =
        {
            "System",
            "Application",
            "Security",
            "Setup",
            "Microsoft-Windows-WindowsUpdateClient/Operational",
            "Microsoft-Windows-DISM-Online/Analytic",
            "Microsoft-Windows-TaskScheduler/Operational",
            "Microsoft-Windows-ServiceControlManager/Operational",
            "Microsoft-Windows-WER-SystemErrorReporting/Operational",
            "Microsoft-Windows-Kernel-Power/Operational",
            "Microsoft-Windows-Kernel-General/Operational",
            "Microsoft-Windows-Wininit/Operational",
            "Microsoft-Windows-Winlogon/Operational",
            "Microsoft-Windows-UserPnp/DeviceMetadata/Debug",
            "Microsoft-Windows-DeviceSetupManager/Operational"
        };

        /// <summary>Создаёт резервную копию всех ключевых журналов.</summary>
        public static void BackupAllLogs(string backupDir = null)
        {
            backupDir ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemRestoreTool", "EventLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(backupDir);
            Logger.Instance.Header($">>> Бэкап журналов событий → {backupDir}");

            int okCount = 0, failCount = 0;
            foreach (var channel in CriticalChannels)
            {
                string safeName = channel.Replace('/', '_').Replace('\\', '_');
                string outFile = Path.Combine(backupDir, safeName + ".evtx");

                try
                {
                    bool ok = EventLogNativeApi.BackupChannel(channel, outFile);
                    if (ok)
                    {
                        var fi = new FileInfo(outFile);
                        Logger.Instance.Success($"  {channel} → {fi.Length / 1024.0:N1} KB");
                        okCount++;
                    }
                    else
                    {
                        Logger.Instance.Warn($"  {channel} — не удалось");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn($"  {channel} — {ex.Message}");
                    failCount++;
                }
            }

            Logger.Instance.Info($"Итого: {okCount} журналов сохранено, {failCount} неудач");
        }

        /// <summary>Очищает все журналы событий (с бэкапом).</summary>
        public static void ClearAllLogsWithBackup()
        {
            string backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemRestoreTool", "EventLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupDir);
            Logger.Instance.Header($">>> Очистка журналов с бэкапом → {backupDir}");

            int okCount = 0;
            foreach (var channel in CriticalChannels)
            {
                string safeName = channel.Replace('/', '_').Replace('\\', '_');
                string outFile = Path.Combine(backupDir, safeName + ".evtx");

                try
                {
                    bool ok = EventLogNativeApi.ClearChannel(channel, outFile);
                    if (ok) { okCount++; Logger.Instance.Success($"  {channel} — очищен"); }
                    else Logger.Instance.Warn($"  {channel} — не удалось очистить");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn($"  {channel} — {ex.Message}");
                }
            }

            Logger.Instance.Info($"Очищено {okCount} журналов.");
        }

        /// <summary>Выводит размеры ключевых файлов журналов.</summary>
        public static void CheckLogSizes()
        {
            Logger.Instance.Header(">>> Размеры файлов журналов событий");

            string logPath = @"C:\Windows\System32\winevt\Logs";
            if (!Directory.Exists(logPath))
            {
                Logger.Instance.Error($"Папка логов не найдена: {logPath}");
                return;
            }

            var files = Directory.EnumerateFiles(logPath, "*.evtx")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Length)
                .Take(20)
                .ToList();

            Logger.Instance.Info($"Топ-20 файлов по размеру в {logPath}:");
            foreach (var f in files)
            {
                Logger.Instance.Info($"  {f.Name,-70} {f.Length / 1024.0 / 1024.0,8:N1} MB");
            }

            long total = Directory.EnumerateFiles(logPath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
            Logger.Instance.Info($"Общий размер всех журналов: {total / 1024.0 / 1024.0:N0} MB");
        }
    }
}
