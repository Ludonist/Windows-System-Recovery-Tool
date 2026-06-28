using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  FILE HASH DATABASE MANAGER
    //  Создание и сравнение базы эталонных хешей системных файлов.
    //  Позволяет:
    //    1. Снять "снимок" системы сразу после установки/восстановления.
    //    2. Позже сравнить с текущим состоянием — выявить подмену/модификацию.
    //  Полезно, когда WinVerifyTrust не работает (например, для .manifest, .mui).
    // ===========================================================================

    public sealed class FileHashEntry
    {
        public string Path { get; set; }
        public string Sha256 { get; set; }
        public string Sha1 { get; set; }
        public long Size { get; set; }
        public DateTime LastWrite { get; set; }
    }

    public static class FileHashDatabaseManager
    {
        /// <summary>Создаёт базу хешей всех файлов в указанных директориях.</summary>
        public static void CreateDatabase(string outputPath, string[] dirs, Action<int, int, string> progress = null)
        {
            Logger.Instance.Header($">>> Создание базы хешей → {outputPath}");

            var entries = new List<FileHashEntry>(capacity: 10000);
            int count = 0;

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;

                var files = SafeEnumerateFiles(dir);
                foreach (var f in files)
                {
                    count++;
                    progress?.Invoke(count, 0, Path.GetFileName(f));

                    var entry = ComputeEntry(f);
                    if (entry != null) entries.Add(entry);
                }
            }

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, json);

            Logger.Instance.Success($"База хешей создана: {entries.Count} файлов → {outputPath}");
            Logger.Instance.Info($"Размер файла: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:N1} MB");
        }

        /// <summary>Сравнивает текущее состояние файлов с базой.</summary>
        public static void CompareWithDatabase(string databasePath, Action<int, int, string> progress = null)
        {
            Logger.Instance.Header($">>> Сравнение с базой хешей: {databasePath}");

            if (!File.Exists(databasePath))
            {
                Logger.Instance.Error($"Файл базы не найден: {databasePath}");
                return;
            }

            var json = File.ReadAllText(databasePath);
            var entries = JsonSerializer.Deserialize<List<FileHashEntry>>(json);
            Logger.Instance.Info($"В базе: {entries.Count} файлов");

            int matched = 0, modified = 0, missing = 0, newFiles = 0;
            var issues = new List<string>();

            int idx = 0;
            foreach (var entry in entries)
            {
                idx++;
                progress?.Invoke(idx, entries.Count, Path.GetFileName(entry.Path));

                if (!File.Exists(entry.Path))
                {
                    missing++;
                    issues.Add($"[ОТСУТСТВУЕТ] {entry.Path}");
                    continue;
                }

                var current = ComputeEntry(entry.Path);
                if (current.Sha256 == entry.Sha256)
                {
                    matched++;
                }
                else
                {
                    modified++;
                    issues.Add($"[ИЗМЕНЁН] {entry.Path}");
                    issues.Add($"   было: {entry.Sha256}");
                    issues.Add($"   стало: {current.Sha256}");
                }
            }

            Logger.Instance.Success($"Совпадает:     {matched:N0}");
            Logger.Instance.Error($"Изменено:      {modified:N0}");
            Logger.Instance.Error($"Отсутствует:   {missing:N0}");

            if (issues.Count > 0 && issues.Count <= 50)
            {
                Logger.Instance.Header("ДЕТАЛИ ИЗМЕНЕНИЙ:");
                foreach (var line in issues)
                    Logger.Instance.Warn(line);
            }
            else if (issues.Count > 50)
            {
                Logger.Instance.Warn($"(показано 50 из {issues.Count} проблем)");
                foreach (var line in issues.Take(50))
                    Logger.Instance.Warn(line);
            }
        }

        /// <summary>Вычисляет запись с хешами для файла.</summary>
        private static FileHashEntry ComputeEntry(string path)
        {
            try
            {
                var info = new FileInfo(path);
                using var fs = File.OpenRead(path);
                using var sha256 = SHA256.Create();
                using var sha1 = SHA1.Create();

                var hash256 = sha256.ComputeHash(fs);
                fs.Position = 0;
                var hash1 = sha1.ComputeHash(fs);

                return new FileHashEntry
                {
                    Path = path,
                    Sha256 = BitConverter.ToString(hash256).Replace("-", "").ToLowerInvariant(),
                    Sha1 = BitConverter.ToString(hash1).Replace("-", "").ToLowerInvariant(),
                    Size = info.Length,
                    LastWrite = info.LastWriteTimeUtc
                };
            }
            catch { return null; }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string dir)
        {
            var stack = new Stack<string>();
            stack.Push(dir);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                string[] files = null;
                try { files = Directory.GetFiles(current); }
                catch { continue; }

                foreach (var f in files)
                    yield return f;

                string[] subdirs = null;
                try { subdirs = Directory.GetDirectories(current); }
                catch { continue; }

                foreach (var s in subdirs)
                    stack.Push(s);
            }
        }

        /// <summary>Стандартный набор директорий для снимка.</summary>
        public static readonly string[] DefaultSnapshotDirs =
        {
            @"C:\Windows\System32",
            @"C:\Windows\SysWOW64",
            @"C:\Windows\System32\drivers",
            @"C:\Windows"
        };
    }
}
