using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  HOSTS FILE MANAGER
    //  Просмотр, проверка, восстановление файла hosts.
    // ===========================================================================

    public static class HostsFileManager
    {
        public static string HostsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

        public static void ShowHosts()
        {
            Logger.Instance.Header(">>> Содержимое файла hosts");
            Logger.Instance.Info($"Путь: {HostsPath}");

            if (!File.Exists(HostsPath))
            {
                Logger.Instance.Error("  Файл hosts не найден!");
                return;
            }

            var info = new FileInfo(HostsPath);
            Logger.Instance.Info($"Размер: {info.Length} байт, изменён: {info.LastWriteTime}");
            Logger.Instance.Raw("");

            int lineNum = 0;
            foreach (var line in File.ReadAllLines(HostsPath))
            {
                lineNum++;
                var t = line.Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith("#"))
                    Logger.Instance.Raw($"  {lineNum,4}:  {line}");
                else
                {
                    Logger.Instance.Raw($"  {lineNum,4}:  {line}");
                }
            }
        }

        public static void BackupHosts()
        {
            if (!File.Exists(HostsPath)) return;
            var backup = HostsPath + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(HostsPath, backup);
            Logger.Instance.Success($"Backup создан: {backup}");
        }

        public static void RestoreDefaultHosts()
        {
            Logger.Instance.Warn("Восстановление файла hosts по умолчанию Microsoft.");
            if (!ConsoleHelper.ReadYesNo("Продолжить?", false)) return;

            BackupHosts();

            string defaultContent = @"# Copyright (c) 1993-2009 Microsoft Corp.
#
# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.
#
# This file contains the mappings of IP addresses to host names. Each
# entry should be kept on an individual line. The IP address should
# be placed in the first column followed by the corresponding host name.
# The IP address and the host name should be separated by at least one
# space.
#
# Additionally, comments (such as these) may be inserted on individual
# lines or following the machine name denoted by a '#' symbol.
#
# For example:
#
#      102.54.94.97     rhino.acme.com          # source server
#       38.25.63.10     x.acme.com              # x client host

# localhost name resolution is handled within DNS itself.
#       127.0.0.1       localhost
#       ::1             localhost
";
            File.WriteAllText(HostsPath, defaultContent);
            Logger.Instance.Success("Файл hosts восстановлен к стандартному содержимому Microsoft.");
        }

        public static void CheckSuspiciousEntries()
        {
            Logger.Instance.Header(">>> Проверка файла hosts на подозрительные записи");

            if (!File.Exists(HostsPath)) { Logger.Instance.Error("Файл не найден"); return; }

            string[] suspect = {
                "microsoft.com", "windowsupdate.com", "windows.com",
                "google.com", "youtube.com", "facebook.com", "twitter.com",
                "instagram.com", "linkedin.com", "github.com",
                "live.com", "outlook.com", "hotmail.com", "office.com",
                "msn.com", "bing.com", "skype.com",
                "amazon.com", "paypal.com", "ebay.com",
                "apple.com", "icloud.com",
                "avast.com", "kaspersky.com", "mcafee.com", "norton.com",
                "malwarebytes.com", "virustotal.com",
                "adobe.com", "oracle.com", "java.com"
            };

            int total = 0, suspectCount = 0;
            foreach (var line in File.ReadAllLines(HostsPath))
            {
                var t = line.Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                total++;
                bool isSuspect = false;
                foreach (var s in suspect)
                    if (t.ToLowerInvariant().Contains(s)) { isSuspect = true; break; }
                if (isSuspect)
                {
                    suspectCount++;
                    Logger.Instance.Error($"  ⚠ {t}");
                }
            }
            Logger.Instance.Info($"Всего записей: {total}, подозрительных: {suspectCount}");
            if (suspectCount == 0)
                Logger.Instance.Success("✓ Подозрительных редиректов не обнаружено");
            else
                Logger.Instance.Warn("⚠ Рекомендуется восстановить файл hosts (пункт меню)");
        }
    }
}
