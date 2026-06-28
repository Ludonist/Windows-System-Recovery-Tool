using System;
using System.Diagnostics;
using System.Security.Principal;

namespace SystemRestoreTool.Utils
{
    /// <summary>
    /// Вспомогательные методы консольного UI: проверка прав,
    /// красивый вывод, ввод пользователя, рисование рамок.
    /// </summary>
    public static class ConsoleHelper
    {
        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool IsWindows10OrLater()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && os.Version >= new Version(10, 0);
        }

        public static void WriteHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            Console.WriteLine("╔" + new string('═', title.Length + 2) + "╗");
            Console.WriteLine("║ " + title + " ║");
            Console.WriteLine("╚" + new string('═', title.Length + 2) + "╝");
            Console.ResetColor();
        }

        public static void WriteMenu(string[] items)
        {
            Console.WriteLine();
            for (int i = 0; i < items.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  [{i + DigitsFor(items.Length)}] ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(items[i]);
            }
            Console.ResetColor();
        }

        private static string DigitsFor(int count) => "";

        public static int ReadInt(string prompt, int min, int max, int defaultValue)
        {
            Console.Write(prompt);
            var line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line)) return defaultValue;
            if (int.TryParse(line, out var v) && v >= min && v <= max) return v;
            return defaultValue;
        }

        public static bool ReadYesNo(string prompt, bool defaultValue)
        {
            Console.Write($"{prompt} [{(defaultValue ? "Y/n" : "y/N")}]: ");
            var line = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(line)) return defaultValue;
            return line == "y" || line == "yes" || line == "д" || line == "да";
        }

        public static void WaitForKey()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        /// <summary>Запуск процесса с ожиданием и захватом кода возврата.
        /// Используется ТОЛЬКО когда нет нативного API (например, для cleanmgr).</summary>
        public static int RunExternal(string exe, string args, out string stdout, out string stderr)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            using var p = Process.Start(psi);
            stdout = p.StandardOutput.ReadToEnd();
            stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode;
        }
    }
}
