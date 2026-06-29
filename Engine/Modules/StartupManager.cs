using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  STARTUP MANAGER
    //  Управление автозагрузкой: Run keys, Startup folder, removal.
    // ===========================================================================

    public static class StartupManager
    {
        public static void ListAll()
        {
            Logger.Instance.Header(">>> Все элементы автозагрузки");

            var items = GetAllStartupItems();
            Logger.Instance.Info($"Найдено элементов: {items.Count}");
            Logger.Instance.Raw("");

            foreach (var item in items.OrderBy(x => x.Source))
            {
                string status = item.Suspicious ? "⚠" : " ";
                Logger.Instance.Raw($"  {status} [{item.Source,-30}] {item.Name,-35} → {Trunc(item.Command, 70)}");
                if (item.Suspicious)
                    Logger.Instance.Raw($"       ⚠ подозрительно: {item.SuspiciousReason}");
            }
        }

        public static List<StartupItem> GetAllStartupItems()
        {
            var list = new List<StartupItem>();

            // 1. HKLM Run
            CollectRunKey(list, Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM Run");
            CollectRunKey(list, Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM RunOnce");
            CollectRunKey(list, Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM Run (WOW64)");
            CollectRunKey(list, Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM RunOnce (WOW64)");

            // 2. HKCU Run
            CollectRunKey(list, Microsoft.Win32.Registry.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKCU Run");
            CollectRunKey(list, Microsoft.Win32.Registry.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU RunOnce");

            // 3. Startup folders
            CollectStartupFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Startup (User)");
            CollectStartupFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Startup (All Users)");

            // 4. Winlogon
            CollectWinlogon(list);

            return list;
        }

        private static void CollectRunKey(List<StartupItem> list, Microsoft.Win32.RegistryKey hive,
            string path, string source)
        {
            using var key = hive.OpenSubKey(path);
            if (key == null) return;
            foreach (var name in key.GetValueNames())
            {
                var cmd = key.GetValue(name)?.ToString() ?? "";
                var item = new StartupItem
                {
                    Source = source,
                    Name = name,
                    Command = cmd,
                    Suspicious = IsSuspicious(cmd, out var reason),
                    SuspiciousReason = reason
                };
                list.Add(item);
            }
        }

        private static void CollectStartupFolder(List<StartupItem> list, string folder, string source)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder))
            {
                list.Add(new StartupItem
                {
                    Source = source,
                    Name = Path.GetFileName(f),
                    Command = f,
                    Suspicious = Path.GetExtension(f).ToLowerInvariant() is ".vbs" or ".js" or ".ps1",
                    SuspiciousReason = "Скрипт в автозагрузке"
                });
            }
        }

        private static void CollectWinlogon(List<StartupItem> list)
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
            if (key == null) return;
            foreach (var name in new[] { "Shell", "Userinit", "AppInit_DLLs", "Taskman", "UIHost" })
            {
                var v = key.GetValue(name)?.ToString();
                if (!string.IsNullOrEmpty(v))
                {
                    bool susp = IsSuspicious(v, out var reason);
                    if (name == "Userinit" && v.ToLowerInvariant() == @"c:\windows\system32\userinit.exe,") susp = false;
                    if (name == "Shell" && v.ToLowerInvariant() == "explorer.exe") susp = false;
                    list.Add(new StartupItem
                    {
                        Source = "Winlogon",
                        Name = name,
                        Command = v,
                        Suspicious = susp,
                        SuspiciousReason = reason
                    });
                }
            }
        }

        private static bool IsSuspicious(string cmd, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(cmd)) return false;

            var lower = cmd.ToLowerInvariant();
            if (lower.Contains("\\temp\\")) { reason = "Путь в Temp"; return true; }
            if (lower.Contains("\\appdata\\local\\temp\\")) { reason = "Путь в AppData Temp"; return true; }
            if (lower.Contains("powershell") && lower.Contains("-enc")) { reason = "PowerShell с -enc (Base64)"; return true; }
            if (lower.Contains("cmd /c") && lower.Contains("download")) { reason = "cmd /c download"; return true; }
            if (lower.Contains("regsvr32") && lower.Contains("/s /u /i:")) { reason = "regsv32 squiblydoo attack"; return true; }
            if (lower.Contains("mshta") && lower.Contains("http")) { reason = "mshta + http"; return true; }
            if (lower.Contains("rundll32") && lower.Contains("javascript")) { reason = "rundll32 + javascript"; return true; }
            return false;
        }

        private static string Trunc(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max - 3) + "...");

        public class StartupItem
        {
            public string Source { get; set; }
            public string Name { get; set; }
            public string Command { get; set; }
            public bool Suspicious { get; set; }
            public string SuspiciousReason { get; set; }
        }
    }
}
