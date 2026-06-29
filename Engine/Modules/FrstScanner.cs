using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  FRST-STYLE SCANNER  (Farbar Recovery Scan Tool inspired)
    //  Комплексный сканер системы в стиле FRST. Включает:
    //    • Список процессов с подписями и MD5
    //    • Службы и драйверы (включая stopped)
    //    • Автозагрузка (Run keys, Startup folder, Scheduled Tasks)
    //    • Записи реестра Winlogon, AppInit_DLLs, Image File Execution Options
    //    • Файл hosts + DNS-кэш
    //    • Установленные программы
    //    • Подключения TCP/UDP
    //    • Браузерные расширения (IE/Edge/Chrome/Firefox)
    //    • Ярлыки .lnk с анализом целей
    //    • WMI-запросы
    //    • Брандмауэр Windows
    //    • Прокси-настройки
    //    • Подозрительные файлы в нестандартных местах
    // ===========================================================================

    public static class FrstScanner
    {
        // ---------------------------------------------------------------------
        //  ГЛАВНЫЙ ОТЧЁТ — аналог FRST.txt
        // ---------------------------------------------------------------------

        public static void RunFullScan()
        {
            Logger.Instance.Header(">>> FRST-STYLE ПОЛНЫЙ СКАН СИСТЕМЫ");

            var sw = Stopwatch.StartNew();
            string reportPath = Path.Combine(
                Path.GetDirectoryName(Logger.Instance.LogFilePath),
                $"FRST_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            using var writer = new StreamWriter(reportPath, false, Encoding.UTF8);

            void W(string s = "")
            {
                writer.WriteLine(s);
                Logger.Instance.Raw(s);
            }

            W("═══════════════════════════════════════════════════════════════════════");
            W($"  FRST-Style Scan Report — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            W($"  Machine: {Environment.MachineName}  User: {Environment.UserName}");
            W($"  OS: {Environment.OSVersion.VersionString}");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");

            // 1. Processes
            W("═══════════════════════════════════════════════════════════════════════");
            W("  1. RUNNING PROCESSES (с подписями и MD5)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanProcesses(W);
            W("");

            // 2. Services + Drivers
            W("═══════════════════════════════════════════════════════════════════════");
            W("  2. SERVICES & DRIVERS (включая остановленные)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanServices(W);
            W("");

            // 3. Startup entries
            W("═══════════════════════════════════════════════════════════════════════");
            W("  3. STARTUP ENTRIES (Run keys + Startup folder)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanStartup(W);
            W("");

            // 4. Winlogon + AppInit_DLLs + IFEO
            W("═══════════════════════════════════════════════════════════════════════");
            W("  4. WINLOGON / AppInit_DLLs / Image File Execution Options");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanWinlogonAndHijacks(W);
            W("");

            // 5. Scheduled Tasks
            W("═══════════════════════════════════════════════════════════════════════");
            W("  5. SCHEDULED TASKS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanScheduledTasks(W);
            W("");

            // 6. Hosts file
            W("═══════════════════════════════════════════════════════════════════════");
            W("  6. HOSTS FILE");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanHostsFile(W);
            W("");

            // 7. TCP/UDP Connections
            W("═══════════════════════════════════════════════════════════════════════");
            W("  7. TCP/UDP CONNECTIONS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanNetworkConnections(W);
            W("");

            // 8. Installed Programs
            W("═══════════════════════════════════════════════════════════════════════");
            W("  8. INSTALLED PROGRAMS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanInstalledPrograms(W);
            W("");

            // 9. Browser Add-ons
            W("═══════════════════════════════════════════════════════════════════════");
            W("  9. BROWSER ADD-ONS (Chrome/Edge/Firefox/IE)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanBrowserAddons(W);
            W("");

            // 10. Shortcuts
            W("═══════════════════════════════════════════════════════════════════════");
            W("  10. SHORTCUTS (.lnk) — проверка целей");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanShortcuts(W);
            W("");

            // 11. Firewall Rules
            W("═══════════════════════════════════════════════════════════════════════");
            W("  11. FIREWALL RULES");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanFirewallRules(W);
            W("");

            // 12. Proxy settings
            W("═══════════════════════════════════════════════════════════════════════");
            W("  12. PROXY SETTINGS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanProxySettings(W);
            W("");

            // 13. Suspicious files
            W("═══════════════════════════════════════════════════════════════════════");
            W("  13. SUSPICIOUS FILES (Temp, AppData, non-standard locations)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanSuspiciousFiles(W);
            W("");

            // 14. DNS Cache
            W("═══════════════════════════════════════════════════════════════════════");
            W("  14. DNS CACHE");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanDnsCache(W);
            W("");

            // 15. WMI Quick Queries
            W("═══════════════════════════════════════════════════════════════════════");
            W("  15. WMI QUICK QUERIES");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanWmi(W);
            W("");

            // 16. Registry Run keys deep
            W("═══════════════════════════════════════════════════════════════════════");
            W("  16. REGISTRY RUN KEYS (deep)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanRegistryRunKeysDeep(W);
            W("");

            // 17. Browser Search Providers
            W("═══════════════════════════════════════════════════════════════════════");
            W("  17. BROWSER SEARCH PROVIDERS / HOMEPAGE");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanBrowserSettings(W);
            W("");

            // 18. Mounted devices & drives
            W("═══════════════════════════════════════════════════════════════════════");
            W("  18. MOUNTED DEVICES & DRIVE LETTERS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanMountedDevices(W);
            W("");

            // 19. System Restore points
            W("═══════════════════════════════════════════════════════════════════════");
            W("  19. SYSTEM RESTORE POINTS");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanRestorePoints(W);
            W("");

            // 20. Installed Updates (Hotfixes)
            W("═══════════════════════════════════════════════════════════════════════");
            W("  20. INSTALLED UPDATES (HOTFIXES)");
            W("═══════════════════════════════════════════════════════════════════════");
            W("");
            ScanHotfixes(W);
            W("");

            sw.Stop();
            W("═══════════════════════════════════════════════════════════════════════");
            W($"  SCAN COMPLETE — {sw.Elapsed.TotalSeconds:F1} sec");
            W($"  Report saved: {reportPath}");
            W("═══════════════════════════════════════════════════════════════════════");

            Logger.Instance.Success($"Отчёт FRST сохранён: {reportPath}");
        }

        // =====================================================================
        //  1. ПРОЦЕССЫ
        // =====================================================================

        private static void ScanProcesses(Action<string> W)
        {
            try
            {
                var procs = Process.GetProcesses();
                W($"Всего процессов: {procs.Length}");
                W("");
                W($"{"PID",-8} {"Name",-30} {"Path",-60} {"Publisher",-25} MD5");
                W(new string('-', 160));

                foreach (var p in procs.OrderBy(x => x.ProcessName))
                {
                    try
                    {
                        string path = "";
                        try { path = p.MainModule?.FileName ?? ""; }
                        catch { /* 64-bit process from 32-bit program */ }

                        string publisher = "";
                        string md5 = "";
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            var sig = SignatureVerifier.Verify(path);
                            publisher = sig.Publisher ?? "";
                            md5 = ComputeMd5(path);
                        }

                        W($"{p.Id,-8} {Trunc(p.ProcessName, 30),-30} {Trunc(path, 60),-60} {Trunc(publisher, 25),-25} {md5}");
                    }
                    catch (Exception ex)
                    {
                        W($"{p.Id,-8} {Trunc(p.ProcessName, 30),-30} (access denied: {ex.Message})");
                    }
                }
            }
            catch (Exception ex)
            {
                W($"Ошибка сканирования процессов: {ex.Message}");
            }
        }

        // =====================================================================
        //  2. СЛУЖБЫ И ДРАЙВЕРЫ
        // =====================================================================

        private static void ScanServices(Action<string> W)
        {
            try
            {
                var scQuery = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_Service");
                int count = 0;
                W($"{"Name",-30} {"Display",-40} {"State",-12} {"StartMode",-10} {"Path",-60}");
                W(new string('-', 160));
                foreach (System.Management.ManagementObject svc in scQuery.Get())
                {
                    count++;
                    string name = svc["Name"]?.ToString() ?? "";
                    string display = svc["DisplayName"]?.ToString() ?? "";
                    string state = svc["State"]?.ToString() ?? "";
                    string start = svc["StartMode"]?.ToString() ?? "";
                    string path = svc["PathName"]?.ToString() ?? "";
                    W($"{Trunc(name, 30),-30} {Trunc(display, 40),-40} {state,-12} {start,-10} {Trunc(path, 60),-60}");
                }
                W("");
                W($"Всего служб: {count}");

                // Драйверы
                W("");
                W("--- ДРАЙВЕРЫ (kernel/system) ---");
                var drvQuery = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_SystemDriver");
                int drvCount = 0;
                foreach (System.Management.ManagementObject drv in drvQuery.Get())
                {
                    drvCount++;
                    if (drv["State"]?.ToString() == "Running")
                    {
                        string name = drv["Name"]?.ToString() ?? "";
                        string path = drv["PathName"]?.ToString() ?? "";
                        string desc = drv["Description"]?.ToString() ?? "";
                        W($"  {Trunc(name, 25),-25} {Trunc(path, 70),-70} {desc}");
                    }
                }
                W($"Всего драйверов (running): {drvCount}");
            }
            catch (Exception ex)
            {
                W($"Ошибка сканирования служб: {ex.Message}");
            }
        }

        // =====================================================================
        //  3. АВТОЗАГРУЗКА
        // =====================================================================

        private static void ScanStartup(Action<string> W)
        {
            string[] runKeys =
            {
                @"HKLM\Software\Microsoft\Windows\CurrentVersion\Run",
                @"HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce",
                @"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce",
                @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Run",
                @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            W("--- Run keys (registry) ---");
            foreach (var keyPath in runKeys)
            {
                W("");
                W($"[{keyPath}]");
                try
                {
                    var (hive, subkey) = ParseRegPath(keyPath);
                    using var key = hive.OpenSubKey(subkey);
                    if (key == null) { W("  (ключ не существует)"); continue; }
                    foreach (var name in key.GetValueNames())
                    {
                        var val = key.GetValue(name)?.ToString() ?? "";
                        W($"  {name} = {val}");
                    }
                }
                catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
            }

            W("");
            W("--- Startup folder ---");
            string[] startupFolders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup")
            };
            foreach (var folder in startupFolders.Distinct())
            {
                W("");
                W($"[{folder}]");
                if (!Directory.Exists(folder)) { W("  (папка не существует)"); continue; }
                foreach (var f in Directory.EnumerateFiles(folder))
                    W($"  {Path.GetFileName(f)}  ({new FileInfo(f).Length} bytes)");
            }
        }

        // =====================================================================
        //  4. WINLOGON / AppInit_DLLs / IFEO
        // =====================================================================

        private static void ScanWinlogonAndHijacks(Action<string> W)
        {
            // Winlogon
            W("--- HKLM\\...\\Winlogon ---");
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                if (key != null)
                {
                    foreach (var name in new[] { "Shell", "Userinit", "AppInit_DLLs", "UIHost", "LegalNoticeText", "Taskman", "VmApplet" })
                    {
                        var val = key.GetValue(name);
                        if (val != null) W($"  {name} = {val}");
                    }
                }
            }
            catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }

            // AppInit_DLLs (x86 + x64)
            W("");
            W("--- AppInit_DLLs ---");
            string[] appInitPaths =
            {
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows"
            };
            foreach (var p in appInitPaths)
            {
                W($"[{p}]");
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                    if (key != null)
                    {
                        var appinit = key.GetValue("AppInit_DLLs");
                        var load = key.GetValue("LoadAppInit_DLLs");
                        W($"  AppInit_DLLs = {appinit ?? "(empty)"}");
                        W($"  LoadAppInit_DLLs = {load ?? "(empty)"}");
                    }
                }
                catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
            }

            // Image File Execution Options (взлом отладчика)
            W("");
            W("--- Image File Execution Options (Debugger hijack check) ---");
            try
            {
                using var ifeo = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options");
                if (ifeo != null)
                {
                    int hijackCount = 0;
                    foreach (var exeName in ifeo.GetSubKeyNames())
                    {
                        using var sub = ifeo.OpenSubKey(exeName);
                        var dbg = sub?.GetValue("Debugger");
                        if (dbg != null && !string.IsNullOrEmpty(dbg.ToString()))
                        {
                            hijackCount++;
                            W($"  ⚠ {exeName} -> Debugger = {dbg}");
                        }
                    }
                    if (hijackCount == 0) W("  ✓ Нет записей Debugger (хорошо)");
                    else W($"  ⚠ Обнаружено {hijackCount} потенциальных IFEO-взломов!");
                }
            }
            catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  5. ЗАПЛАНИРОВАННЫЕ ЗАДАЧИ
        // =====================================================================

        private static void ScanScheduledTasks(Action<string> W)
        {
            try
            {
                // Через COM Schedule.Service
                dynamic sched = Activator.CreateInstance(
                    Type.GetTypeFromProgID("Schedule.Service"));
                sched.Connect();
                var root = sched.GetFolder("\\");

                int count = 0;
                W($"{"Task",-40} {"State",-15} {"LastRun",-22} {"NextRun",-22} Path");
                W(new string('-', 160));
                EnumerateTasks(root, W, ref count);
                W("");
                W($"Всего задач: {count}");
            }
            catch (Exception ex)
            {
                W($"Ошибка: {ex.Message}");
                W("Альтернатива: schtasks /query /fo LIST /v");
            }
        }

        private static void EnumerateTasks(dynamic folder, Action<string> W, ref int count)
        {
            try
            {
                var tasks = folder.GetTasks(0); // 0 = include hidden
                foreach (var t in tasks)
                {
                    count++;
                    string name = t.Name;
                    string path = t.Path;
                    int state = t.State; // 0=Unknown, 1=Disabled, 2=Queued, 3=Ready, 4=Running
                    string stateStr = state switch
                    {
                        0 => "Unknown", 1 => "Disabled", 2 => "Queued",
                        3 => "Ready", 4 => "Running", _ => $"?{state}"
                    };
                    string last = "?";
                    string next = "?";
                    try { last = t.LastRunTime.ToString("yyyy-MM-dd HH:mm"); } catch { }
                    try { next = t.NextRunTime.ToString("yyyy-MM-dd HH:mm"); } catch { }
                    W($"{Trunc(name, 40),-40} {stateStr,-15} {last,-22} {next,-22} {path}");
                }
                // Subfolders
                foreach (var sub in folder.GetFolders(0))
                    EnumerateTasks(sub, W, ref count);
            }
            catch { /* ignore */ }
        }

        // =====================================================================
        //  6. ФАЙЛ HOSTS
        // =====================================================================

        private static void ScanHostsFile(Action<string> W)
        {
            string hostsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

            W($"Файл: {hostsPath}");
            if (!File.Exists(hostsPath)) { W("  ⚠ Файл hosts не найден!"); return; }

            try
            {
                var info = new FileInfo(hostsPath);
                W($"Размер: {info.Length} байт, изменён: {info.LastWriteTime}");
                W("");
                W("--- Содержимое (без комментариев и пустых строк) ---");

                int entryCount = 0;
                int suspectCount = 0;
                foreach (var line in File.ReadAllLines(hostsPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                    entryCount++;
                    W($"  {trimmed}");

                    // Подозрительно: редирект известных доменов
                    if (trimmed.Contains("microsoft.com") ||
                        trimmed.Contains("windowsupdate.com") ||
                        trimmed.Contains("google.com") ||
                        trimmed.Contains("facebook.com"))
                    {
                        suspectCount++;
                    }
                }

                W("");
                W($"Всего записей: {entryCount}");
                if (suspectCount > 0)
                    W($"⚠ Подозрительных редиректов: {suspectCount}!");
                else
                    W("✓ Подозрительных редиректов не обнаружено");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  7. СЕТЕВЫЕ ПОДКЛЮЧЕНИЯ
        // =====================================================================

        private static void ScanNetworkConnections(Action<string> W)
        {
            try
            {
                W($"{"Protocol",-8} {"Local",-25} {"Remote",-25} {"State",-12} PID  Process");
                W(new string('-', 120));

                var props = IPGlobalProperties.GetIPGlobalProperties();
                var conns = props.GetActiveTcpConnections();
                foreach (var c in conns)
                {
                    string procName = "?";
                    try
                    {
                        using var p = Process.GetProcessById(c.GetHashCode() > 0 ? 0 : 0);
                    }
                    catch { }
                    W($"{"TCP",-8} {c.LocalEndPoint,-25} {c.RemoteEndPoint,-25} {c.State,-12}      (active)");
                }

                var listeners = props.GetActiveTcpListeners();
                foreach (var ep in listeners)
                    W($"{"TCP-L",-8} {ep,-25} {"*:*",-25} {"LISTENING",-12}");

                var udpListeners = props.GetActiveUdpListeners();
                foreach (var ep in udpListeners)
                    W($"{"UDP-L",-8} {ep,-25} {"*:*",-25} {"LISTENING",-12}");

                W("");
                W($"Активных TCP-соединений: {conns.Length}");
                W($"Прослушиваемых TCP-портов: {listeners.Length}");
                W($"Прослушиваемых UDP-портов: {udpListeners.Length}");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  8. УСТАНОВЛЕННЫЕ ПРОГРАММЫ
        // =====================================================================

        private static void ScanInstalledPrograms(Action<string> W)
        {
            string[] uninstallKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            int count = 0;
            foreach (var basePath in uninstallKeys)
            {
                using var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(basePath);
                if (baseKey == null) continue;
                foreach (var subName in baseKey.GetSubKeyNames())
                {
                    using var sub = baseKey.OpenSubKey(subName);
                    var name = sub?.GetValue("DisplayName")?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    var publisher = sub?.GetValue("Publisher")?.ToString() ?? "";
                    var version = sub?.GetValue("DisplayVersion")?.ToString() ?? "";
                    var date = sub?.GetValue("InstallDate")?.ToString() ?? "";
                    var loc = sub?.GetValue("InstallLocation")?.ToString() ?? "";
                    count++;
                    W($"  {Trunc(name, 45),-45} {Trunc(version, 12),-12} {Trunc(publisher, 25),-25} {date} {loc}");
                }
            }
            W("");
            W($"Всего установленных программ: {count}");
        }

        // =====================================================================
        //  9. БРАУЗЕРНЫЕ РАСШИРЕНИЯ
        // =====================================================================

        private static void ScanBrowserAddons(Action<string> W)
        {
            // Chrome
            ScanChromeExtensions(W, "Chrome",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data"));

            // Edge
            ScanChromeExtensions(W, "Edge",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Edge", "User Data"));

            // Firefox
            ScanFirefoxAddons(W);

            // IE BHO
            ScanIEBHO(W);
        }

        private static void ScanChromeExtensions(Action<string> W, string browser, string userDataDir)
        {
            W($"--- {browser} extensions ---");
            if (!Directory.Exists(userDataDir)) { W("  (профиль не найден)"); W(""); return; }
            try
            {
                foreach (var profileDir in Directory.GetDirectories(userDataDir, "Profile*").Concat(
                    new[] { Path.Combine(userDataDir, "Default") }))
                {
                    var extDir = Path.Combine(profileDir, "Extensions");
                    if (!Directory.Exists(extDir)) continue;
                    W($"Профиль: {Path.GetFileName(profileDir)}");
                    foreach (var extId in Directory.GetDirectories(extDir))
                    {
                        var id = Path.GetFileName(extId);
                        // Получаем имя из manifest.json последней версии
                        string name = id;
                        try
                        {
                            var versions = Directory.GetDirectories(extId);
                            if (versions.Length > 0)
                            {
                                var manifest = Path.Combine(versions[0], "manifest.json");
                                if (File.Exists(manifest))
                                {
                                    var json = File.ReadAllText(manifest);
                                    var m = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
                                    if (m.Success) name = m.Groups[1].Value;
                                }
                            }
                        }
                        catch { }
                        W($"  {id,-35} {name}");
                    }
                }
            }
            catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
            W("");
        }

        private static void ScanFirefoxAddons(Action<string> W)
        {
            W("--- Firefox extensions ---");
            var mozillaPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox");
            if (!Directory.Exists(mozillaPath)) { W("  (Firefox не установлен)"); W(""); return; }

            var profilesIni = Path.Combine(mozillaPath, "profiles.ini");
            if (File.Exists(profilesIni))
            {
                var lines = File.ReadAllLines(profilesIni);
                string path = "";
                foreach (var line in lines)
                {
                    if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                        path = line.Substring(5).Replace("/", "\\");
                }
                if (!string.IsNullOrEmpty(path))
                {
                    var profileDir = Path.Combine(mozillaPath, path);
                    var extDir = Path.Combine(profileDir, "extensions");
                    if (Directory.Exists(extDir))
                    {
                        foreach (var f in Directory.EnumerateFiles(extDir, "*.xpi"))
                            W($"  {Path.GetFileName(f)}");
                    }
                }
            }
            W("");
        }

        private static void ScanIEBHO(Action<string> W)
        {
            W("--- Internet Explorer BHOs ---");
            string[] bhoPaths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects"
            };
            foreach (var p in bhoPaths)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                if (key == null) continue;
                foreach (var clsid in key.GetSubKeyNames())
                {
                    W($"  CLSID: {clsid}");
                }
            }
            W("");
        }

        // =====================================================================
        //  10. ЯРЛЫКИ (.lnk)
        // =====================================================================

        private static void ScanShortcuts(Action<string> W)
        {
            string[] locations =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Internet Explorer", "Quick Launch", "User Pinned", "TaskBar")
            };

            int checkedCount = 0, brokenCount = 0, suspiciousCount = 0;
            foreach (var loc in locations.Distinct().Where(Directory.Exists))
            {
                foreach (var lnk in Directory.EnumerateFiles(loc, "*.lnk", SearchOption.AllDirectories))
                {
                    checkedCount++;
                    try
                    {
                        var target = ResolveShortcut(lnk);
                        if (string.IsNullOrEmpty(target))
                        {
                            brokenCount++;
                            W($"  ⚠ BROKEN: {lnk}");
                        }
                        else if (!File.Exists(target) && !Directory.Exists(target))
                        {
                            brokenCount++;
                            W($"  ⚠ TARGET MISSING: {lnk} → {target}");
                        }
                        else
                        {
                            // Подозрительно: цель в Temp/AppData
                            if (target.Contains("\\Temp\\") || target.Contains("\\AppData\\Local\\Temp\\"))
                            {
                                suspiciousCount++;
                                W($"  ⚠ SUSPICIOUS (Temp): {lnk} → {target}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        W($"  Ошибка {lnk}: {ex.Message}");
                    }
                }
            }
            W("");
            W($"Проверено ярлыков: {checkedCount}");
            W($"Сломанных: {brokenCount}");
            W($"Подозрительных (цель в Temp): {suspiciousCount}");
        }

        private static string ResolveShortcut(string lnkPath)
        {
            try
            {
                // Через WScript.Shell COM
                dynamic shell = Activator.CreateInstance(
                    Type.GetTypeFromProgID("WScript.Shell"));
                dynamic sc = shell.CreateShortcut(lnkPath);
                return sc.TargetPath;
            }
            catch { return ""; }
        }

        // =====================================================================
        //  11. ПРАВИЛА БРАНДМАУЭРА
        // =====================================================================

        private static void ScanFirewallRules(Action<string> W)
        {
            try
            {
                // Через COM HNetCfg.FwPolicy2
                dynamic policy = Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                var rules = policy.Rules;

                int count = 0;
                int enabledCount = 0;
                int blockCount = 0;
                foreach (var rule in rules)
                {
                    count++;
                    if (!rule.Enabled) continue;
                    enabledCount++;
                    if (rule.Action == 0) // 0 = block
                    {
                        blockCount++;
                        W($"  BLOCK  {rule.Name,-40} {rule.ApplicationName ?? "(any)"}");
                    }
                }
                W("");
                W($"Всего правил: {count}");
                W($"Включено: {enabledCount}");
                W($"Блокирующих: {blockCount}");
            }
            catch (Exception ex)
            {
                W($"Ошибка: {ex.Message}");
                W("Альтернатива: netsh advfirewall firewall show rule name=all");
            }
        }

        // =====================================================================
        //  12. ПРОКСИ-НАСТРОЙКИ
        // =====================================================================

        private static void ScanProxySettings(Action<string> W)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
                if (key != null)
                {
                    W("HKCU\\...\\Internet Settings:");
                    foreach (var name in new[] { "ProxyEnable", "ProxyServer", "ProxyOverride", "AutoConfigURL", "AutoConfigProxy" })
                    {
                        var v = key.GetValue(name);
                        if (v != null) W($"  {name} = {v}");
                    }
                }

                // WinHTTP proxy
                W("");
                W("WinHTTP proxy:");
                int ec = ConsoleHelper.RunExternal("netsh.exe", "winhttp show proxy", out var so, out _);
                if (ec == 0)
                    foreach (var line in so.Split('\n').Take(10))
                        W($"  {line.TrimEnd()}");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  13. ПОДОЗРИТЕЛЬНЫЕ ФАЙЛЫ
        // =====================================================================

        private static void ScanSuspiciousFiles(Action<string> W)
        {
            string[] suspectDirs =
            {
                Path.Combine(Path.GetTempPath()),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
            };

            string[] suspectExts = { ".exe", ".dll", ".bat", ".cmd", ".vbs", ".js", ".ps1", ".scr" };
            int total = 0;

            foreach (var dir in suspectDirs.Distinct())
            {
                if (!Directory.Exists(dir)) continue;
                W($"Сканирование: {dir}");
                try
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        if (!suspectExts.Contains(ext)) continue;
                        var info = new FileInfo(f);
                        total++;
                        var sig = SignatureVerifier.Verify(f);
                        string pub = sig.IsMicrosoft ? "MS" :
                                     sig.IsValid ? Trunc(sig.Publisher ?? "?", 25) :
                                     !sig.IsSigned ? "NO-SIG" : "BAD";
                        W($"  {Path.GetFileName(f),-35} {info.Length,12} bytes  {info.LastWriteTime:yyyy-MM-dd}  {pub}");
                    }
                }
                catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
                W("");
            }
            W($"Подозрительных файлов всего: {total}");
        }

        // =====================================================================
        //  14. DNS КЭШ
        // =====================================================================

        private static void ScanDnsCache(Action<string> W)
        {
            try
            {
                int ec = ConsoleHelper.RunExternal("ipconfig.exe", "/displaydns", out var so, out _);
                if (ec == 0)
                {
                    var lines = so.Split('\n');
                    int entries = 0;
                    foreach (var line in lines)
                    {
                        var t = line.Trim();
                        if (t.StartsWith("Запись") || t.StartsWith("Record") || t.Contains("---"))
                        {
                            entries++;
                            if (entries <= 50) W($"  {t}");
                        }
                    }
                    W("");
                    W($"Всего DNS-записей в кэше: {entries}");
                }
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  15. WMI ЗАПРОСЫ
        // =====================================================================

        private static void ScanWmi(Action<string> W)
        {
            WmiQuery(W, "Win32_OperatingSystem", new[] { "Caption", "Version", "BuildNumber", "OSArchitecture", "InstallDate", "LastBootUpTime", "SerialNumber" });
            WmiQuery(W, "Win32_ComputerSystem", new[] { "Manufacturer", "Model", "TotalPhysicalMemory", "NumberOfProcessors", "NumberOfLogicalProcessors", "Domain" });
            WmiQuery(W, "Win32_Processor", new[] { "Name", "Manufacturer", "MaxClockSpeed", "NumberOfCores", "NumberOfLogicalProcessors" });
            WmiQuery(W, "Win32_BaseBoard", new[] { "Manufacturer", "Product", "SerialNumber", "Version" });
            WmiQuery(W, "Win32_BIOS", new[] { "Manufacturer", "Name", "Version", "SerialNumber", "ReleaseDate" });
            WmiQuery(W, "Win32_DiskDrive", new[] { "Model", "Size", "InterfaceType", "SerialNumber", "FirmwareRevision" });
            WmiQuery(W, "Win32_LogicalDisk", new[] { "DeviceID", "FileSystem", "Size", "FreeSpace", "VolumeName", "DriveType" });
            WmiQuery(W, "Win32_NetworkAdapter", new[] { "Name", "Manufacturer", "MACAddress", "NetConnectionID", "Speed" }, "WHERE NetConnectionID IS NOT NULL");
            WmiQuery(W, "Win32_VideoController", new[] { "Name", "DriverVersion", "AdapterRAM", "VideoProcessor" });
            WmiQuery(W, "Win32_SoundDevice", new[] { "Name", "Manufacturer", "Status" });
            WmiQuery(W, "Win32_PointingDevice", new[] { "Name", "Manufacturer", "HardwareType" });
            WmiQuery(W, "Win32_Keyboard", new[] { "Name", "Description", "DeviceID" });
        }

        private static void WmiQuery(Action<string> W, string className, string[] fields, string where = "")
        {
            W("");
            W($"--- {className} ---");
            try
            {
                var query = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM {className} {where}");
                foreach (System.Management.ManagementObject obj in query.Get())
                {
                    foreach (var f in fields)
                    {
                        var v = obj[f];
                        if (v != null) W($"  {f,-30} = {v}");
                    }
                    W("");
                }
            }
            catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  16. РЕЕСТР RUN KEYS (DEEP)
        // =====================================================================

        private static void ScanRegistryRunKeysDeep(Action<string> W)
        {
            // Userinit / Shell (должны быть стандартными)
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                if (key != null)
                {
                    var ui = key.GetValue("Userinit")?.ToString();
                    var sh = key.GetValue("Shell")?.ToString();
                    W($"Userinit = {ui}");
                    W($"Shell = {sh}");
                    if (ui != null && ui.ToLowerInvariant() != @"c:\windows\system32\userinit.exe,")
                        W($"  ⚠ Подозрительное значение Userinit!");
                    if (sh != null && sh.ToLowerInvariant() != "explorer.exe")
                        W($"  ⚠ Подозрительное значение Shell!");
                }
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }

            // Explorer Run keys
            W("");
            W("--- Explorer\\Run ---");
            string[] explorerRuns =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellCmdHere",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconHiddenIdentifiers"
            };
            foreach (var p in explorerRuns)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                if (key == null) continue;
                W($"[{p}]");
                foreach (var name in key.GetValueNames())
                    W($"  {name} = {key.GetValue(name)}");
                foreach (var sub in key.GetSubKeyNames())
                    W($"  \\{sub}");
            }

            // Policies Explorer
            W("");
            W("--- Policies\\Explorer\\Run ---");
            string[] policyPaths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
            };
            foreach (var p in policyPaths)
            {
                using var key1 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                using var key2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(p);
                W($"[HKLM\\{p}]");
                if (key1 != null) foreach (var n in key1.GetValueNames()) W($"  {n} = {key1.GetValue(n)}");
                W($"[HKCU\\{p}]");
                if (key2 != null) foreach (var n in key2.GetValueNames()) W($"  {n} = {key2.GetValue(n)}");
            }
        }

        // =====================================================================
        //  17. ПОИСКОВЫЕ ПРОВАЙДЕРЫ БРАУЗЕРОВ
        // =====================================================================

        private static void ScanBrowserSettings(Action<string> W)
        {
            // Edge / Chrome homepage & search
            var chrome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Default", "Preferences");
            var edge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default", "Preferences");

            foreach (var (browser, file) in new[] { ("Chrome", chrome), ("Edge", edge) })
            {
                W($"--- {browser} settings ---");
                if (!File.Exists(file)) { W("  (не найдено)"); W(""); continue; }
                try
                {
                    var json = File.ReadAllText(file);
                    var homeMatch = Regex.Match(json, @"""homepage""\s*:\s*""([^""]+)""");
                    var searchMatch = Regex.Match(json, @"""default_search_provider""\s*:\s*\{[^}]*""name""\s*:\s*""([^""]+)""");
                    W($"  Homepage: {homeMatch.Groups[1].Value}");
                    W($"  Default search: {searchMatch.Groups[1].Value}");
                }
                catch (Exception ex) { W($"  Ошибка: {ex.Message}"); }
                W("");
            }
        }

        // =====================================================================
        //  18. СМОНТИРОВАННЫЕ УСТРОЙСТВА
        // =====================================================================

        private static void ScanMountedDevices(Action<string> W)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\MountedDevices");
                if (key == null) { W("MountedDevices не найден"); return; }
                int count = 0;
                foreach (var name in key.GetValueNames())
                {
                    count++;
                    var val = key.GetValue(name);
                    if (val is byte[] bytes)
                        W($"  {name,-60} ({bytes.Length} bytes)");
                    else
                        W($"  {name,-60} = {val}");
                }
                W("");
                W($"Всего записей: {count}");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }

            // USBSTOR
            W("");
            W("--- USB Storage history ---");
            try
            {
                using var usb = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum\USBSTOR");
                if (usb != null)
                {
                    foreach (var type in usb.GetSubKeyNames())
                        W($"  {type}");
                }
            }
            catch { }
        }

        // =====================================================================
        //  19. ТОЧКИ ВОССТАНОВЛЕНИЯ
        // =====================================================================

        private static void ScanRestorePoints(Action<string> W)
        {
            try
            {
                var scope = new System.Management.ManagementScope(@"\\.\root\default");
                var query = new System.Management.ObjectQuery("SELECT * FROM SystemRestore");
                using var searcher = new System.Management.ManagementObjectSearcher(scope, query);
                int count = 0;
                foreach (System.Management.ManagementObject rp in searcher.Get())
                {
                    count++;
                    var dt = ManagementDateTimeToDateTime(rp["CreationTime"]?.ToString());
                    W($"  [{rp["SequenceNumber"]}] {dt:yyyy-MM-dd HH:mm}  {rp["Description"]}");
                }
                W("");
                W($"Всего точек восстановления: {count}");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        private static DateTime ManagementDateTimeToDateTime(string dt)
        {
            if (string.IsNullOrEmpty(dt)) return DateTime.MinValue;
            try
            {
                // Format: yyyymmddHHMMSS.mmmmmm+UUU
                return new DateTime(
                    int.Parse(dt.Substring(0, 4)),
                    int.Parse(dt.Substring(4, 2)),
                    int.Parse(dt.Substring(6, 2)),
                    int.Parse(dt.Substring(8, 2)),
                    int.Parse(dt.Substring(10, 2)),
                    int.Parse(dt.Substring(12, 2)));
            }
            catch { return DateTime.MinValue; }
        }

        // =====================================================================
        //  20. УСТАНОВЛЕННЫЕ ОБНОВЛЕНИЯ (HOTFIXES)
        // =====================================================================

        private static void ScanHotfixes(Action<string> W)
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
                int count = 0;
                foreach (System.Management.ManagementObject hf in q.Get())
                {
                    count++;
                    W($"  {hf["HotFixID"],-15} {hf["Description"]?.ToString() ?? "",-30} {hf["InstalledOn"]}");
                }
                W("");
                W($"Всего установленных обновлений: {count}");
            }
            catch (Exception ex) { W($"Ошибка: {ex.Message}"); }
        }

        // =====================================================================
        //  УТИЛИТЫ
        // =====================================================================

        private static string ComputeMd5(string path)
        {
            try
            {
                using var md5 = MD5.Create();
                using var fs = File.OpenRead(path);
                var hash = md5.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch { return "?"; }
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 3) + "...";
        }

        private static (Microsoft.Win32.RegistryKey hive, string subkey) ParseRegPath(string path)
        {
            if (path.StartsWith(@"HKLM\"))
                return (Microsoft.Win32.Registry.LocalMachine, path.Substring(5));
            if (path.StartsWith(@"HKCU\"))
                return (Microsoft.Win32.Registry.CurrentUser, path.Substring(5));
            if (path.StartsWith(@"HKCR\"))
                return (Microsoft.Win32.Registry.ClassesRoot, path.Substring(5));
            if (path.StartsWith(@"HKU\"))
                return (Microsoft.Win32.Registry.Users, path.Substring(4));
            if (path.StartsWith(@"HKCC\"))
                return (Microsoft.Win32.Registry.CurrentConfig, path.Substring(5));
            throw new FormatException($"Неизвестный hive: {path}");
        }
    }
}
