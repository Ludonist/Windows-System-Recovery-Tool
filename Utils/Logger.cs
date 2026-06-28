using System;
using System.IO;
using System.Threading;

namespace SystemRestoreTool.Utils
{
    /// <summary>
    /// Потокобезопасный логгер, пишет одновременно в консоль (с цветом) и в файл.
    /// Один лог-файл на запуск, с автоматическим резервированием предыдущего.
    /// </summary>
    public sealed class Logger : IDisposable
    {
        private static readonly Lazy<Logger> _instance = new(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly object _sync = new();
        private readonly StreamWriter _writer;
        public string LogFilePath { get; }

        private Logger()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemRestoreTool");
            Directory.CreateDirectory(dir);

            LogFilePath = Path.Combine(dir, $"srt_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _writer = new StreamWriter(LogFilePath, append: false, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void Raw(string message, ConsoleColor? color = null)
        {
            lock (_sync)
            {
                if (color.HasValue)
                {
                    var prev = Console.ForegroundColor;
                    Console.ForegroundColor = color.Value;
                    Console.WriteLine(message);
                    Console.ForegroundColor = prev;
                }
                else
                {
                    Console.WriteLine(message);
                }
                _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {StripAnsi(message)}");
            }
        }

        private static string StripAnsi(string s) => s;

        public void Info(string msg)    => Raw($"[i] {msg}", ConsoleColor.Cyan);
        public void Success(string msg) => Raw($"[+] {msg}", ConsoleColor.Green);
        public void Warn(string msg)    => Raw($"[!] {msg}", ConsoleColor.Yellow);
        public void Error(string msg)   => Raw($"[X] {msg}", ConsoleColor.Red);
        public void Header(string msg)  => Raw(msg, ConsoleColor.Magenta);

        /// <summary>Прогресс-бар в одну строку с обновлением.</summary>
        public void Progress(int current, int total, string label)
        {
            if (total <= 0) return;
            int pct = (int)((current * 100L) / total);
            int barWidth = 30;
            int filled = (int)(pct / 100.0 * barWidth);
            string bar = new string('█', filled) + new string('░', barWidth - filled);
            lock (_sync)
            {
                Console.Write($"\r   {label} [{bar}] {pct,3}% ({current}/{total})   ");
                if (current >= total) Console.WriteLine();
            }
        }

        public void Dispose()
        {
            lock (_sync) _writer?.Dispose();
        }
    }
}
