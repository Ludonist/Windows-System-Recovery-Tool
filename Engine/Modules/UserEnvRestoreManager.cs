using System;
using System.IO;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  USER ENVIRONMENT RESTORE MANAGER
    //  Восстановление профилей пользователей и их настроек:
    //    • Проверка целостности NTUSER.DAT
    //    • Проверка Standard Profile / Mandatory Profile
    //    • Сброс повреждённых профильных кэшей
    //  Эквивалент: SystemPropertiesAdvanced.exe → User Profiles
    // ===========================================================================

    public static class UserEnvRestoreManager
    {
        /// <summary>Проверяет целостность профилей пользователей.</summary>
        public static void CheckUserProfiles()
        {
            Logger.Instance.Header(">>> Проверка профилей пользователей");

            // 1. Профили в реестре
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
                if (key == null)
                {
                    Logger.Instance.Error("ProfileList не найден в реестре — критическая проблема!");
                    return;
                }

                Logger.Instance.Success("Найденные профили пользователей:");
                foreach (var sid in key.GetSubKeyNames())
                {
                    using var profKey = key.OpenSubKey(sid);
                    if (profKey == null) continue;

                    string imagePath = profKey.GetValue("ProfileImagePath") as string;
                    if (string.IsNullOrEmpty(imagePath)) continue;

                    bool exists = Directory.Exists(imagePath);
                    string ntuser = Path.Combine(imagePath, "NTUSER.DAT");
                    bool ntuserExists = File.Exists(ntuser);

                    if (exists && ntuserExists)
                        Logger.Instance.Success($"  [OK] {sid} → {imagePath}");
                    else if (exists && !ntuserExists)
                        Logger.Instance.Error($"  [DAMAGED] {sid} → {imagePath} (NTUSER.DAT отсутствует!)");
                    else
                        Logger.Instance.Error($"  [ORPHAN] {sid} → {imagePath} (папка не существует!)");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"CheckUserProfiles: {ex.Message}");
            }

            // 2. Проверка Default User
            string defaultProfile = @"C:\Users\Default";
            if (Directory.Exists(defaultProfile))
            {
                string defNtuser = Path.Combine(defaultProfile, "NTUSER.DAT");
                if (File.Exists(defNtuser))
                    Logger.Instance.Success($"  Default profile OK: {defNtuser}");
                else
                    Logger.Instance.Warn($"  Default profile повреждён: NTUSER.DAT отсутствует");
            }

            // 3. Public profile
            string publicProfile = @"C:\Users\Public";
            if (Directory.Exists(publicProfile))
                Logger.Instance.Success($"  Public profile OK: {publicProfile}");
            else
                Logger.Instance.Error("  Public profile ОТСУТСТВУЕТ — это критическая проблема!");
        }

        /// <summary>Проверяет и при необходимости сбрасывает кэш иконок.</summary>
        public static void RebuildIconCache()
        {
            Logger.Instance.Header(">>> Перестроение кэша иконок");

            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string iconCache = Path.Combine(localAppData, "IconCache.db");
                string explorerFolder = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");

                if (File.Exists(iconCache))
                {
                    File.Delete(iconCache);
                    Logger.Instance.Success($"Удалён: {iconCache}");
                }

                if (Directory.Exists(explorerFolder))
                {
                    foreach (var f in Directory.EnumerateFiles(explorerFolder, "iconcache_*.db"))
                    {
                        try { File.Delete(f); Logger.Instance.Success($"Удалён: {Path.GetFileName(f)}"); }
                        catch (Exception ex) { Logger.Instance.Warn($"  Не удалось удалить {Path.GetFileName(f)}: {ex.Message}"); }
                    }
                }

                Logger.Instance.Info("Перезапустите explorer.exe, чтобы кэш перестроился.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"RebuildIconCache: {ex.Message}");
            }
        }

        /// <summary>Проверяет и чистит кэш шрифтов.</summary>
        public static void RebuildFontCache()
        {
            Logger.Instance.Header(">>> Перестроение кэша шрифтов");

            string fontCacheDir = @"C:\Windows\ServiceProfiles\LocalService\AppData\Local\FontCache";
            string fontCacheFile = @"C:\Windows\System32\FNTCACHE.DAT";

            try
            {
                // Останавливаем службу FontCache
                Logger.Instance.Info("Остановка службы FontCache...");
                ServicesNativeApi.StopService("FontCache");
                ServicesNativeApi.StopService("FontCache3.0.0.0");

                if (File.Exists(fontCacheFile))
                {
                    File.Delete(fontCacheFile);
                    Logger.Instance.Success($"Удалён: {fontCacheFile}");
                }

                if (Directory.Exists(fontCacheDir))
                {
                    foreach (var f in Directory.EnumerateFiles(fontCacheDir, "*.dat"))
                    {
                        try { File.Delete(f); Logger.Instance.Success($"Удалён: {Path.GetFileName(f)}"); }
                        catch { /* может быть занят */ }
                    }
                }

                // Запускаем обратно
                ServicesNativeApi.StartService("FontCache");
                ServicesNativeApi.StartService("FontCache3.0.0.0");

                Logger.Instance.Success("Кэш шрифтов будет перестроен автоматически.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"RebuildFontCache: {ex.Message}");
            }
        }

        /// <summary>Проверка наличия ключевых пользовательских DLL.</summary>
        public static void CheckUserProfileDlls()
        {
            Logger.Instance.Header(">>> Проверка DLL пользовательского окружения");

            string sysDir = Environment.SystemDirectory;
            string[] userDlls =
            {
                "userenv.dll",
                "profapi.dll",
                "ntdll.dll",
                "kernel32.dll",
                "user32.dll",
                "shell32.dll",
                "shlwapi.dll",
                "msvcp_win.dll",
                "wininet.dll",
                "urlmon.dll",
                "iertutil.dll"
            };

            foreach (var dll in userDlls)
            {
                string path = Path.Combine(sysDir, dll);
                if (!File.Exists(path))
                {
                    Logger.Instance.Error($"  [ОТСУТСТВУЕТ] {path}");
                    continue;
                }

                var sig = SignatureVerifier.Verify(path);
                if (sig.IsValid && sig.IsMicrosoft)
                    Logger.Instance.Success($"  [OK] {dll}");
                else if (!sig.IsSigned)
                    Logger.Instance.Error($"  [NO-SIG] {dll} — подпись отсутствует!");
                else if (!sig.IsMicrosoft)
                    Logger.Instance.Error($"  [3RDPARTY] {dll} — подписан: {sig.Publisher}");
                else
                    Logger.Instance.Warn($"  [BAD] {dll} — {sig.Error}");
            }
        }
    }
}
