using System;
using System.Runtime.InteropServices;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  WINDOWS UPDATE AGENT MANAGER (WUA API через COM)
    //  Прямое использование COM-интерфейса ISystemInformation / IUpdateSession.
    //  CLSID UpdateSession = {4CB43D7F-2EEE-4FCB-A1F3-5CF0E69E84D3}
    //  Документация: https://learn.microsoft.com/windows/win32/wua_sdk/portal-client
    // ===========================================================================

    public static class WindowsUpdateAgentManager
    {
        /// <summary>Проверяет текущее состояние Windows Update через COM API.</summary>
        public static void CheckUpdateStatus()
        {
            Logger.Instance.Header(">>> Статус Windows Update (COM WUA API)");

            try
            {
                // ISystemInformation — для общей информации о системе
                dynamic sysInfo = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.SystemInfo"));
                Logger.Instance.Info($"RebootRequired: {sysInfo.RebootRequired}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"SystemInfo COM: {ex.Message}");
            }

            try
            {
                // IUpdateSession — основная точка доступа к WU
                dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));

                // Создаём поиск обновлений
                dynamic searcher = session.CreateUpdateSearcher();
                Logger.Instance.Info($"ServerSelection: {searcher.ServerSelection}");
                Logger.Instance.Info($"Online: {searcher.Online}");

                // История поиска обновлений
                int historyCount = searcher.GetTotalHistoryCount();
                Logger.Instance.Info($"История обновлений: {historyCount} записей");

                if (historyCount > 0)
                {
                    // Покажем последние 10 обновлений
                    var history = searcher.QueryHistory(0, Math.Min(10, historyCount));
                    Logger.Instance.Info("");
                    Logger.Instance.Info("=== Последние 10 обновлений ===");
                    int idx = 0;
                    foreach (var item in history)
                    {
                        idx++;
                        Logger.Instance.Info($"  [{idx}] {item.Title}");
                        Logger.Instance.Info($"       Дата: {item.Date}");
                        Logger.Instance.Info($"       Результат: 0x{item.ResultCode:X} ({ResultToString(item.ResultCode)})");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"UpdateSession COM: {ex.Message}");
                Logger.Instance.Warn("Возможно служба wuauserv остановлена. Запустите её через пункт 20.");
            }
        }

        /// <summary>Преобразует код результата WU в строку.</summary>
        private static string ResultToString(int code)
        {
            return code switch
            {
                0 => "NotStarted",
                1 => "InProgress",
                2 => "Succeeded",
                3 => "SucceededWithErrors",
                4 => "Failed",
                5 => "Aborted",
                _ => $"Unknown({code})"
            };
        }

        /// <summary>Ищет доступные обновления (не устанавливая их).</summary>
        public static void SearchForUpdates()
        {
            Logger.Instance.Header(">>> Поиск доступных обновлений (WUA COM)");
            Logger.Instance.Warn("Это может занять несколько минут...");

            try
            {
                dynamic session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
                dynamic searcher = session.CreateUpdateSearcher();

                Logger.Instance.Info("Поиск обновлений...");
                // Ищем все неустановленные обновления
                var result = searcher.Search("IsInstalled=0 and Type='Software'");

                Logger.Instance.Success($"Поиск завершён. Найдено обновлений: {result.Updates.Count}");

                int idx = 0;
                foreach (var update in result.Updates)
                {
                    idx++;
                    Logger.Instance.Info($"  [{idx}] {update.Title}");
                    Logger.Instance.Info($"       Размер: {update.MaxDownloadSize / 1024.0 / 1024.0:F1} MB");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"SearchForUpdates: {ex.Message}");
            }
        }

        /// <summary>Выводит информацию об автоматическом обновлении (AU) через реестр.</summary>
        public static void CheckAutomaticUpdates()
        {
            Logger.Instance.Header(">>> Настройки автоматического обновления (AU)");

            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
                if (key != null)
                {
                    var noAU = key.GetValue("NoAutoUpdate");
                    var auOpt = key.GetValue("AUOptions");
                    var schedInst = key.GetValue("ScheduledInstallTime");
                    Logger.Instance.Info($"NoAutoUpdate:        {noAU ?? "(не задано)"}");
                    Logger.Instance.Info($"AUOptions:           {auOpt ?? "(не задано)"} ({AuOptionsToString(auOpt)})");
                    Logger.Instance.Info($"ScheduledInstallTime: {schedInst ?? "(не задано)"}");
                }
                else
                {
                    Logger.Instance.Info("Политики AU не настроены (используются параметры по умолчанию).");
                }

                using var wuKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update");
                if (wuKey != null)
                {
                    Logger.Instance.Info("");
                    Logger.Instance.Info("=== Параметры Windows Update ===");
                    foreach (var name in wuKey.GetValueNames())
                    {
                        Logger.Instance.Info($"  {name} = {wuKey.GetValue(name)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"CheckAutomaticUpdates: {ex.Message}");
            }
        }

        private static string AuOptionsToString(object val)
        {
            if (val == null) return "";
            int v = Convert.ToInt32(val);
            return v switch
            {
                1 => "Не проверять",
                2 => "Проверять, но не скачивать",
                3 => "Проверять, спросить перед скачиванием",
                4 => "Проверять, скачивать, спросить перед установкой",
                5 => "Автоматически: скачать и установить",
                _ => $"Unknown({v})"
            };
        }
    }
}
