using System;
using System.IO;
using System.Linq;
using SystemRestoreTool.Api;
using SystemRestoreTool.Engine;
using SystemRestoreTool.Engine.Modules;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool
{
    // ===========================================================================
    //  SYSTEM RESTORE TOOL  v2.0  —  ГЛАВНАЯ ПРОГРАММА
    //  Восстановление системных файлов Windows 10/11 через ПРЯМЫЕ Windows API:
    //
    //    • dismapi.dll   — DISM (Check/Scan/Restore/Cleanup)
    //    • sfc.dll       — SFC (проверка защиты файлов)
    //    • sfc_os.dll    — SfcSynchronousScan (= Sfc /ScanNow)
    //    • wintrust.dll  — проверка подписей Authenticode
    //    • crypt32.dll   — извлечение издателя подписи
    //    • srclient.dll  — точки восстановления системы
    //    • advapi32.dll  — Service Control Manager, реестр, привилегии
    //    • kernel32.dll  — файловые операции, перезагрузка, модули
    //    • wevtapi.dll   — журналы событий
    //    • vssapi.dll    — Volume Shadow Copy
    //    • Microsoft.Dism NuGet — управляемая обёртка
    //
    //  Модули:
    //    • SystemRestorePointManager — точки восстановления
    //    • ServicesRepairManager     — перезапуск критических служб
    //    • BootRecoveryManager       — BCD, загрузчик
    //    • RegistryRestoreManager    — бэкап/восстановление реестра
    //    • WindowsUpdateRepairManager — сброс WU
    //    • WinSxsRepairManager       — хранилище компонентов
    //    • EventLogManager           — журналы событий
    //    • UserEnvRestoreManager     — профили пользователей
    //    • FileHashDatabaseManager   — база хешей для сравнения
    // ===========================================================================

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Windows System Recovery Tool 2.5";
            PrintBanner();

            // Обработка --help / -h / /?
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?" || args[0] == "help"))
            {
                PrintHelp();
                return;
            }

            // Обработка --version
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                Console.WriteLine("Windows System Recovery Tool v2.5.0");
                Console.WriteLine("  Built with .NET 6, C# 10");
                Console.WriteLine("  Repository: https://github.com/Ludonist/Windows-System-Recovery-Tool");
                Console.WriteLine("  License: MIT");
                return;
            }

            // Проверка ОС
            if (!ConsoleHelper.IsWindows10OrLater())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("║   ⚠  НЕПОДДЕРЖИВАЕМАЯ ОПЕРАЦИОННАЯ СИСТЕМА                           ║");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("║   Эта программа работает только на Windows 10 (build 19041+) или      ║");
                Console.WriteLine("║   Windows 11. Текущая система не соответствует требованиям.           ║");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                ConsoleHelper.WaitForKey();
                return;
            }

            // Проверка прав администратора
            if (!ConsoleHelper.IsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("║   ⚠  ТРЕБУЮТСЯ ПРАВА АДМИНИСТРАТОРА                                  ║");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("║   DISM, SFC, реестр и управление службами работают только с          ║");
                Console.WriteLine("║   повышенными правами. Пожалуйста, запустите программу от имени      ║");
                Console.WriteLine("║   администратора:                                                    ║");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("║   1) Правый клик на SystemRestoreTool.exe                            ║");
                Console.WriteLine("║   2) Выберите «Запуск от имени администратора»                       ║");
                Console.WriteLine("║                                                                      ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();
                ConsoleHelper.WaitForKey();
                return;
            }
            Logger.Instance.Success("Права администратора: ПОДТВЕРЖДЕНЫ");
            Logger.Instance.Info($"Лог запуска: {Logger.Instance.LogFilePath}");

            // Авто-режим (через аргумент --super-full / --full / --verify)
            if (args.Length > 0)
            {
                if (HandleAutoMode(args))
                {
                    Logger.Instance.Info("");
                    Logger.Instance.Info("=== АВТО-РЕЖИМ ЗАВЕРШЁН ===");
                    Logger.Instance.Info($"Полный лог сохранён: {Logger.Instance.LogFilePath}");
                    ConsoleHelper.WaitForKey();
                    return;
                }
            }

            InteractiveMenu();
        }

        // ---------------------------------------------------------------------
        //  СПРАВКА (--help)
        // ---------------------------------------------------------------------

        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
