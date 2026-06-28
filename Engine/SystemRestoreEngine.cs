using System;
using System.Runtime.InteropServices;
using System.Threading;
using SystemRestoreTool.Api;
using SystemRestoreTool.Engine.Modules;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine
{
    // ===========================================================================
    //  SYSTEM RESTORE ENGINE  v2.0
    //  Высокоуровневая оркестрация ПОЛНОГО цикла восстановления через ПРЯМЫЕ
    //  P/Invoke вызовы к dismapi.dll / sfc.dll / sfc_os.dll / wintrust.dll /
    //  srclient.dll / advapi32.dll + все модули восстановления.
    // ===========================================================================

    public static class SystemRestoreEngine
    {
        // ---------------------------------------------------------------------
        //  СУПЕР-ПОЛНЫЙ ЦИКЛ ВОССТАНОВЛЕНИЯ
        //  1. Создание точки восстановления
        //  2. CheckHealth + ScanHealth + RestoreHealth (нативный DISM)
        //  3. StartComponentCleanup (без ResetBase)
        //  4. Sfc /ScanNow (нативный sfc_os.dll)
        //  5. Полная проверка целостности (System32 + drivers)
        //  6. Перезапуск критических служб
        //  7. Восстановление Windows Update
        //  8. Очистка кэшей (icon, font, SoftwareDistribution)
        // ---------------------------------------------------------------------

        public static void RunSuperFullCycle(bool createRestorePoint, bool resetBase)
        {
            Logger.Instance.Header(">>> СУПЕР-ПОЛНЫЙ ЦИКЛ ВОССТАНОВЛЕНИЯ СИСТЕМЫ (v2.0)");

            // 1. Точка восстановления
            if (createRestorePoint)
            {
                Logger.Instance.Header("[1/8] Создание точки восстановления");
                SystemRestorePointManager.CreatePreRestorePoint();
            }

            // 2. DISM: CheckHealth + ScanHealth + RestoreHealth
            Logger.Instance.Header("[2/8] DISM через нативный API (dismapi.dll)");
            RunDismCheckScanRestore(false);

            // 3. Очистка WinSxS
            Logger.Instance.Header("[3/8] Очистка хранилища компонентов (WinSxS)");
            try { WinSxsRepairManager.FullCleanup(resetBase); }
            catch (Exception ex) { Logger.Instance.Error($"WinSxS cleanup: {ex.Message}"); }

            // 4. SFC /ScanNow через нативный sfc_os.dll
            Logger.Instance.Header("[4/8] Sfc /ScanNow через нативный API (sfc_os.dll)");
            try { RunSfcScanNow(SfcScanType.ScanAndRepair); }
            catch (Exception ex) { Logger.Instance.Error($"SFC: {ex.Message}"); }

            // 5. Полная проверка целостности критических файлов
            Logger.Instance.Header("[5/8] Проверка целостности критических файлов");
            try
            {
                var report = IntegrityChecker.CheckCriticalFiles(ScanScope.CriticalOnly,
                    (c, t, n) => Logger.Instance.Progress(c, t, n));
                Console.WriteLine();
                IntegrityChecker.PrintReport(report);
            }
            catch (Exception ex) { Logger.Instance.Error($"Integrity: {ex.Message}"); }

            // 6. Перезапуск критических служб
            Logger.Instance.Header("[6/8] Перезапуск критических служб Windows");
            try { ServicesRepairManager.RestartAllCriticalServices(); }
            catch (Exception ex) { Logger.Instance.Error($"Services restart: {ex.Message}"); }

            // 7. Восстановление Windows Update
            Logger.Instance.Header("[7/8] Восстановление Windows Update");
            try { WindowsUpdateRepairManager.FullRepair(); }
            catch (Exception ex) { Logger.Instance.Error($"WU repair: {ex.Message}"); }

            // 8. Очистка кэшей
            Logger.Instance.Header("[8/8] Очистка кэшей (иконки, шрифты)");
            try { UserEnvRestoreManager.RebuildIconCache(); }
            catch (Exception ex) { Logger.Instance.Error($"Icon cache: {ex.Message}"); }
            try { UserEnvRestoreManager.RebuildFontCache(); }
            catch (Exception ex) { Logger.Instance.Error($"Font cache: {ex.Message}"); }

            Logger.Instance.Header(">>> СУПЕР-ПОЛНЫЙ ЦИКЛ ЗАВЕРШЁН");
            Logger.Instance.Info("Рекомендуется перезагрузить систему для применения всех изменений.");
        }

        // ---------------------------------------------------------------------
        //  ПОЛНЫЙ ЦИКЛ ЧЕРЕЗ ПРЯМЫЕ API (без Microsoft.Dism NuGet)
        // ---------------------------------------------------------------------

        public static void RunFullNativeCycle(bool includeCleanup, bool includeResetBase)
        {
            IntPtr session = IntPtr.Zero;
            string logPath = Logger.Instance.LogFilePath + ".native.dism.log";

            try
            {
                Logger.Instance.Header(">>> [1/6] DismInitialize");
                DismNativeApi.DismInitialize(DismLogLevel.Verbose, logPath, null);
                Logger.Instance.Success("DISM API инициализирован (dismapi.dll)");

                Logger.Instance.Header(">>> [2/6] DismOpenSession (Online)");
                session = DismNativeApi.DismOpenSession(null, null, null);
                Logger.Instance.Success($"Online-сессия открыта (IntPtr = 0x{session.ToString("X")})");

                Logger.Instance.Header(">>> [3/6] DismCheckImageHealth (CheckHealth)");
                string health = DismNativeApi.RunCheckHealth(session);
                Logger.Instance.Info($"Результат CheckHealth: {health}");

                Logger.Instance.Header(">>> [4/6] DismScanImageHealth (ScanHealth)");
                Logger.Instance.Info("Это может занять 5–15 минут...");
                DismNativeApi.RunScanHealth(session);
                Logger.Instance.Success("ScanHealth завершён");

                Logger.Instance.Header(">>> [5/6] DismRestoreImageHealth (RestoreHealth)");
                Logger.Instance.Info("Это может занять 10–30 минут...");
                int lastPct = -1;
                DismNativeApi.RunRestoreHealth(session, (current, total, _) =>
                {
                    if (total > 0)
                    {
                        int pct = (int)(current * 100L / total);
                        if (pct != lastPct)
                        {
                            Logger.Instance.Progress((int)current, (int)total, "RestoreHealth");
                            lastPct = pct;
                        }
                    }
                });
                Logger.Instance.Success("RestoreHealth завершён");

                if (includeCleanup)
                {
                    Logger.Instance.Header($">>> [6/6] DismStartComponentCleanup (ResetBase={includeResetBase})");
                    lastPct = -1;
                    DismNativeApi.RunComponentCleanup(session, includeResetBase, (current, total, _) =>
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
                    Logger.Instance.Success("StartComponentCleanup завершён");
                }

                DismNativeApi.DismCloseSession(session);
                session = IntPtr.Zero;
                DismNativeApi.DismShutdown();
                Logger.Instance.Success("DISM API: сессия закрыта и API выгружен");
            }
            catch (COMException ex)
            {
                Logger.Instance.Error($"DISM COM Error 0x{ex.HResult:X8}: {ex.Message}");
                TryCleanup(session);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"DISM общая ошибка: {ex.Message}");
                TryCleanup(session);
                throw;
            }

            Logger.Instance.Header(">>> [SFC] SfcSynchronousScan (Sfc /ScanNow)");
            Logger.Instance.Info("Запуск SFC-сканирования через нативный API sfc_os.dll...");
            bool ok = SfcNativeApi.RunSfcScan(SfcScanType.ScanAndRepair);
            if (ok) Logger.Instance.Success("SFC-сканирование завершено");
            else Logger.Instance.Warn("SfcSynchronousScan не отработал — возможно требуется sfc.exe");

            Logger.Instance.Header(">>> [INTEGRITY] Проверка целостности критических файлов");
            var report = IntegrityChecker.CheckCriticalFiles(ScanScope.CriticalOnly, (c, t, name) =>
            {
                Logger.Instance.Progress(c, t, name);
            });
            Console.WriteLine();
            IntegrityChecker.PrintReport(report);
        }

        /// <summary>Только DISM CheckHealth + ScanHealth + RestoreHealth (без cleanup/SFC).</summary>
        public static void RunDismCheckScanRestore(bool includeCleanup)
        {
            IntPtr session = IntPtr.Zero;
            string logPath = Logger.Instance.LogFilePath + ".csr.log";

            try
            {
                DismNativeApi.DismInitialize(DismLogLevel.Verbose, logPath, null);
                session = DismNativeApi.DismOpenSession(null, null, null);

                Logger.Instance.Info("DismCheckImageHealth...");
                Logger.Instance.Info("Результат: " + DismNativeApi.RunCheckHealth(session));

                Logger.Instance.Info("DismScanImageHealth — это может занять 5-15 мин...");
                DismNativeApi.RunScanHealth(session);
                Logger.Instance.Success("ScanHealth завершён");

                Logger.Instance.Info("DismRestoreImageHealth — это может занять 10-30 мин...");
                int lastPct = -1;
                DismNativeApi.RunRestoreHealth(session, (current, total, _) =>
                {
                    if (total > 0 && (int)(current * 100L / total) != lastPct)
                    {
                        lastPct = (int)(current * 100L / total);
                        Logger.Instance.Progress((int)current, (int)total, "RestoreHealth");
                    }
                });
                Console.WriteLine();
                Logger.Instance.Success("RestoreHealth завершён");

                if (includeCleanup)
                {
                    lastPct = -1;
                    Logger.Instance.Info("DismStartComponentCleanup...");
                    DismNativeApi.RunComponentCleanup(session, false, (current, total, _) =>
                    {
                        if (total > 0 && (int)(current * 100L / total) != lastPct)
                        {
                            lastPct = (int)(current * 100L / total);
                            Logger.Instance.Progress((int)current, (int)total, "Cleanup");
                        }
                    });
                    Console.WriteLine();
                    Logger.Instance.Success("Cleanup завершён");
                }
            }
            finally
            {
                try { if (session != IntPtr.Zero) DismNativeApi.DismCloseSession(session); } catch { }
                try { DismNativeApi.DismShutdown(); } catch { }
            }
        }

        // ---------------------------------------------------------------------
        //  ОТДЕЛЬНЫЕ ОПЕРАЦИИ DISM
        // ---------------------------------------------------------------------

        public static void RunCheckHealth()
        {
            IntPtr session = OpenSessionOrThrow();
            try
            {
                Logger.Instance.Info("DismCheckImageHealth (CheckHealth)...");
                string result = DismNativeApi.RunCheckHealth(session);
                Logger.Instance.Info($"Результат: {result}");
            }
            finally { CloseSession(session); }
        }

        public static void RunScanHealth()
        {
            IntPtr session = OpenSessionOrThrow();
            try
            {
                Logger.Instance.Info("DismScanImageHealth (ScanHealth) — это может занять 5–15 минут...");
                DismNativeApi.RunScanHealth(session);
                Logger.Instance.Success("ScanHealth завершён");
            }
            finally { CloseSession(session); }
        }

        public static void RunRestoreHealth()
        {
            IntPtr session = OpenSessionOrThrow();
            try
            {
                Logger.Instance.Info("DismRestoreImageHealth (RestoreHealth) — это может занять 10–30 минут...");
                int lastPct = -1;
                DismNativeApi.RunRestoreHealth(session, (current, total, _) =>
                {
                    if (total > 0 && (int)(current * 100L / total) != lastPct)
                    {
                        lastPct = (int)(current * 100L / total);
                        Logger.Instance.Progress((int)current, (int)total, "RestoreHealth");
                    }
                });
                Console.WriteLine();
                Logger.Instance.Success("RestoreHealth завершён");
            }
            finally { CloseSession(session); }
        }

        public static void RunStartComponentCleanup(bool resetBase)
        {
            IntPtr session = OpenSessionOrThrow();
            try
            {
                Logger.Instance.Info($"DismStartComponentCleanup (ResetBase={resetBase})...");
                int lastPct = -1;
                DismNativeApi.RunComponentCleanup(session, resetBase, (current, total, _) =>
                {
                    if (total > 0 && (int)(current * 100L / total) != lastPct)
                    {
                        lastPct = (int)(current * 100L / total);
                        Logger.Instance.Progress((int)current, (int)total, "Cleanup");
                    }
                });
                Console.WriteLine();
                Logger.Instance.Success("StartComponentCleanup завершён");
            }
            finally { CloseSession(session); }
        }

        /// <summary>SFC /ScanNow через нативный API sfc_os.dll.</summary>
        public static void RunSfcScanNow(SfcScanType type = SfcScanType.ScanAndRepair)
        {
            Logger.Instance.Info($"SfcSynchronousScan (sfc_os.dll), режим = {type}...");
            bool ok = SfcNativeApi.RunSfcScan(type);
            if (ok) Logger.Instance.Success("SFC-сканирование завершено");
            else
            {
                Logger.Instance.Warn("Нативный SfcSynchronousScan не сработал.");
                Logger.Instance.Warn("На некоторых сборках Win10 функция требует доп. флагов инициализации.");
            }
        }

        /// <summary>Проверка защиты критических файлов через SFC API.</summary>
        public static void CheckProtectedFiles()
        {
            Logger.Instance.Info("Перебор SfcIsFileProtected для критических файлов...");
            int prot = 0, unprot = 0;
            foreach (var f in IntegrityChecker.CriticalFiles)
            {
                if (!System.IO.File.Exists(f)) continue;
                bool isP = SfcNativeApi.IsFileProtected(f);
                if (isP) { prot++; Logger.Instance.Success($"  [WFP+] {f}"); }
                else     { unprot++; Logger.Instance.Warn($"  [WFP-] {f}"); }
            }
            Logger.Instance.Info($"Итого: {prot} под защитой WFP, {unprot} без защиты");
        }

        /// <summary>Проверка подписей критических файлов через WinTrust API.</summary>
        public static void VerifySignatures()
        {
            Logger.Instance.Info("WinVerifyTrust для критических файлов...");
            int valid = 0, missing = 0, bad = 0, thirdParty = 0;
            foreach (var f in IntegrityChecker.CriticalFiles)
            {
                if (!System.IO.File.Exists(f)) continue;
                var sig = SignatureVerifier.Verify(f);
                if (sig.IsValid && sig.IsMicrosoft)
                {
                    valid++;
                    Logger.Instance.Success($"  [OK] {f}  ({sig.Publisher})");
                }
                else if (!sig.IsSigned)
                {
                    missing++;
                    Logger.Instance.Error($"  [NO-SIG] {f}  — подпись отсутствует!");
                }
                else if (sig.IsSigned && !sig.IsMicrosoft)
                {
                    thirdParty++;
                    Logger.Instance.Error($"  [3RDPARTY] {f}  — подписан: {sig.Publisher}");
                }
                else
                {
                    bad++;
                    Logger.Instance.Error($"  [BAD] {f}  — {sig.Error}");
                }
            }
            Logger.Instance.Info($"Итого: валидных Microsoft={valid}, без подписи={missing}, плохих={bad}, сторонних={thirdParty}");
        }

        // ---------------------------------------------------------------------
        //  Вспомогательные
        // ---------------------------------------------------------------------

        private static IntPtr OpenSessionOrThrow()
        {
            string logPath = Logger.Instance.LogFilePath + ".native.dism.log";
            DismNativeApi.DismInitialize(DismLogLevel.Verbose, logPath, null);
            var s = DismNativeApi.DismOpenSession(null, null, null);
            Logger.Instance.Success("DISM API: инициализирован, Online-сессия открыта");
            return s;
        }

        private static void CloseSession(IntPtr session)
        {
            try { if (session != IntPtr.Zero) DismNativeApi.DismCloseSession(session); } catch { }
            try { DismNativeApi.DismShutdown(); } catch { }
        }

        private static void TryCleanup(IntPtr session)
        {
            try { if (session != IntPtr.Zero) DismNativeApi.DismCloseSession(session); } catch { }
            try { DismNativeApi.DismShutdown(); } catch { }
        }
    }
}
