using System;
using System.Threading;
using Microsoft.Dism;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;
using DismLogLevelNet = Microsoft.Dism.DismLogLevel;
using DismImageHealthStateNet = Microsoft.Dism.DismImageHealthState;

namespace SystemRestoreTool.Engine
{
    // ===========================================================================
    //  MANAGED DISM WRAPPER  (Microsoft.Dism NuGet, версия 3.2.0)
    //  Нативная управляемая обёртка над DISM API.
    //
    //  ВНИМАНИЕ: Microsoft.Dism 3.2.0 предоставляет только:
    //    • DismApi.Initialize(DismLogLevel, logPath)
    //    • DismApi.Shutdown()
    //    • DismApi.OpenOnlineSession() : DismSession
    //    • session.CheckImageHealth(bool limitAccess) : DismImageHealthState
    //    • session.RestoreImageHealth(bool limitAccess, List<string>, ...)
    //  Методов ScanImageHealth и CleanupImage в этой версии НЕТ — для них
    //  используется наш собственный P/Invoke (DismNativeApi).
    // ===========================================================================

    public static class DismManagedWrapper
    {
        /// <summary>
        /// Полный цикл через Microsoft.Dism NuGet (CheckHealth + RestoreHealth).
        /// Для ScanHealth и CleanupImage делегируем к нашему DismNativeApi.
        /// </summary>
        public static DismManagedResult RunFullRepair(bool doCleanup, bool doResetBase)
        {
            var result = new DismManagedResult();
            string logPath = Logger.Instance.LogFilePath + ".dism.log";

            try
            {
                Logger.Instance.Header(">>> Microsoft.Dism NuGet: Initialize");
                DismApi.Initialize(DismLogLevelNet.LogErrorsWarningsInfo, logPath);
                Logger.Instance.Success("Microsoft.Dism: API инициализирован");

                using (var session = DismApi.OpenOnlineSession())
                {
                    Logger.Instance.Success("Microsoft.Dism: Online-сессия открыта");

                    // 1. CheckHealth
                    Logger.Instance.Header(">>> [1/4] CheckImageHealth (CheckHealth)");
                    var health = DismApi.CheckImageHealth(session, false);
                    result.CheckHealthResult = health.ToString();
                    Logger.Instance.Info($"CheckHealth → {health}");
                    switch (health)
                    {
                        case DismImageHealthStateNet.Healthy:
                            Logger.Instance.Success("Образ здоров (Healthy)");
                            break;
                        case DismImageHealthStateNet.Repairable:
                            Logger.Instance.Warn("Образ повреждён, но восстанавливается (Repairable)");
                            break;
                        case DismImageHealthStateNet.NonRepairable:
                            Logger.Instance.Error("Критическое повреждение (NonRepairable)");
                            break;
                    }

                    // 2. ScanHealth (через нативный API — в NuGet этого нет)
                    Logger.Instance.Header(">>> [2/4] ScanImageHealth (нативный dismapi.dll)");
                    Logger.Instance.Info("Это может занять 5–15 минут...");
                    try { DismNativeApi.RunScanHealth(session.DangerousGetHandle()); }
                    catch (Exception ex) { Logger.Instance.Warn($"ScanHealth fallback не сработал: {ex.Message}"); }
                    Logger.Instance.Success("ScanHealth завершён");

                    // 3. RestoreHealth через NuGet
                    Logger.Instance.Header(">>> [3/4] RestoreImageHealth (RestoreHealth)");
                    Logger.Instance.Info("Это может занять 10–30 минут...");
                    int lastPct = -1;
                    DismApi.RestoreImageHealth(session, false, null, progress =>
                    {
                        if (progress.Total > 0)
                        {
                            int pct = (int)(progress.Current * 100L / progress.Total);
                            if (pct != lastPct)
                            {
                                Logger.Instance.Progress(progress.Current, progress.Total, "RestoreHealth");
                                lastPct = pct;
                            }
                        }
                    });
                    Logger.Instance.Success("RestoreHealth завершён");

                    // 4. Cleanup через нативный API (в NuGet нет)
                    if (doCleanup)
                    {
                        Logger.Instance.Header($">>> [4/4] StartComponentCleanup (ResetBase={doResetBase})");
                        try
                        {
                            lastPct = -1;
                            DismNativeApi.RunComponentCleanup(session.DangerousGetHandle(), doResetBase,
                                (current, total, _) =>
                                {
                                    if (total > 0)
                                    {
                                        int pct = (int)(current * 100L / total);
                                        if (pct != lastPct)
                                        {
                                            Logger.Instance.Progress((int)current, (int)total, "Cleanup");
                                            lastPct = pct;
                                        }
                                    }
                                });
                            Logger.Instance.Success("Cleanup завершён");
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Warn($"Cleanup fallback не сработал: {ex.Message}");
                        }
                    }
                }

                DismApi.Shutdown();
                result.Success = true;
                Logger.Instance.Success("Microsoft.Dism: сессия закрыта и API выгружен");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                Logger.Instance.Error($"Microsoft.Dism ошибка: {ex.Message}");
                try { DismApi.Shutdown(); } catch { /* игнор */ }
            }

            return result;
        }

        /// <summary>Только CheckHealth через Microsoft.Dism.</summary>
        public static DismImageHealthStateNet RunCheckHealthOnly()
        {
            DismApi.Initialize(DismLogLevelNet.LogErrorsWarningsInfo, Logger.Instance.LogFilePath + ".chk.log");
            try
            {
                using var session = DismApi.OpenOnlineSession();
                var h = DismApi.CheckImageHealth(session, false);
                Logger.Instance.Info($"Microsoft.Dism CheckImageHealth → {h}");
                return h;
            }
            finally
            {
                DismApi.Shutdown();
            }
        }

        /// <summary>RestoreHealth через Microsoft.Dism (ScanHealth через нативный API).</summary>
        public static void RunRestoreHealthOnly()
        {
            DismApi.Initialize(DismLogLevelNet.LogErrorsWarningsInfo, Logger.Instance.LogFilePath + ".rest.log");
            try
            {
                using var session = DismApi.OpenOnlineSession();
                int lastPct = -1;
                Logger.Instance.Info("Microsoft.Dism RestoreImageHealth выполняется...");
                DismApi.RestoreImageHealth(session, false, null, progress =>
                {
                    if (progress.Total > 0 && (progress.Current * 100 / progress.Total) != lastPct)
                    {
                        lastPct = (progress.Current * 100 / progress.Total);
                        Logger.Instance.Progress(progress.Current, progress.Total, "RestoreHealth");
                    }
                });
                Logger.Instance.Success("Microsoft.Dism RestoreImageHealth завершён");
            }
            finally
            {
                DismApi.Shutdown();
            }
        }
    }

    public sealed class DismManagedResult
    {
        public bool Success { get; set; }
        public string CheckHealthResult { get; set; }
        public Exception Exception { get; set; }
    }
}