Windows System Recovery Tool v2.5 — справка
============================================

ИСПОЛЬЗОВАНИЕ:
    SystemRestoreTool.exe [команда]

КОМАНДЫ:
    (без аргументов)          Запуск в интерактивном режиме (меню из 66 пунктов)

    --super-full              СУПЕР-полный цикл:
                                1) Создание точки восстановления
                                2) DISM CheckHealth + ScanHealth + RestoreHealth
                                3) StartComponentCleanup
                                4) Sfc /ScanNow (нативный sfc_os.dll)
                                5) Проверка целостности 40+ критических файлов
                                6) Перезапуск 40+ критических служб
                                7) Восстановление Windows Update
                                8) Очистка кэшей (иконки, шрифты)

    --super-full --reset-base То же + /ResetBase WinSxS (необратимо!)

    --full                    Полный DISM + SFC цикл:
                                CheckHealth + ScanHealth + RestoreHealth
                                + StartComponentCleanup + Sfc /ScanNow

    --verify                  Проверка 40+ критических файлов
                                (WFP + подпись + отчёт)

    --scan-all                Глубокая проверка System32 + drivers
                                (~5000 файлов, 10-30 минут)

    --help, -h, /?            Эта справка
    --version, -v             Версия программы

ПРИМЕРЫ:
    :: Самый полный цикл с созданием точки восстановления
    SystemRestoreTool.exe --super-full

    :: Только проверка, без изменений
    SystemRestoreTool.exe --verify

ЛОГИРОВАНИЕ:
    Лог сохраняется в:
    %LOCALAPPDATA%\SystemRestoreTool\srt_YYYYMMDD_HHMMSS.log

ССЫЛКИ:
    Repository:  https://github.com/Ludonist/Windows-System-Recovery-Tool
    Issues:      https://github.com/Ludonist/Windows-System-Recovery-Tool/issues
    Releases:    https://github.com/Ludonist/Windows-System-Recovery-Tool/releases
    License:     MIT
