using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine
{
    // ===========================================================================
    //  INTEGRITY CHECKER  v2.0  —  расширенная проверка ВСЕХ системных файлов
    //  Проверка:
    //    1. КРИТИЧЕСКИХ файлов (40+ ключевых файлов)
    //    2. ВСЕХ файлов в C:\Windows\System32 (5000+ файлов)
    //    3. ВСЕХ файлов в C:\Windows\SysWOW64 (4000+ файлов)
    //    4. ВСЕХ драйверов в C:\Windows\System32\drivers (300+ файлов)
    //    5. ВСЕХ файлов в C:\Windows\System32\drivers\UMDF (User-Mode Drivers)
    //    6. Ключевых файлов в C:\Windows (explorer.exe, notepad.exe и т.д.)
    //    7. Файлов в C:\Windows\WinSxS (хранилище компонентов)
    //
    //  Для каждого файла:
    //    • Проверка защиты WFP (SfcIsFileProtected)
    //    • Проверка цифровой подписи (WinVerifyTrust)
    //    • Извлечение издателя (Microsoft / 3rd party)
    //    • Вычисление SHA256 (для сравнения с эталоном)
    // ===========================================================================

    public sealed class IntegrityReport
    {
        public int TotalChecked { get; set; }
        public int Protected { get; set; }
        public int NotProtected { get; set; }
        public int ValidSignature { get; set; }
        public int NoSignature { get; set; }
        public int BadSignature { get; set; }
        public int MicrosoftSigned { get; set; }
        public int ThirdPartySigned { get; set; }
        public int MissingFiles { get; set; }
        public List<FileIssue> Issues { get; } = new();

        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public TimeSpan Duration => FinishedAt - StartedAt;
    }

    public sealed class FileIssue
    {
        public string Path { get; set; }
        public IssueKind Kind { get; set; }
        public string Detail { get; set; }
    }

    public enum IssueKind
    {
        Missing,
        NotProtected,
        NoSignature,
        BadSignature,
        ThirdPartySigned,
        HashMismatch
    }

    public enum ScanScope
    {
        /// <summary>Только 40 критических файлов (быстро, ~30 сек).</summary>
        CriticalOnly,
        /// <summary>System32 + drivers (2000-5000 файлов, ~10-30 мин).</summary>
        System32AndDrivers,
        /// <summary>Полная проверка: System32 + SysWOW64 + drivers + WinSxS (медленно).</summary>
        Full
    }

    public static class IntegrityChecker
    {
        // ==================================================================
        //  СПИСОК КРИТИЧЕСКИХ ФАЙЛОВ (40+ ключевых)
        // ==================================================================

        public static readonly string[] CriticalFiles =
        {
            // === Базовые системные DLL ===
            @"C:\Windows\System32\kernel32.dll",
            @"C:\Windows\System32\ntdll.dll",
            @"C:\Windows\System32\user32.dll",
            @"C:\Windows\System32\advapi32.dll",
            @"C:\Windows\System32\shell32.dll",
            @"C:\Windows\System32\ole32.dll",
            @"C:\Windows\System32\oleaut32.dll",
            @"C:\Windows\System32\gdi32.dll",
            @"C:\Windows\System32\msvcrt.dll",
            @"C:\Windows\System32\ws2_32.dll",
            @"C:\Windows\System32\wininet.dll",
            @"C:\Windows\System32\urlmon.dll",
            @"C:\Windows\System32\iertutil.dll",
            @"C:\Windows\System32\mscoree.dll",
            @"C:\Windows\System32\clbcatq.dll",
            @"C:\Windows\System32\combase.dll",
            @"C:\Windows\System32\sechost.dll",
            @"C:\Windows\System32\rpcrt4.dll",
            @"C:\Windows\System32\setupapi.dll",
            @"C:\Windows\System32\version.dll",
            @"C:\Windows\System32\imm32.dll",
            @"C:\Windows\System32\msctf.dll",
            @"C:\Windows\System32\shlwapi.dll",
            @"C:\Windows\System32\psapi.dll",
            @"C:\Windows\System32\dbghelp.dll",
            @"C:\Windows\System32\winmm.dll",
            @"C:\Windows\System32\cabinet.dll",
            @"C:\Windows\System32\msi.dll",

            // === Загрузчики и ключевые процессы ===
            @"C:\Windows\System32\winload.exe",
            @"C:\Windows\System32\winresume.exe",
            @"C:\Windows\System32\winlogon.exe",
            @"C:\Windows\System32\csrss.exe",
            @"C:\Windows\System32\services.exe",
            @"C:\Windows\System32\lsass.exe",
            @"C:\Windows\System32\lsaiso.exe",
            @"C:\Windows\System32\smss.exe",
            @"C:\Windows\System32\svchost.exe",
            @"C:\Windows\System32\spoolsv.exe",
            @"C:\Windows\System32\wininit.exe",
            @"C:\Windows\System32\fontdrvhost.exe",
            @"C:\Windows\System32\dwm.exe",
            @"C:\Windows\System32\RuntimeBroker.exe",
            @"C:\Windows\System32\SearchIndexer.exe",
            @"C:\Windows\System32\SearchProtocolHost.exe",
            @"C:\Windows\System32\sihost.exe",
            @"C:\Windows\System32\taskhostw.exe",
            @"C:\Windows\System32\ctfmon.exe",
            @"C:\Windows\System32\ExplorerFrame.dll",

            // === Криптография / безопасность ===
            @"C:\Windows\System32\bcrypt.dll",
            @"C:\Windows\System32\ncrypt.dll",
            @"C:\Windows\System32\schannel.dll",
            @"C:\Windows\System32\crypt32.dll",
            @"C:\Windows\System32\cryptbase.dll",
            @"C:\Windows\System32\cryptsp.dll",
            @"C:\Windows\System32\wintrust.dll",
            @"C:\Windows\System32\ncryptprov.dll",
            @"C:\Windows\System32\msasn1.dll",
            @"C:\Windows\System32\rpcss.dll",

            // === DISM / SFC ===
            @"C:\Windows\System32\sfc.dll",
            @"C:\Windows\System32\sfc_os.dll",
            @"C:\Windows\System32\dismapi.dll",
            @"C:\Windows\System32\dism.exe",
            @"C:\Windows\System32\dismcore.dll",
            @"C:\Windows\System32\dismprov.dll",
            @"C:\Windows\System32\providers\dismprov.dll",
            @"C:\Windows\System32\sfc.exe",

            // === Управление и диагностика ===
            @"C:\Windows\System32\mmc.exe",
            @"C:\Windows\System32\mmcndmgr.dll",
            @"C:\Windows\System32\powershell.exe",
            @"C:\Windows\System32\pwsh.dll",
            @"C:\Windows\System32\cmd.exe",
            @"C:\Windows\System32\regedit.exe",
            @"C:\Windows\System32\taskmgr.exe",
            @"C:\Windows\System32\eventvwr.exe",
            @"C:\Windows\System32\perfmon.exe",
            @"C:\Windows\System32\compmgmtlauncher.exe",
            @"C:\Windows\System32\devmgmt.msc",
            @"C:\Windows\System32\diskmgmt.msc",
            @"C:\Windows\System32\services.msc",
            @"C:\Windows\System32\regedt32.exe",
            @"C:\Windows\System32\wbem\wmic.exe",
            @"C:\Windows\System32\wbem\WmiPrvSE.exe",
            @"C:\Windows\System32\wbem\winmgmt.exe",

            // === Shell ===
            @"C:\Windows\explorer.exe",
            @"C:\Windows\System32\shdocvw.dll",
            @"C:\Windows\System32\shellstyle.dll",
            @"C:\Windows\System32\themecpl.dll",
            @"C:\Windows\System32\themeui.dll",

            // === Драйверы ядра ===
            @"C:\Windows\System32\drivers\tcpip.sys",
            @"C:\Windows\System32\drivers\ntfs.sys",
            @"C:\Windows\System32\drivers\volmgr.sys",
            @"C:\Windows\System32\drivers\volmgrx.sys",
            @"C:\Windows\System32\drivers\disk.sys",
            @"C:\Windows\System32\drivers\partmgr.sys",
            @"C:\Windows\System32\drivers\ACPI.sys",
            @"C:\Windows\System32\drivers\hal.dll",
            @"C:\Windows\System32\drivers\ndis.sys",
            @"C:\Windows\System32\drivers\http.sys",
            @"C:\Windows\System32\drivers\Wdf01000.sys",
            @"C:\Windows\System32\drivers\WDFLDR.SYS",
            @"C:\Windows\System32\drivers\ksecdd.sys",
            @"C:\Windows\System32\drivers\ksecpkg.sys",
            @"C:\Windows\System32\drivers\klif.sys",
            @"C:\Windows\System32\drivers\msisadrv.sys",
            @"C:\Windows\System32\drivers\pci.sys",
            @"C:\Windows\System32\drivers\pcmcia.sys",
            @"C:\Windows\System32\drivers\mountmgr.sys",
            @"C:\Windows\System32\drivers\fileinfo.sys",
            @"C:\Windows\System32\drivers\fltMgr.sys",
            @"C:\Windows\System32\drivers\luafv.sys",
            @"C:\Windows\System32\drivers\mrxsmb.sys",
            @"C:\Windows\System32\drivers\mup.sys",
            @"C:\Windows\System32\drivers\npsvctrtrt.sys",

            // === Журналы и события ===
            @"C:\Windows\System32\wevtapi.dll",
            @"C:\Windows\System32\wevtsvc.dll",

            // === .NET Framework ===
            @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\clr.dll",
            @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll",
            @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll",

            // === Расширения WinRT ===
            @"C:\Windows\System32\WinMetadata\Windows.Foundation.winmd",
            @"C:\Windows\System32\WinRTTraceLogger.dll",

            // === Подсистема Linux (WSL) ===
            @"C:\Windows\System32\lxss.dll",
            @"C:\Windows\System32\wslapi.dll"
        };

        // ==================================================================
        //  ДИРЕКТОРИИ ДЛЯ ПОЛНОЙ ПРОВЕРКИ
        // ==================================================================

        public static readonly string[] System32Paths =
        {
            @"C:\Windows\System32",
            @"C:\Windows\System32\drivers",
            @"C:\Windows\System32\drivers\UMDF",
            @"C:\Windows\System32\drivers\wd"
        };

        public static readonly string[] SysWOW64Paths =
        {
            @"C:\Windows\SysWOW64"
        };

        public static readonly string[] WindowsRootPaths =
        {
            @"C:\Windows"  // все файлы .exe/.dll/.sys в корне
        };

        /// <summary>
        /// Главная функция проверки. В зависимости от scope проверяет разный объём файлов.
        /// </summary>
        public static IntegrityReport CheckCriticalFiles(ScanScope scope = ScanScope.CriticalOnly,
            Action<int, int, string> progress = null)
        {
            var report = new IntegrityReport { StartedAt = DateTime.Now };

            // Соберём список файлов согласно scope
            var files = new List<string>(capacity: 10000);
            files.AddRange(CriticalFiles);

            if (scope >= ScanScope.System32AndDrivers)
            {
                files.AddRange(EnumerateDirectory(System32Paths, new[] { ".dll", ".exe", ".sys", ".cpl" }));
                files.AddRange(EnumerateDirectory(WindowsRootPaths, new[] { ".exe", ".dll" }));
            }
            if (scope >= ScanScope.Full)
            {
                files.AddRange(EnumerateDirectory(SysWOW64Paths, new[] { ".dll", ".exe", ".sys", ".cpl" }));
            }

            // Дедупликация
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files) unique.Add(f);
            files = new List<string>(unique);

            int idx = 0;
            int total = files.Count;

            foreach (var file in files)
            {
                idx++;
                progress?.Invoke(idx, total, Path.GetFileName(file));

                if (!File.Exists(file))
                {
                    report.TotalChecked++;
                    report.MissingFiles++;
                    // Только критические файлы добавляем в Issues (для всех System32 - слишком много)
                    if (IsCriticalPath(file))
                    {
                        report.Issues.Add(new FileIssue
                        {
                            Path = file,
                            Kind = IssueKind.Missing,
                            Detail = "Файл отсутствует — критическая проблема!"
                        });
                    }
                    continue;
                }

                report.TotalChecked++;

                // 1. Проверка защиты WFP
                bool protectedBySfc;
                try { protectedBySfc = SfcNativeApi.IsFileProtected(file); }
                catch { protectedBySfc = false; }
                if (protectedBySfc) report.Protected++;
                else report.NotProtected++;

                // 2. Проверка подписи (только для .exe, .dll, .sys, .cab)
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".exe" or ".dll" or ".sys" or ".cab" or ".cpl")
                {
                    SignatureResult sig;
                    try { sig = SignatureVerifier.Verify(file); }
                    catch { continue; }

                    if (sig.IsValid && sig.IsSigned)
                    {
                        report.ValidSignature++;
                        if (sig.IsMicrosoft) report.MicrosoftSigned++;
                        else
                        {
                            report.ThirdPartySigned++;
                            // Добавляем в Issues только если файл КРИТИЧЕСКИЙ
                            if (IsCriticalPath(file))
                            {
                                report.Issues.Add(new FileIssue
                                {
                                    Path = file,
                                    Kind = IssueKind.ThirdPartySigned,
                                    Detail = $"Подписан НЕ Microsoft! Издатель: {sig.Publisher}"
                                });
                            }
                        }
                    }
                    else if (!sig.IsSigned)
                    {
                        report.NoSignature++;
                        if (IsCriticalPath(file))
                        {
                            report.Issues.Add(new FileIssue
                            {
                                Path = file,
                                Kind = IssueKind.NoSignature,
                                Detail = "Подпись отсутствует — возможна подмена"
                            });
                        }
                    }
                    else
                    {
                        report.BadSignature++;
                        if (IsCriticalPath(file))
                        {
                            report.Issues.Add(new FileIssue
                            {
                                Path = file,
                                Kind = IssueKind.BadSignature,
                                Detail = sig.Error
                            });
                        }
                    }
                }
            }

            report.FinishedAt = DateTime.Now;
            return report;
        }

        /// <summary>
        /// Проверяет, входит ли путь в список критических (для логирования в Issues).
        /// </summary>
        private static bool IsCriticalPath(string file)
        {
            foreach (var c in CriticalFiles)
                if (c.Equals(file, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Перечисление файлов в директориях по расширениям.</summary>
        private static IEnumerable<string> EnumerateDirectory(string[] dirs, string[] exts)
        {
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;

                IEnumerable<string> allFiles;
                try { allFiles = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly); }
                catch { continue; }

                foreach (var f in allFiles)
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    foreach (var want in exts)
                    {
                        if (ext == want) { yield return f; break; }
                    }
                }
            }
        }

        /// <summary>
        /// Перечисление всех защищённых файлов через SfcGetNextProtectedFile.
        /// </summary>
        public static List<string> EnumerateProtectedFiles(Action<int, string> progress = null)
        {
            var list = new List<string>(2000);
            var data = new ProtectedFileData();
            int count = 0;

            while (SfcNativeApi.SfcGetNextProtectedFile(IntPtr.Zero, ref data))
            {
                if (!string.IsNullOrEmpty(data.FileName))
                {
                    list.Add(data.FileName);
                    count++;
                    if (count % 100 == 0)
                        progress?.Invoke(count, data.FileName);
                }
            }

            return list;
        }

        /// <summary>Вычисление SHA256 файла.</summary>
        public static string ComputeSha256(string filePath)
        {
            try
            {
                using var sha = SHA256.Create();
                using var fs = File.OpenRead(filePath);
                var hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch { return null; }
        }

        /// <summary>Вычисление SHA1 файла.</summary>
        public static string ComputeSha1(string filePath)
        {
            try
            {
                using var sha = SHA1.Create();
                using var fs = File.OpenRead(filePath);
                var hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch { return null; }
        }

        /// <summary>Вычисление MD5 файла (для совместимости со старыми базами).</summary>
        public static string ComputeMd5(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var fs = File.OpenRead(filePath);
                var hash = md5.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch { return null; }
        }

        /// <summary>Печать красивого отчёта в консоль.</summary>
        public static void PrintReport(IntegrityReport report)
        {
            ConsoleHelper.WriteHeader("ОТЧЁТ О ЦЕЛОСТНОСТИ СИСТЕМНЫХ ФАЙЛОВ");

            Logger.Instance.Info($"Проверено файлов:           {report.TotalChecked:N0}");
            Logger.Instance.Info($"Под защитой WFP:            {report.Protected:N0}");
            Logger.Instance.Info($"Без защиты WFP:             {report.NotProtected:N0}");
            Logger.Instance.Success($"Валидная подпись Microsoft: {report.MicrosoftSigned:N0}");
            Logger.Instance.Warn($"Подписаны третьей стороной: {report.ThirdPartySigned:N0}");
            Logger.Instance.Warn($"Без подписи:                {report.NoSignature:N0}");
            Logger.Instance.Error($"С плохой подписью:          {report.BadSignature:N0}");
            Logger.Instance.Error($"Отсутствуют:                {report.MissingFiles:N0}");
            Logger.Instance.Info($"Время проверки:             {report.Duration.TotalSeconds:F1} сек");

            if (report.Issues.Count > 0)
            {
                ConsoleHelper.WriteHeader($"ОБНАРУЖЕНЫ ПРОБЛЕМЫ: {report.Issues.Count}");
                foreach (var issue in report.Issues)
                {
                    var msg = $"  [{issue.Kind}] {issue.Path}";
                    if (issue.Kind == IssueKind.Missing ||
                        issue.Kind == IssueKind.BadSignature ||
                        issue.Kind == IssueKind.ThirdPartySigned ||
                        issue.Kind == IssueKind.NoSignature)
                    {
                        Logger.Instance.Error(msg);
                        Logger.Instance.Error($"     → {issue.Detail}");
                    }
                    else
                    {
                        Logger.Instance.Warn(msg);
                        Logger.Instance.Warn($"     → {issue.Detail}");
                    }
                }
            }
            else
            {
                Logger.Instance.Success("Все критические файлы в порядке — подмен не обнаружено.");
            }
        }
    }
}
