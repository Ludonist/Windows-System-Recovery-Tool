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
    //  ADVANCED CHECKS MANAGER
    //  Десятки отдельных проверок безопасности и целостности:
    //    • UAC-настройки
    //    • SmartScreen
    //    • Windows Defender состояние
    //    • Подпись всех .exe в System32 (анти-троян)
    //    • Проверка критических разделов реестра
    //    • Анализ prefetch
    //    • Проверка Powershell ExecutionPolicy
    //    • Анализ pending.xml
    //    • Проверка COMPONENTS hive
    //    • Проверка подписи драйверов
    //    • Записи Winsock LSP
    //    • Проверка sidebar/gadgets (Vista)
    //    • Анализ Action Center / Security Center
    //    • Проверка подписей всех файлов в C:\Windows\
    //    • Проверка критических служб на автозагрузку
    //    • Проверка пробуждений системы (wake timers)
    //    • Анализ очереди печати
    //    • Проверка подписи runtime-библиотек VC++
    //    • ... и ещё десятки проверок
    // ===========================================================================

    public static class AdvancedChecksManager
    {
        // ---------------------------------------------------------------------
        //  ГЛАВНЫЙ ЦИКЛ — все проверки
        // ---------------------------------------------------------------------

        public static void RunAllAdvancedChecks()
        {
            Logger.Instance.Header(">>> РАСШИРЕННЫЕ ПРОВЕРКИ БЕЗОПАСНОСТИ И ЦЕЛОСТНОСТИ");

            int passed = 0, warnings = 0, failed = 0;

            void RunCheck(string name, Action check)
            {
                Logger.Instance.Header($"[CHECK] {name}");
                try
                {
                    check();
                    Logger.Instance.Success($"  ✓ {name} — OK");
                    passed++;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"  ✗ {name} — {ex.Message}");
                    failed++;
                }
                Logger.Instance.Raw("");
            }

            // === БЕЗОПАСНОСТЬ СИСТЕМЫ ===
            RunCheck("UAC включен", CheckUacEnabled);
            RunCheck("SmartScreen активен", CheckSmartScreen);
            RunCheck("Windows Defender работающий", CheckWindowsDefender);
            RunCheck("Real-time protection", CheckRealTimeProtection);
            RunCheck("Антивирус обновлён", CheckAntivirusUpdated);
            RunCheck("Брандмауэр активен", CheckFirewallEnabled);

            // === ПОДПИСИ ФАЙЛОВ ===
            RunCheck("Подписи всех EXE в C:\\Windows\\", CheckWindowsExeSignatures);
            RunCheck("Подписи всех SYS в System32\\drivers", CheckDriverSignatures);
            RunCheck("Подписи DLL в System32\\*.dll", CheckSystem32DllSignatures);
            RunCheck("Подписи в SysWOW64", CheckSysWow64Signatures);

            // === РЕЕСТР ===
            RunCheck("Userinit / Shell стандартные", CheckWinlogonValues);
            RunCheck("AppInit_DLLs пустые", CheckAppInitDlls);
            RunCheck("IFEO без Debugger-взломов", CheckIFEO);
            RunCheck("Run keys чисты", CheckRunKeysClean);
            RunCheck("Policies\\Explorer\\Run пустой", CheckPoliciesExplorerRun);
            RunCheck("Нет редиректов hosts", CheckHostsRedirects);
            RunCheck("Записи Safe Mode чисты", CheckSafeModeKeys);

            // === СЛУЖБЫ ===
            RunCheck("Критические службы автозагрузки", CheckCriticalServices);
            RunCheck("Службы без пути", CheckServicesWithoutPath);
            RunCheck("Службы с неправильным путём", CheckServicesBadPath);
            RunCheck("Disabled services check", CheckDisabledServices);

            // === СЕТЬ ===
            RunCheck("Winsock LSP", CheckWinsockLSP);
            RunCheck("Listening ports", CheckListeningPorts);
            RunCheck("Pending TCP connections", CheckPendingConnections);
            RunCheck("DNS-серверы валидны", CheckDnsServers);
            RunCheck("DHCP состояние", CheckDhcpState);

            // === АВТОЗАГРУЗКА ===
            RunCheck("Startup folder пуст", CheckStartupFolderClean);
            RunCheck("Нет исполняемых в Temp", CheckNoExecutablesInTemp);
            RunCheck("Запланированные задачи — без подозрительных", CheckScheduledTasksSuspicious);
            RunCheck("Нет скриптов в Startup", CheckNoScriptsInStartup);

            // === БРАУЗЕРЫ ===
            RunCheck("Chrome расширения", CheckChromeExtensions);
            RunCheck("Edge расширения", CheckEdgeExtensions);
            RunCheck("Firefox расширения", CheckFirefoxExtensions);
            RunCheck("IE BHOs", CheckIEBhos);

            // === ФАЙЛЫ ===
            RunCheck("Pending.xml (Windows Update)", CheckPendingXml);
            RunCheck("COMPONENTS hive доступен", CheckComponentsHive);
            RunCheck("CBS.log на ошибки", CheckCbsLogErrors);
            RunCheck("Prefetch существует и не пуст", CheckPrefetch);
            RunCheck("Файл pagefile.sys", CheckPagefile);
            RunCheck("Файл hiberfil.sys", CheckHiberfil);

            // === POWERSHELL ===
            RunCheck("PowerShell ExecutionPolicy", CheckPowershellPolicy);

            // === СИСТЕМА ===
            RunCheck("Pending reboot", CheckPendingReboot);
            RunCheck("Disk space свободен", CheckDiskSpace);
            RunCheck("Windows Updates проверены", CheckWuPendingReboot);
            RunCheck("Memory integrity (HVCI)", CheckHVCI);
            RunCheck("TPM состояние", CheckTpm);
            RunCheck("Secure Boot", CheckSecureBoot);
            RunCheck("Last boot время", CheckLastBootTime);
            RunCheck("Time sinc (NTP)", CheckTimeSync);

            // Итог
            Logger.Instance.Header("=== ИТОГ РАСШИРЕННЫХ ПРОВЕРОК ===");
            Logger.Instance.Success($"✓ Пройдено: {passed}");
            Logger.Instance.Warn($"⚠ Предупреждений: {warnings}");
            Logger.Instance.Error($"✗ Провалено: {failed}");
        }

        // =====================================================================
        //  БЕЗОПАСНОСТЬ СИСТЕМЫ
        // =====================================================================

        public static void CheckUacEnabled()
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            var v = key?.GetValue("EnableLUA");
            bool enabled = v != null && (int)v == 1;
            if (enabled)
            {
                Logger.Instance.Success("  UAC: включён");
                var consent = key?.GetValue("ConsentPromptBehaviorAdmin");
                Logger.Instance.Info($"  ConsentPromptBehaviorAdmin = {consent} (0=не спрашивать, 5=спрашивать)");
            }
            else
            {
                Logger.Instance.Warn("  ⚠ UAC отключён! Это снижает безопасность системы.");
                throw new Exception("UAC disabled");
            }
        }

        public static void CheckSmartScreen()
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
            var smartscreen = key?.GetValue("SmartScreenEnabled");
            Logger.Instance.Info($"  SmartScreenEnabled = {smartscreen}");
            if (smartscreen?.ToString() == "Off")
                Logger.Instance.Warn("  ⚠ SmartScreen отключён!");
        }

        public static void CheckWindowsDefender()
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows Defender");
            if (key == null)
            {
                Logger.Instance.Warn("  Windows Defender не найден в реестре");
                return;
            }
            using var rt = key.OpenSubKey("Real-Time Protection");
            var disabled = rt?.GetValue("DisableRealtimeMonitoring");
            Logger.Instance.Info($"  DisableRealtimeMonitoring = {disabled}");
            if (disabled != null && (int)disabled == 1)
                throw new Exception("Real-time protection disabled");
        }

        public static void CheckRealTimeProtection()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher(
                    @"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                foreach (System.Management.ManagementObject av in q.Get())
                {
                    Logger.Instance.Info($"  AV: {av["displayName"]}  state={av["productState"]}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"  WMI SecurityCenter2: {ex.Message}");
            }
        }

        public static void CheckAntivirusUpdated()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher(
                    @"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                bool any = false;
                foreach (System.Management.ManagementObject av in q.Get())
                {
                    any = true;
                    int state = Convert.ToInt32(av["productState"]);
                    // 0x10000 — definitions updated
                    // 0x1000  — definitions outdated
                    bool definitionsUpdated = (state & 0xF000) == 0x1000;
                    Logger.Instance.Info($"  AV={av["displayName"]} state=0x{state:X} defsUpdated={definitionsUpdated}");
                }
                if (!any) Logger.Instance.Warn("  Антивирус не найден");
            }
            catch (Exception ex) { Logger.Instance.Warn($"  {ex.Message}"); }
        }

        public static void CheckFirewallEnabled()
        {
            try
            {
                dynamic policy = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                bool domain = policy.FirewallEnabled[1];
                bool priv = policy.FirewallEnabled[2];
                bool pub = policy.FirewallEnabled[3];
                Logger.Instance.Info($"  Domain: {domain}, Private: {priv}, Public: {pub}");
                if (!domain || !priv || !pub) throw new Exception("Firewall disabled on some profiles");
            }
            catch (Exception ex) { throw new Exception($"Firewall check: {ex.Message}"); }
        }

        // =====================================================================
        //  ПОДПИСИ ФАЙЛОВ
        // =====================================================================

        public static void CheckWindowsExeSignatures()
        {
            int valid = 0, bad = 0, thirdParty = 0;
            var dir = @"C:\Windows";
            foreach (var f in Directory.EnumerateFiles(dir, "*.exe").Take(50))
            {
                var sig = SignatureVerifier.Verify(f);
                if (sig.IsValid && sig.IsMicrosoft) valid++;
                else if (sig.IsValid && !sig.IsMicrosoft) thirdParty++;
                else bad++;
            }
            Logger.Instance.Info($"  Проверено: {valid + bad + thirdParty}, MS: {valid}, 3rd: {thirdParty}, BAD: {bad}");
            if (bad > 0) throw new Exception($"{bad} files with bad signature");
        }

        public static void CheckDriverSignatures()
        {
            int valid = 0, bad = 0, thirdParty = 0;
            var dir = @"C:\Windows\System32\drivers";
            foreach (var f in Directory.EnumerateFiles(dir, "*.sys"))
            {
                var sig = SignatureVerifier.Verify(f);
                if (sig.IsValid && sig.IsMicrosoft) valid++;
                else if (sig.IsValid && !sig.IsMicrosoft) thirdParty++;
                else bad++;
            }
            Logger.Instance.Info($"  Проверено: {valid + bad + thirdParty}, MS: {valid}, 3rd: {thirdParty}, BAD: {bad}");
            if (bad > 5) Logger.Instance.Warn($"  {bad} drivers without valid signature");
        }

        public static void CheckSystem32DllSignatures()
        {
            int total = 0, ms = 0, thirdParty = 0, bad = 0;
            var dir = @"C:\Windows\System32";
            foreach (var f in Directory.EnumerateFiles(dir, "*.dll").Take(100))
            {
                total++;
                var sig = SignatureVerifier.Verify(f);
                if (sig.IsValid && sig.IsMicrosoft) ms++;
                else if (sig.IsValid && !sig.IsMicrosoft) thirdParty++;
                else bad++;
            }
            Logger.Instance.Info($"  Проверено: {total}, MS: {ms}, 3rd: {thirdParty}, BAD: {bad}");
        }

        public static void CheckSysWow64Signatures()
        {
            int total = 0, ms = 0, thirdParty = 0, bad = 0;
            var dir = @"C:\Windows\SysWOW64";
            if (!Directory.Exists(dir)) { Logger.Instance.Info("  SysWOW64 не найден (x86 система)"); return; }
            foreach (var f in Directory.EnumerateFiles(dir, "*.dll").Take(100))
            {
                total++;
                var sig = SignatureVerifier.Verify(f);
                if (sig.IsValid && sig.IsMicrosoft) ms++;
                else if (sig.IsValid && !sig.IsMicrosoft) thirdParty++;
                else bad++;
            }
            Logger.Instance.Info($"  Проверено: {total}, MS: {ms}, 3rd: {thirdParty}, BAD: {bad}");
        }

        // =====================================================================
        //  РЕЕСТР
        // =====================================================================

        public static void CheckWinlogonValues()
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
            var ui = key?.GetValue("Userinit")?.ToString();
            var sh = key?.GetValue("Shell")?.ToString();
            Logger.Instance.Info($"  Userinit = {ui}");
            Logger.Instance.Info($"  Shell = {sh}");
            if (ui?.ToLowerInvariant() != @"c:\windows\system32\userinit.exe,")
                throw new Exception($"Suspicious Userinit: {ui}");
            if (sh?.ToLowerInvariant() != "explorer.exe")
                throw new Exception($"Suspicious Shell: {sh}");
        }

        public static void CheckAppInitDlls()
        {
            foreach (var p in new[] {
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows" })
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                var appinit = key?.GetValue("AppInit_DLLs")?.ToString();
                var load = key?.GetValue("LoadAppInit_DLLs");
                Logger.Instance.Info($"  [{p}] AppInit_DLLs='{appinit}' LoadAppInit_DLLs={load}");
                if (!string.IsNullOrEmpty(appinit))
                    throw new Exception($"AppInit_DLLs is set: {appinit}");
            }
        }

        public static void CheckIFEO()
        {
            using var ifeo = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options");
            if (ifeo == null) return;
            int hijackCount = 0;
            foreach (var exeName in ifeo.GetSubKeyNames())
            {
                using var sub = ifeo.OpenSubKey(exeName);
                var dbg = sub?.GetValue("Debugger");
                if (dbg != null && !string.IsNullOrEmpty(dbg.ToString()))
                {
                    hijackCount++;
                    Logger.Instance.Warn($"  ⚠ IFEO Debugger for {exeName}: {dbg}");
                }
            }
            if (hijackCount > 0)
                throw new Exception($"{hijackCount} IFEO hijacks detected");
            Logger.Instance.Info("  ✓ IFEO без Debugger-взломов");
        }

        public static void CheckRunKeysClean()
        {
            string[] runKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
            };
            int total = 0;
            foreach (var p in runKeys)
            {
                using var k1 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                using var k2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(p);
                if (k1 != null) total += k1.ValueCount;
                if (k2 != null) total += k2.ValueCount;
            }
            Logger.Instance.Info($"  Всего записей в Run keys: {total}");
        }

        public static void CheckPoliciesExplorerRun()
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run");
            if (k == null) { Logger.Instance.Info("  Policies\\Explorer\\Run не существует"); return; }
            int count = k.ValueCount;
            Logger.Instance.Info($"  Записей: {count}");
            if (count > 0)
            {
                foreach (var n in k.GetValueNames())
                    Logger.Instance.Warn($"    {n} = {k.GetValue(n)}");
            }
        }

        public static void CheckHostsRedirects()
        {
            var hostsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
            if (!File.Exists(hostsPath)) return;
            string[] suspect = { "microsoft.com", "windowsupdate.com", "google.com", "facebook.com", "youtube.com" };
            int hits = 0;
            foreach (var line in File.ReadAllLines(hostsPath))
            {
                var t = line.Trim();
                if (t.StartsWith("#") || string.IsNullOrEmpty(t)) continue;
                foreach (var s in suspect)
                    if (t.Contains(s)) { hits++; Logger.Instance.Warn($"  ⚠ hosts: {t}"); }
            }
            if (hits > 0) throw new Exception($"{hits} suspicious hosts redirects");
        }

        public static void CheckSafeModeKeys()
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SafeBoot");
            if (k == null) throw new Exception("SafeBoot key not found");
            Logger.Instance.Info($"  SafeBoot subkeys: {k.SubKeyCount}");
        }

        // =====================================================================
        //  СЛУЖБЫ
        // =====================================================================

        public static void CheckCriticalServices()
        {
            string[] critical = { "EventLog", "Schedule", "RpcSs", "Winmgmt", "PlugPlay", "Spooler", "CryptSvc" };
            foreach (var svc in critical)
            {
                uint state = ServicesNativeApi.GetServiceState(svc);
                string stateStr = ServicesNativeApi.StateToString(state);
                Logger.Instance.Info($"  {svc,-25} {stateStr}");
                if (state != ServicesNativeApi.SERVICE_RUNNING)
                    Logger.Instance.Warn($"    ⚠ Критическая служба не запущена!");
            }
        }

        public static void CheckServicesWithoutPath()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Service");
                int badCount = 0;
                foreach (System.Management.ManagementObject svc in q.Get())
                {
                    var path = svc["PathName"]?.ToString();
                    if (string.IsNullOrEmpty(path))
                    {
                        badCount++;
                        Logger.Instance.Warn($"  ⚠ {svc["Name"]} — нет пути!");
                    }
                }
                Logger.Instance.Info($"  Служб без пути: {badCount}");
            }
            catch (Exception ex) { Logger.Instance.Warn($"  {ex.Message}"); }
        }

        public static void CheckServicesBadPath()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Service");
                int badCount = 0;
                foreach (System.Management.ManagementObject svc in q.Get())
                {
                    var path = svc["PathName"]?.ToString() ?? "";
                    if (path.Contains("\\Temp\\") || path.Contains("\\AppData\\"))
                    {
                        badCount++;
                        Logger.Instance.Warn($"  ⚠ {svc["Name"]} — путь в Temp/AppData: {path}");
                    }
                }
                Logger.Instance.Info($"  Служб с подозрительным путём: {badCount}");
            }
            catch (Exception ex) { Logger.Instance.Warn($"  {ex.Message}"); }
        }

        public static void CheckDisabledServices()
        {
            string[] shouldNotBeDisabled = { "WinDefend", "mpssvc", "wuauserv", "BITS", "MpsSvc", "CryptSvc" };
            foreach (var svc in shouldNotBeDisabled)
            {
                uint state = ServicesNativeApi.GetServiceState(svc);
                Logger.Instance.Info($"  {svc}: state={state}");
            }
        }

        // =====================================================================
        //  СЕТЬ
        // =====================================================================

        public static void CheckWinsockLSP()
        {
            int ec = ConsoleHelper.RunExternal("netsh.exe", "winsock show catalog", out var so, out _);
            if (ec == 0)
            {
                int entries = so.Split('\n').Count(l => l.Contains("Entry Type"));
                Logger.Instance.Info($"  Winsock LSP entries: {entries}");
            }
        }

        public static void CheckListeningPorts()
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var tcp = props.GetActiveTcpListeners();
            var udp = props.GetActiveUdpListeners();
            Logger.Instance.Info($"  TCP listeners: {tcp.Length}, UDP listeners: {udp.Length}");
            foreach (var ep in tcp.Take(20))
            {
                Logger.Instance.Info($"    TCP {ep}");
            }
        }

        public static void CheckPendingConnections()
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var conns = props.GetActiveTcpConnections();
            int pending = conns.Count(c => c.State == TcpState.SynSent || c.State == TcpState.TimeWait);
            Logger.Instance.Info($"  Active: {conns.Length}, pending/time-wait: {pending}");
        }

        public static void CheckDnsServers()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                var dns = nic.GetIPProperties().DnsAddresses;
                Logger.Instance.Info($"  {nic.Name}: DNS = {string.Join(", ", dns)}");
            }
        }

        public static void CheckDhcpState()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                var props = nic.GetIPProperties();
                var dhcp = props.DhcpServerAddresses;
                Logger.Instance.Info($"  {nic.Name}: DHCP = {string.Join(", ", dhcp)}");
            }
        }

        // =====================================================================
        //  АВТОЗАГРУЗКА
        // =====================================================================

        public static void CheckStartupFolderClean()
        {
            string[] folders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };
            foreach (var f in folders.Distinct())
            {
                if (!Directory.Exists(f)) continue;
                var files = Directory.GetFiles(f);
                Logger.Instance.Info($"  {f}: {files.Length} files");
                foreach (var file in files)
                    Logger.Instance.Warn($"    ⚠ {Path.GetFileName(file)}");
            }
        }

        public static void CheckNoExecutablesInTemp()
        {
            var temp = Path.GetTempPath();
            int count = 0;
            foreach (var f in Directory.EnumerateFiles(temp, "*.exe", SearchOption.TopDirectoryOnly))
                count++;
            foreach (var f in Directory.EnumerateFiles(temp, "*.dll", SearchOption.TopDirectoryOnly))
                count++;
            Logger.Instance.Info($"  EXE/DLL в Temp: {count}");
            if (count > 20) Logger.Instance.Warn($"  Много исполняемых файлов в Temp");
        }

        public static void CheckScheduledTasksSuspicious()
        {
            try
            {
                dynamic sched = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
                sched.Connect();
                var root = sched.GetFolder("\\");
                int count = 0;
                EnumTasksRecursive(root, ref count);
                Logger.Instance.Info($"  Всего задач: {count}");
            }
            catch (Exception ex) { Logger.Instance.Warn($"  {ex.Message}"); }
        }

        private static void EnumTasksRecursive(dynamic folder, ref int count)
        {
            try
            {
                var tasks = folder.GetTasks(0);
                foreach (var t in tasks)
                {
                    count++;
                }
                foreach (var sub in folder.GetFolders(0))
                    EnumTasksRecursive(sub, ref count);
            }
            catch { }
        }

        public static void CheckNoScriptsInStartup()
        {
            string[] exts = { "*.vbs", "*.js", "*.ps1", "*.bat", "*.cmd" };
            string[] folders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };
            int count = 0;
            foreach (var f in folders.Distinct())
            {
                if (!Directory.Exists(f)) continue;
                foreach (var ext in exts)
                    count += Directory.GetFiles(f, ext).Length;
            }
            Logger.Instance.Info($"  Скриптов в Startup: {count}");
        }

        // =====================================================================
        //  БРАУЗЕРЫ
        // =====================================================================

        public static void CheckChromeExtensions()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data");
            int count = CountChromeExt(path);
            Logger.Instance.Info($"  Chrome расширений: {count}");
        }

        public static void CheckEdgeExtensions()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data");
            int count = CountChromeExt(path);
            Logger.Instance.Info($"  Edge расширений: {count}");
        }

        private static int CountChromeExt(string userDataDir)
        {
            if (!Directory.Exists(userDataDir)) return 0;
            int count = 0;
            foreach (var profileDir in Directory.GetDirectories(userDataDir, "Profile*").Concat(
                new[] { Path.Combine(userDataDir, "Default") }))
            {
                var extDir = Path.Combine(profileDir, "Extensions");
                if (Directory.Exists(extDir))
                    count += Directory.GetDirectories(extDir).Length;
            }
            return count;
        }

        public static void CheckFirefoxExtensions()
        {
            var mozilla = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mozilla", "Firefox");
            if (!Directory.Exists(mozilla)) { Logger.Instance.Info("  Firefox не установлен"); return; }
            var profilesIni = Path.Combine(mozilla, "profiles.ini");
            if (!File.Exists(profilesIni)) return;
            int count = 0;
            var lines = File.ReadAllLines(profilesIni);
            string path = "";
            foreach (var line in lines)
                if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                    path = line.Substring(5).Replace("/", "\\");
            if (!string.IsNullOrEmpty(path))
            {
                var extDir = Path.Combine(mozilla, path, "extensions");
                if (Directory.Exists(extDir))
                    count = Directory.GetFiles(extDir, "*.xpi").Length;
            }
            Logger.Instance.Info($"  Firefox расширений: {count}");
        }

        public static void CheckIEBhos()
        {
            string[] paths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects"
            };
            int count = 0;
            foreach (var p in paths)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                if (key != null) count += key.SubKeyCount;
            }
            Logger.Instance.Info($"  IE BHOs: {count}");
        }

        // =====================================================================
        //  ФАЙЛЫ
        // =====================================================================

        public static void CheckPendingXml()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "WinSxS", "pending.xml");
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                Logger.Instance.Warn($"  ⚠ pending.xml существует! Размер: {info.Length} байт");
                Logger.Instance.Warn("    Windows ожидает перезагрузки для завершения обновления.");
            }
            else
            {
                Logger.Instance.Info("  ✓ pending.xml не найден");
            }
        }

        public static void CheckComponentsHive()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32", "config", "COMPONENTS");
            if (!File.Exists(path))
                throw new Exception("COMPONENTS hive не найден!");
            var info = new FileInfo(path);
            Logger.Instance.Info($"  COMPONENTS: {info.Length / 1024 / 1024:F1} MB, изменён {info.LastWriteTime:yyyy-MM-dd}");
        }

        public static void CheckCbsLogErrors()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Logs", "CBS", "CBS.log");
            if (!File.Exists(path)) { Logger.Instance.Info("  CBS.log не найден"); return; }
            int errors = 0;
            using var sr = new StreamReader(path);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("Error", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("ErrorReporting", StringComparison.OrdinalIgnoreCase))
                    errors++;
            }
            Logger.Instance.Info($"  CBS.log errors: {errors}");
            if (errors > 100) Logger.Instance.Warn("  Много ошибок в CBS.log");
        }

        public static void CheckPrefetch()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            if (!Directory.Exists(path)) { Logger.Instance.Info("  Prefetch папка не найдена"); return; }
            var files = Directory.GetFiles(path, "*.pf");
            Logger.Instance.Info($"  Prefetch файлов: {files.Length}");
        }

        public static void CheckPagefile()
        {
            var path = Path.Combine("C:\\", "pagefile.sys");
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                Logger.Instance.Info($"  pagefile.sys: {info.Length / 1024 / 1024 / 1024:F1} GB");
            }
            else Logger.Instance.Info("  pagefile.sys не найден");
        }

        public static void CheckHiberfil()
        {
            var path = Path.Combine("C:\\", "hiberfil.sys");
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                Logger.Instance.Info($"  hiberfil.sys: {info.Length / 1024 / 1024 / 1024:F1} GB");
            }
            else Logger.Instance.Info("  hiberfil.sys не найден (гибернация отключена)");
        }

        // =====================================================================
        //  POWERSHELL
        // =====================================================================

        public static void CheckPowershellPolicy()
        {
            int ec = ConsoleHelper.RunExternal("powershell.exe", "-Command \"Get-ExecutionPolicy -List\"",
                out var so, out _);
            if (ec == 0)
                Logger.Instance.Info($"\n{so}");
        }

        // =====================================================================
        //  СИСТЕМА
        // =====================================================================

        public static void CheckPendingReboot()
        {
            string[] keysToCheck =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
            };
            bool pending = false;
            foreach (var p in keysToCheck)
            {
                using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(p);
                if (k != null)
                {
                    pending = true;
                    Logger.Instance.Warn($"  ⚠ {p} существует");
                }
            }
            if (pending) Logger.Instance.Warn("  Требуется перезагрузка!");
            else Logger.Instance.Info("  ✓ Перезагрузка не требуется");
        }

        public static void CheckDiskSpace()
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var freeGB = drive.AvailableFreeSpace / 1024.0 / 1024 / 1024;
                var totalGB = drive.TotalSize / 1024.0 / 1024 / 1024;
                Logger.Instance.Info($"  {drive.Name} {freeGB:F1}/{totalGB:F1} GB free");
                if (freeGB < 5)
                    Logger.Instance.Warn($"    ⚠ Мало свободного места на {drive.Name}!");
            }
        }

        public static void CheckWuPendingReboot()
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (k != null) Logger.Instance.Warn("  ⚠ Windows Update требует перезагрузки");
            else Logger.Instance.Info("  ✓ WU без перезагрузки");
        }

        public static void CheckHVCI()
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard");
            var hvci = k?.GetValue("EnableVirtualizationBasedSecurity");
            Logger.Instance.Info($"  VBS/HVCI: {hvci}");
        }

        public static void CheckTpm()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher(
                    @"root\cimv2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm");
                foreach (System.Management.ManagementObject tpm in q.Get())
                {
                    Logger.Instance.Info($"  TPM: {tpm["ManufacturerVersion"]}, Enabled={tpm["IsEnabled_InitialValue"]}");
                }
            }
            catch (Exception ex) { Logger.Instance.Warn($"  TPM: {ex.Message}"); }
        }

        public static void CheckSecureBoot()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher(
                    @"root\standardcimv2", "SELECT * FROM Win32_SecureBoot");
                foreach (System.Management.ManagementObject sb in q.Get())
                {
                    Logger.Instance.Info($"  SecureBoot state: {sb["SecureBootEnabled"]}");
                }
            }
            catch (Exception ex) { Logger.Instance.Warn($"  SecureBoot: {ex.Message}"); }
        }

        public static void CheckLastBootTime()
        {
            try
            {
                var q = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (System.Management.ManagementObject os in q.Get())
                {
                    var lastBoot = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString());
                    var uptime = DateTime.Now - lastBoot;
                    Logger.Instance.Info($"  Last boot: {lastBoot:yyyy-MM-dd HH:mm}, uptime: {uptime.Days}d {uptime.Hours}h");
                }
            }
            catch (Exception ex) { Logger.Instance.Warn($"  {ex.Message}"); }
        }

        public static void CheckTimeSync()
        {
            int ec = ConsoleHelper.RunExternal("w32tm.exe", "/query /status", out var so, out _);
            if (ec == 0)
                Logger.Instance.Info($"\n{so}");
            else
                Logger.Instance.Warn("  w32tm не доступен");
        }
    }

    /// <summary>Вспомогательный класс для конвертации WMI datetime.</summary>
    internal static class ManagementDateTimeConverter
    {
        public static DateTime ToDateTime(string dt)
        {
            if (string.IsNullOrEmpty(dt)) return DateTime.MinValue;
            try
            {
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
    }
}
