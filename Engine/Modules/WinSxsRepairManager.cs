using System;
using System.IO;
using System.Linq;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  WINSXS REPAIR MANAGER
    //  Работа с хранилищем компонентов Windows (WinSxS):
    //    • Анализ размера и состояния через DismAnalyzeComponentStore
    //    • Полная очистка (StartComponentCleanup + ResetBase)
    //    • Восстановление повреждённых манифестов
    //  WinSxS = C:\Windows\WinSxS — фактически база всех системных компонентов.
    // ===========================================================================

    public static class WinSxsRepairManager
    {
        /// <summary>Анализ хранилища компонентов через DISM API.</summary>
        public static void AnalyzeComponentStore()
        {
            Logger.Instance.Header(">>> Анализ хранилища компонентов (DismAnalyzeComponentStore)");

            IntPtr session = IntPtr.Zero;
            string logPath = Logger.Instance.LogFilePath + ".analyze.log";

            try
            {
                DismNativeApi.DismInitialize(DismLogLevel.Verbose, logPath, null);
                session = DismNativeApi.DismOpenSession(null, null, null);

                // Анализ
                DismNativeApi.DismAnalyzeComponentStore(session, out var infoPtr);
                if (infoPtr != IntPtr.Zero)
                {
                    var info = System.Runtime.InteropServices.Marshal.PtrToStructure
                        <DismNativeApi.DismComponentStoreInfo>(infoPtr);
                    Logger.Instance.Success($"ActualSize:        {info.ActualSize:N0} байт");
                    Logger.Instance.Success($"SystemCenteredSize: {info.SystemCenteredSizeMB} MB");
                    Logger.Instance.Info($"SharedWithWindows: {info.SharedWithWindows}");
                    Logger.Instance.Info($"BackedUp:          {info.BackedUp}");
                    Logger.Instance.Info($"Disabled:          {info.Disabled}");

                    DismNativeApi.DismDelete(infoPtr);
                }

                // Размер папки WinSxS на диске
                var winsxs = new DirectoryInfo(@"C:\Windows\WinSxS");
                if (winsxs.Exists)
                {
                    long size = GetDirectorySize(winsxs);
                    Logger.Instance.Info($"Физический размер C:\\Windows\\WinSxS: {size / 1024.0 / 1024.0:N0} MB");
                    Logger.Instance.Info($"Кол-во файлов в WinSxS: {CountFiles(winsxs):N0}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"AnalyzeComponentStore: {ex.Message}");
            }
            finally
            {
                try { if (session != IntPtr.Zero) DismNativeApi.DismCloseSession(session); } catch { }
                try { DismNativeApi.DismShutdown(); } catch { }
            }
        }

        /// <summary>Полная очистка WinSxS (с ResetBase или без).</summary>
        public static void FullCleanup(bool resetBase)
        {
            Logger.Instance.Header($">>> Полная очистка WinSxS (ResetBase={resetBase})");

            if (resetBase)
            {
                Logger.Instance.Warn("ВНИМАНИЕ: ResetBase необратим — нельзя удалить установленные обновления!");
                Logger.Instance.Warn("Все предыдущие версии компонентов будут удалены навсегда.");
            }

            IntPtr session = IntPtr.Zero;
            string logPath = Logger.Instance.LogFilePath + ".cleanup.log";

            try
            {
                DismNativeApi.DismInitialize(DismLogLevel.Verbose, logPath, null);
                session = DismNativeApi.DismOpenSession(null, null, null);

                int lastPct = -1;
                DismNativeApi.RunComponentCleanup(session, resetBase, (current, total, _) =>
                {
                    if (total > 0)
                    {
                        int pct = (int)(current * 100L / total);
                        if (pct != lastPct)
                        {
                            Logger.Instance.Progress((int)current, (int)total, "WinSxS Cleanup");
                            lastPct = pct;
                        }
                    }
                });
                Console.WriteLine();
                Logger.Instance.Success("Очистка WinSxS завершена.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"WinSxS cleanup: {ex.Message}");
            }
            finally
            {
                try { if (session != IntPtr.Zero) DismNativeApi.DismCloseSession(session); } catch { }
                try { DismNativeApi.DismShutdown(); } catch { }
            }
        }

        /// <summary>Проверка манифестов компонентов в WinSxS.</summary>
        public static void CheckManifests()
        {
            Logger.Instance.Header(">>> Проверка манифестов компонентов WinSxS");

            string manifestsDir = @"C:\Windows\WinSxS\Manifests";
            if (!Directory.Exists(manifestsDir))
            {
                Logger.Instance.Error("Папка Manifests не найдена — критическая проблема!");
                return;
            }

            int total = 0, valid = 0, broken = 0;
            long totalSize = 0;

            foreach (var f in Directory.EnumerateFiles(manifestsDir, "*.manifest"))
            {
                total++;
                var info = new FileInfo(f);
                totalSize += info.Length;

                if (info.Length == 0)
                {
                    broken++;
                    Logger.Instance.Error($"  ПУСТОЙ манифест: {Path.GetFileName(f)}");
                    continue;
                }

                // Простейшая проверка XML-структуры
                try
                {
                    var doc = new System.Xml.XmlDocument();
                    doc.Load(f);
                    valid++;
                }
                catch (Exception ex)
                {
                    broken++;
                    Logger.Instance.Error($"  ПОВРЕЖДЁН: {Path.GetFileName(f)} — {ex.Message}");
                }
            }

            Logger.Instance.Info($"Всего манифестов:  {total:N0}");
            Logger.Instance.Success($"Валидных:          {valid:N0}");
            Logger.Instance.Error($"Повреждённых:      {broken:N0}");
            Logger.Instance.Info($"Общий размер:      {totalSize / 1024.0:N0} KB");

            if (broken > 0)
            {
                Logger.Instance.Warn("Найдены повреждённые манифесты. Запустите DISM /RestoreHealth для исправления.");
            }
        }

        // -----------------------------------------------------------------

        private static long GetDirectorySize(DirectoryInfo dir)
        {
            try { return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); }
            catch { return 0; }
        }

        private static long CountFiles(DirectoryInfo dir)
        {
            try { return dir.EnumerateFiles("*", SearchOption.AllDirectories).LongCount(); }
            catch { return 0; }
        }
    }
}