");
            Console.ResetColor();
        }

        // ---------------------------------------------------------------------
        //  АВТО-РЕЖИМ
        // ---------------------------------------------------------------------

        private static bool HandleAutoMode(string[] args)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--super-full":
                case "/super-full":
                    Logger.Instance.Header(">>> АВТО: СУПЕР-ПОЛНЫЙ ЦИКЛ");
                    SystemRestoreEngine.RunSuperFullCycle(
                        createRestorePoint: true,
                        resetBase: args.Contains("--reset-base"));
                    return true;

                case "--full":
                case "/full":
                    Logger.Instance.Header(">>> АВТО: полный цикл (нативный API)");
                    SystemRestoreEngine.RunFullNativeCycle(true, false);
                    return true;

                case "--verify":
                case "/verify":
                    Logger.Instance.Header(">>> АВТО: проверка целостности");
                    var report = IntegrityChecker.CheckCriticalFiles(ScanScope.CriticalOnly,
                        (c, t, n) => Logger.Instance.Progress(c, t, n));
                    Console.WriteLine();
                    IntegrityChecker.PrintReport(report);
                    return true;

                case "--scan-all":
                case "/scan-all":
                    Logger.Instance.Header(">>> АВТО: глубокая проверка System32 + drivers");
                    var rep = IntegrityChecker.CheckCriticalFiles(ScanScope.System32AndDrivers,
                        (c, t, n) => Logger.Instance.Progress(c, t, n));
                    Console.WriteLine();
                    IntegrityChecker.PrintReport(rep);
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------------
        //  ИНТЕРАКТИВНОЕ МЕНЮ
        // ---------------------------------------------------------------------

        private static void InteractiveMenu()
        {
            while (true)
            {
                ConsoleHelper.WriteHeader("ГЛАВНОЕ МЕНЮ — System Restore Tool v2.0");

                var items = new[]
                {
                    // === СУПЕР-ЦИКЛЫ ===
                    "★ СУПЕР-ПОЛНЫЙ ЦИКЛ: точка восст. + DISM + SFC + службы + WU + кэши",
                    "  ПОЛНЫЙ ЦИКЛ (нативный API): CheckHealth+ScanHealth+RestoreHealth+Cleanup+SFC",
                    "  ПОЛНЫЙ ЦИКЛ (Microsoft.Dism NuGet): тот же цикл, через NuGet-пакет",
                    "",
                    // === DISM ===
                    "  DISM /Online /Cleanup-Image /CheckHealth        (нативный API)",
                    "  DISM /Online /Cleanup-Image /ScanHealth         (нативный API)",
                    "  DISM /Online /Cleanup-Image /RestoreHealth      (нативный API)",
                    "  DISM /Online /Cleanup-Image /StartComponentCleanup",
                    "  DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                    "",
                    // === SFC ===
                    "  Sfc /ScanNow                                     (нативный sfc_os.dll)",
                    "  Sfc /VerifyOnly                                  (нативный sfc_os.dll)",
                    "",
                    // === ПРОВЕРКА ЦЕЛОСТНОСТИ ===
                    "  Проверка защиты критических файлов (SfcIsFileProtected)",
                    "  Проверка подписей критических файлов (WinVerifyTrust + издатель)",
                    "  ★ ПРОВЕРКА 40+ КРИТИЧЕСКИХ ФАЙЛОВ (WFP + подпись + отчёт)",
                    "  ★ ГЛУБОКАЯ ПРОВЕРКА System32 + drivers (~5000 файлов)",
                    "  ★ ПОЛНАЯ ПРОВЕРКА System32 + SysWOW64 + drivers (~10000 файлов)",
                    "  Перечислить ВСЕ защищённые файлы (SfcGetNextProtectedFile)",
                    "",
                    // === ТОЧКИ ВОССТАНОВЛЕНИЯ ===
                    "  Создать точку восстановления (srclient.dll)",
                    "  Включить System Restore через реестр",
                    "",
                    // === СЛУЖБЫ ===
                    "  Перезапустить все критические службы Windows (40+ служб)",
                    "  Запустить все критические службы (без остановки)",
                    "  Состояние критических служб",
                    "",
                    // === РЕЕСТР ===
                    "  Бэкап всех кустов реестра (RegSaveKey + копирование файлов)",
                    "  Проверка папки RegBack (старые резервные копии)",
                    "",
                    // === ЗАГРУЗЧИК ===
                    "  Проверка загрузочных файлов Windows (BCD, bootmgr, winload)",
                    "  Восстановление BCD (экспорт + проверка хранилища)",
                    "",
                    // === WINDOWS UPDATE ===
                    "  Полное восстановление Windows Update (5 этапов)",
                    "  Очистка очереди BITS",
                    "  Проверка CBS.log на ошибки",
                    "",
                    // === WINSXS ===
                    "  Анализ хранилища компонентов (DismAnalyzeComponentStore)",
                    "  Полная очистка WinSxS (StartComponentCleanup)",
                    "  Проверка манифестов в WinSxS\\Manifests",
                    "",
                    // === ЖУРНАЛЫ СОБЫТИЙ ===
                    "  Бэкап всех журналов событий (wevtapi.dll)",
                    "  Очистка журналов с резервным копированием",
                    "  Размеры файлов журналов событий",
                    "",
                    // === ПРОФИЛИ + КЭШИ ===
                    "  Проверка профилей пользователей (NTUSER.DAT)",
                    "  Перестроение кэша иконок (IconCache.db)",
                    "  Перестроение кэша шрифтов (FNTCACHE.DAT)",
                    "  Проверка DLL пользовательского окружения",
                    "",
                    // === БАЗА ХЕШЕЙ ===
                    "  Создать базу хешей System32 (для последующего сравнения)",
                    "  Сравнить текущие файлы с базой хешей",
                    "",
                    // === УСТРОЙСТВА (SetupAPI) ===
                    "  ★ Список всех устройств (SetupAPI)",
                    "  ★ Проверка драйверов устройств (подписи)",
                    "  Сканирование аппаратных изменений (CM_Reenumerate_DevNode)",
                    "",
                    // === ПИТАНИЕ (powrprof.dll) ===
                    "  ★ Информация о питании (схема, батарея, возможности)",
                    "  Установить схему: Высокая производительность",
                    "  Установить схему: Сбалансированная",
                    "  Установить схему: Энергосбережение",
                    "  Восстановить схемы питания по умолчанию (powercfg)",
                    "  Включить файл гибернации (powercfg /hibernate on)",
                    "  Отключить файл гибернации (powercfg /hibernate off)",
                    "",
                    // === СЕТЬ (WinHTTP/WinINet/Winsock) ===
                    "  ★ Проверка доступности серверов Windows Update (WinHTTP)",
                    "  ★ Сброс сетевого стека (Winsock + TCP/IP + DNS + Firewall)",
                    "  Показать конфигурацию сети (ipconfig /all)",
                    "  Сброс WinHTTP-прокси (netsh winhttp reset proxy)",
                    "",
                    // === WINDOWS ERROR REPORTING (wer.dll) ===
                    "  ★ Статистика Windows Error Reporting (WER)",
                    "  ★ Последние сбои приложений (из WER-архива)",
                    "  Очистить очередь и архив WER-отчётов",
                    "  Включить Windows Error Reporting",
                    "  Отключить Windows Error Reporting",
                    "",
                    // === WUA COM API (Windows Update Agent) ===
                    "  ★ Статус Windows Update (COM WUA API + история)",
                    "  Поиск доступных обновлений (через COM API)",
                    "  Настройки автоматического обновления (AU)",
                    "",
                    // === УТИЛИТЫ ===
                    "  Открыть папку с логами",
                    "  Перезагрузить компьютер (через Win32 API)",
                    "  Выход"
                };

                // Фильтруем пустые разделители для нумерации
                var visibleItems = items.Where(s => s.Length > 0).ToArray();
                for (int i = 0; i < visibleItems.Length; i++)
                {
                    if (visibleItems[i].StartsWith("★"))
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (visibleItems[i].StartsWith("  "))
                        Console.ForegroundColor = ConsoleColor.Gray;
                    else
                        Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine($"  [{i + 1,2}] {visibleItems[i].TrimStart(' ', '★')}");
                }
                Console.ResetColor();

                int choice = ConsoleHelper.ReadInt("\nВыберите действие: ", 1, visibleItems.Length, 1);

                try
                {
                    RunChoice(choice);
                    if (choice == visibleItems.Length) return; // Выход
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Ошибка выполнения: {ex.Message}");
                    Logger.Instance.Error($"Стек: {ex.StackTrace}");
                }

                ConsoleHelper.WaitForKey();
            }
        }

        // ---------------------------------------------------------------------
        //  ВЫПОЛНЕНИЕ ВЫБРАННОГО ПУНКТА МЕНЮ
        // ---------------------------------------------------------------------

        private static void RunChoice(int n)
        {
            switch (n)
            {
                // === СУПЕР-ЦИКЛЫ ===
                case 1:
                    bool rp = ConsoleHelper.ReadYesNo("Создать точку восстановления перед началом?", true);
                    bool rb = ConsoleHelper.ReadYesNo("Применить /ResetBase (неотменимо!)?", false);
                    SystemRestoreEngine.RunSuperFullCycle(rp, rb);
                    break;
                case 2:
                    bool cleanup2 = ConsoleHelper.ReadYesNo("Запустить StartComponentCleanup?", true);
                    bool reset2 = cleanup2 && ConsoleHelper.ReadYesNo("Применить /ResetBase?", false);
                    SystemRestoreEngine.RunFullNativeCycle(cleanup2, reset2);
                    break;
                case 3:
                    bool mCleanup = ConsoleHelper.ReadYesNo("Запустить Cleanup?", true);
                    bool mReset = mCleanup && ConsoleHelper.ReadYesNo("Применить /ResetBase?", false);
                    DismManagedWrapper.RunFullRepair(mCleanup, mReset);
                    break;

                // === DISM ===
                case 4:  SystemRestoreEngine.RunCheckHealth();              break;
                case 5:  SystemRestoreEngine.RunScanHealth();               break;
                case 6:  SystemRestoreEngine.RunRestoreHealth();            break;
                case 7:  SystemRestoreEngine.RunStartComponentCleanup(false); break;
                case 8:  SystemRestoreEngine.RunStartComponentCleanup(true);  break;

                // === SFC ===
                case 9:  SystemRestoreEngine.RunSfcScanNow(SfcScanType.ScanAndRepair); break;
                case 10: SystemRestoreEngine.RunSfcScanNow(SfcScanType.VerifyOnly);    break;

                // === ПРОВЕРКА ЦЕЛОСТНОСТИ ===
                case 11: SystemRestoreEngine.CheckProtectedFiles();         break;
                case 12: SystemRestoreEngine.VerifySignatures();            break;
                case 13:
                    {
                        var report = IntegrityChecker.CheckCriticalFiles(ScanScope.CriticalOnly,
                            (c, t, name) => Logger.Instance.Progress(c, t, name));
                        Console.WriteLine();
                        IntegrityChecker.PrintReport(report);
                        break;
                    }
                case 14:
                    {
                        Logger.Instance.Warn("Глубокая проверка займёт 10-30 минут...");
                        var report = IntegrityChecker.CheckCriticalFiles(ScanScope.System32AndDrivers,
                            (c, t, name) => Logger.Instance.Progress(c, t, name));
                        Console.WriteLine();
                        IntegrityChecker.PrintReport(report);
                        break;
                    }
                case 15:
                    {
                        Logger.Instance.Warn("Полная проверка займёт 30-60 минут...");
                        var report = IntegrityChecker.CheckCriticalFiles(ScanScope.Full,
                            (c, t, name) => Logger.Instance.Progress(c, t, name));
                        Console.WriteLine();
                        IntegrityChecker.PrintReport(report);
                        break;
                    }
                case 16: EnumerateAllProtectedFiles();                     break;

                // === ТОЧКИ ВОССТАНОВЛЕНИЯ ===
                case 17:
                    {
                        Console.Write("Описание точки: ");
                        string desc = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(desc)) desc = "System Restore Tool";
                        SystemRestorePointManager.CreateRestorePoint(desc);
                        break;
                    }
                case 18: SystemRestorePointManager.EnableSystemRestore();  break;

                // === СЛУЖБЫ ===
                case 19: ServicesRepairManager.RestartAllCriticalServices(); break;
                case 20: ServicesRepairManager.StartAllCriticalServices(); break;
                case 21: ServicesRepairManager.PrintCriticalServicesStatus(); break;

                // === РЕЕСТР ===
                case 22: RegistryRestoreManager.BackupAllHives();          break;
                case 23: RegistryRestoreManager.CheckRegBackFolder();      break;

                // === ЗАГРУЗЧИК ===
                case 24: BootRecoveryManager.CheckBootFiles();             break;
                case 25: BootRecoveryManager.RebuildBootConfig();          break;
                case 26: BootRecoveryManager.CheckWindowsBootLoader();     break;

                // === WINDOWS UPDATE ===
                case 27: WindowsUpdateRepairManager.FullRepair();          break;
                case 28: WindowsUpdateRepairManager.ClearBitsQueue();      break;
                case 29: WindowsUpdateRepairManager.CheckCbsLogs();        break;

                // === WINSXS ===
                case 30: WinSxsRepairManager.AnalyzeComponentStore();      break;
                case 31: WinSxsRepairManager.FullCleanup(false);           break;
                case 32: WinSxsRepairManager.CheckManifests();             break;

                // === ЖУРНАЛЫ СОБЫТИЙ ===
                case 33: EventLogManager.BackupAllLogs();                  break;
                case 34: EventLogManager.ClearAllLogsWithBackup();         break;
                case 35: EventLogManager.CheckLogSizes();                  break;

                // === ПРОФИЛИ + КЭШИ ===
                case 36: UserEnvRestoreManager.CheckUserProfiles();        break;
                case 37: UserEnvRestoreManager.RebuildIconCache();         break;
                case 38: UserEnvRestoreManager.RebuildFontCache();         break;
                case 39: UserEnvRestoreManager.CheckUserProfileDlls();     break;

                // === БАЗА ХЕШЕЙ ===
                case 40: CreateHashDatabase();                             break;
                case 41: CompareHashDatabase();                            break;

                // === УСТРОЙСТВА (SetupAPI) ===
                case 42: DeviceManager.ListAllDevices();                   break;
                case 43: DeviceManager.CheckDeviceDrivers();               break;
                case 44: DeviceManager.ScanForHardwareChanges();           break;

                // === ПИТАНИЕ (powrprof.dll) ===
                case 45: PowerOptionsManager.ShowPowerInfo();              break;
                case 46: PowerOptionsManager.SetHighPerformance();         break;
                case 47: PowerOptionsManager.SetBalanced();                break;
                case 48: PowerOptionsManager.SetPowerSaver();              break;
                case 49: PowerOptionsManager.RestoreDefaultPowerSchemes(); break;
                case 50: PowerOptionsManager.ToggleHibernate(true);        break;
                case 51: PowerOptionsManager.ToggleHibernate(false);       break;

                // === СЕТЬ (WinHTTP/WinINet/Winsock) ===
                case 52: NetworkDiagnosticManager.CheckWindowsUpdateConnectivity(); break;
                case 53: NetworkDiagnosticManager.ResetNetworkStack();     break;
                case 54: NetworkDiagnosticManager.ShowNetworkConfig();     break;
                case 55: NetworkDiagnosticManager.ResetWinHttpProxy();     break;

                // === WINDOWS ERROR REPORTING (wer.dll) ===
                case 56: WerManager.ShowWerStatistics();                   break;
                case 57: WerManager.ShowRecentCrashes();                   break;
                case 58: WerManager.ClearWerReports();                     break;
                case 59: WerManager.EnableWer();                           break;
                case 60: WerManager.DisableWer();                          break;

                // === WUA COM API (Windows Update Agent) ===
                case 61: WindowsUpdateAgentManager.CheckUpdateStatus();    break;
                case 62: WindowsUpdateAgentManager.SearchForUpdates();     break;
                case 63: WindowsUpdateAgentManager.CheckAutomaticUpdates(); break;

                // === УТИЛИТЫ ===
                case 64: OpenLogsFolder();                                 break;
                case 65: RebootSystem();                                   break;
                case 66: return; // Выход
            }
        }

        // ---------------------------------------------------------------------
        //  ВСПОМОГАТЕЛЬНЫЕ КОМАНДЫ
        // ---------------------------------------------------------------------

        private static void EnumerateAllProtectedFiles()
        {
            Logger.Instance.Info("Перечисление всех защищённых файлов через SfcGetNextProtectedFile...");
            bool show = ConsoleHelper.ReadYesNo("Выводить имена на экран?", false);

            var list = IntegrityChecker.EnumerateProtectedFiles((n, name) =>
            {
                Logger.Instance.Info($"  ...{n} файлов перечислено (последний: {name})");
            });

            Logger.Instance.Success($"Всего защищённых файлов: {list.Count}");
            if (show)
            {
                foreach (var f in list)
                    Logger.Instance.Raw("    " + f);
            }
        }

        private static void CreateHashDatabase()
        {
            Console.Write("Путь для сохранения базы (Enter = по умолчанию): ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SystemRestoreTool", $"HashDB_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }

            Logger.Instance.Info($"Создание базы хешей в: {path}");
            Logger.Instance.Warn("Это займёт 5-15 минут (для System32)...");

            FileHashDatabaseManager.CreateDatabase(path, FileHashDatabaseManager.DefaultSnapshotDirs,
                (c, _, n) =>
                {
                    if (c % 100 == 0)
                        Logger.Instance.Info($"  Обработано файлов: {c} (последний: {n})");
                });
        }

        private static void CompareHashDatabase()
        {
            Console.Write("Путь к файлу базы хешей: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Logger.Instance.Error("Файл базы не указан или не существует.");
                return;
            }

            FileHashDatabaseManager.CompareWithDatabase(path,
                (c, t, n) =>
                {
                    if (c % 100 == 0)
                        Logger.Instance.Info($"  Проверено {c} файлов (последний: {n})");
                });
        }

        private static void OpenLogsFolder()
        {
            var dir = Path.GetDirectoryName(Logger.Instance.LogFilePath);
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
                Logger.Instance.Success($"Открыта папка: {dir}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Не удалось открыть папку: {ex.Message}");
                Logger.Instance.Info($"Путь: {dir}");
            }
        }

        private static void RebootSystem()
        {
            if (!ConsoleHelper.ReadYesNo("Точно перезагрузить компьютер СЕЙЧАС?", false))
                return;

            Logger.Instance.Info("Перезагрузка через Win32 API (ExitWindowsEx)...");
            bool ok = Kernel32NativeApi.RebootSystem();
            if (!ok)
                Logger.Instance.Error("Не удалось перезагрузить — проверьте права администратора.");
        }

        // ---------------------------------------------------------------------
        //  БАННЕР
        // ---------------------------------------------------------------------

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                                                      ║");
            Console.WriteLine("║   WINDOWS SYSTEM RECOVERY TOOL  v2.5.0                               ║");
            Console.WriteLine("║   Прямое восстановление системных файлов Windows 10/11               ║");
            Console.WriteLine("║                                                                      ║");
            Console.WriteLine("║   ✦ 17 нативных Win32 API через P/Invoke                            ║");
            Console.WriteLine("║   ✦ 66 операций восстановления и диагностики                        ║");
            Console.WriteLine("║   ✦ Проверка подмены файлов через WinVerifyTrust                    ║");
            Console.WriteLine("║   ✦ Microsoft.Dism NuGet + COM WUA API                              ║");
            Console.WriteLine("║                                                                      ║");
            Console.WriteLine("║   API (https://learn.microsoft.com/windows/win32/apiindex):         ║");
            Console.WriteLine("║   • dismapi.dll  • sfc.dll/sfc_os.dll  • wintrust.dll               ║");
            Console.WriteLine("║   • crypt32.dll  • srclient.dll       • advapi32.dll (SCM)          ║");
            Console.WriteLine("║   • kernel32.dll • wevtapi.dll        • vssapi.dll                  ║");
            Console.WriteLine("║   • wer.dll      • setupapi.dll       • powrprof.dll                ║");
            Console.WriteLine("║   • winhttp.dll  • wininet.dll        • ws2_32.dll                  ║");
            Console.WriteLine("║                                                                      ║");
            Console.WriteLine("║   Repository: github.com/Ludonist/Windows-System-Recovery-Tool      ║");
            Console.WriteLine("║   License: MIT                                                       ║");
            Console.WriteLine("║                                                                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
