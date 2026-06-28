using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SystemRestoreTool.Utils
{
    // ===========================================================================
    //  LOCALIZER
    //  Многоязычная поддержка интерфейса (RU / EN / ZH).
    //  Загружает строки из JSON-файлов в папке Resources/.
    //  Сохраняет выбор пользователя в реестре для следующих запусков.
    // ===========================================================================

    public static class Localizer
    {
        public const string DEFAULT_LANGUAGE = "ru";

        /// <summary>Поддерживаемые языки.</summary>
        public static readonly string[] SupportedLanguages = { "ru", "en", "zh", "de", "fr", "es", "ja", "ko", "pt" };

        /// <summary>Человекочитаемые названия языков.</summary>
        public static readonly Dictionary<string, (string Native, string English)> LanguageNames = new()
        {
            { "ru", ("Русский",       "Russian"              ) },
            { "en", ("English",        "English"              ) },
            { "zh", ("简体中文",        "Chinese (Simplified)" ) },
            { "de", ("Deutsch",        "German"               ) },
            { "fr", ("Français",       "French"               ) },
            { "es", ("Español",        "Spanish"              ) },
            { "ja", ("日本語",          "Japanese"             ) },
            { "ko", ("한국어",          "Korean"               ) },
            { "pt", ("Português",      "Portuguese"           ) }
        };

        private static Dictionary<string, JsonElement> _strings;
        private static string _currentLanguage = DEFAULT_LANGUAGE;

        /// <summary>Текущий язык (например, "ru", "en", "zh").</summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>Название текущего языка на родном языке.</summary>
        public static string CurrentLanguageNative =>
            LanguageNames.TryGetValue(_currentLanguage, out var n) ? n.Native : _currentLanguage;

        // -----------------------------------------------------------------

        static Localizer()
        {
            LoadLanguage(GetSavedLanguage() ?? DEFAULT_LANGUAGE);
        }

        /// <summary>Загружает язык из JSON-файла.</summary>
        public static bool LoadLanguage(string code)
        {
            if (!SupportedLanguages.Contains(code))
                code = DEFAULT_LANGUAGE;

            try
            {
                string path = FindStringsFile(code);
                if (path == null)
                {
                    _strings = new Dictionary<string, JsonElement>();
                    _currentLanguage = DEFAULT_LANGUAGE;
                    return false;
                }

                string json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json);
                _strings = new Dictionary<string, JsonElement>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                    _strings[prop.Name] = prop.Value;

                _currentLanguage = code;
                return true;
            }
            catch (Exception)
            {
                _strings = new Dictionary<string, JsonElement>();
                _currentLanguage = DEFAULT_LANGUAGE;
                return false;
            }
        }

        /// <summary>
        /// Возвращает переведённую строку по ключу вида "section.key".
        /// Если ключ не найден — возвращает сам ключ.
        /// </summary>
        public static string Get(string key)
        {
            if (_strings == null) return key;
            var parts = key.Split('.', 2);
            if (parts.Length != 2) return key;

            if (!_strings.TryGetValue(parts[0], out var section))
                return key;

            if (section.ValueKind != JsonValueKind.Object)
                return key;

            if (section.TryGetProperty(parts[1], out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();

            return key;
        }

        /// <summary>Сокращение для Get().</summary>
        public static string S(string key) => Get(key);

        /// <summary>Возвращает список доступных языков с именами.</summary>
        public static List<(string Code, string Native, string English)> GetAvailableLanguages()
        {
            var list = new List<(string, string, string)>();
            foreach (var code in SupportedLanguages)
            {
                if (LanguageNames.TryGetValue(code, out var names))
                    list.Add((code, names.Native, names.English));
            }
            return list;
        }

        /// <summary>Сохраняет выбор языка в реестр.</summary>
        public static void SaveLanguage(string code)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\SystemRestoreTool");
                key?.SetValue("Language", code, Microsoft.Win32.RegistryValueKind.String);
            }
            catch { }
        }

        /// <summary>Загружает сохранённый язык из реестра.</summary>
        public static string GetSavedLanguage()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\SystemRestoreTool");
                return key?.GetValue("Language") as string;
            }
            catch { return null; }
        }

        // -----------------------------------------------------------------

        private static string FindStringsFile(string code)
        {
            // 1. Сначала пробуем embedded resource (для single-file publish)
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string resourceName = $"SystemRestoreTool.Resources.strings.{code}.json";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    // Сохраняем во временный файл и возвращаем путь
                    string tempPath = Path.Combine(Path.GetTempPath(), $"srt_strings_{code}.json");
                    using var fs = File.Create(tempPath);
                    stream.CopyTo(fs);
                    return tempPath;
                }
            }
            catch { /* игнор */ }

            // 2. Папка Resources/ рядом с исполняемым файлом
            string exeDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(exeDir,             "Resources", $"strings.{code}.json"),
                Path.Combine(exeDir, "..",       "Resources", $"strings.{code}.json"),
                Path.Combine(exeDir, "..", "..", "Resources", $"strings.{code}.json"),
                Path.Combine(exeDir,             $"strings.{code}.json"),
            };

            foreach (var p in candidates)
            {
                if (File.Exists(p)) return p;
            }

            return null;
        }
    }
}
