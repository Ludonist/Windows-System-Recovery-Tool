using System;
using System.IO;
using System.Runtime.InteropServices;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  REGISTRY RESTORE MANAGER
    //  Резервное копирование и восстановление кустов реестра через
    //  advapi32.dll!RegSaveKey / RegRestoreKey. Плюс загрузка/выгрузка кустов
    //  через RegLoadKey / RegUnLoadKey для доступа к OFFLINE-кустам.
    // ===========================================================================

    public static class RegistryRestoreManager
    {
        // Корневые пути к файлам кустов реестра Windows
        public static readonly string[] RegistryHiveFiles =
        {
            @"C:\Windows\System32\config\SAM",
            @"C:\Windows\System32\config\SECURITY",
            @"C:\Windows\System32\config\SOFTWARE",
            @"C:\Windows\System32\config\SYSTEM",
            @"C:\Windows\System32\config\DEFAULT",
            @"C:\Windows\System32\config\COMPONENTS",
            @"C:\Windows\System32\config\NTUSER.DAT",  // default user profile
            @"C:\Windows\System32\config\BCD-Template"
        };

        /// <summary>Создаёт резервную копию всех кустов реестра в указанную папку.</summary>
        public static void BackupAllHives(string backupDir = null)
        {
            backupDir ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemRestoreTool", "RegBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(backupDir);
            Logger.Instance.Header($">>> Резервное копирование кустов реестра → {backupDir}");

            // Запрашиваем привилегии SeBackupPrivilege и SeRestorePrivilege
            if (!EnableBackupRestorePrivileges())
            {
                Logger.Instance.Error("Не удалось запросить SeBackupPrivilege / SeRestorePrivilege.");
                return;
            }

            // 1. Сохраняем через RegSaveKey (управляемые кусты)
            SaveKeyViaApi(Kernel32NativeApi.HKEY_LOCAL_MACHINE, "SAM",    Path.Combine(backupDir, "SAM.save"));
            SaveKeyViaApi(Kernel32NativeApi.HKEY_LOCAL_MACHINE, "SECURITY", Path.Combine(backupDir, "SECURITY.save"));
            SaveKeyViaApi(Kernel32NativeApi.HKEY_LOCAL_MACHINE, "SOFTWARE", Path.Combine(backupDir, "SOFTWARE.save"));
            SaveKeyViaApi(Kernel32NativeApi.HKEY_LOCAL_MACHINE, "SYSTEM",  Path.Combine(backupDir, "SYSTEM.save"));
            SaveKeyViaApi(Kernel32NativeApi.HKEY_LOCAL_MACHINE, "COMPONENTS", Path.Combine(backupDir, "COMPONENTS.save"));

            // 2. Копируем файлы кустов напрямую (для возможности офлайн-восстановления)
            foreach (var hive in RegistryHiveFiles)
            {
                if (!File.Exists(hive))
                {
                    Logger.Instance.Warn($"  Куст не найден: {hive}");
                    continue;
                }
                try
                {
                    var dst = Path.Combine(backupDir, Path.GetFileName(hive));
                    File.Copy(hive, dst, overwrite: true);
                    // Копируем и .LOG файлы транзакций, если есть
                    var log1 = Path.ChangeExtension(hive, ".log");
                    var log2 = hive + ".log1";
                    var log3 = hive + ".log2";
                    foreach (var log in new[] { log1, log2, log3 })
                    {
                        if (File.Exists(log))
                            File.Copy(log, Path.Combine(backupDir, Path.GetFileName(log)), overwrite: true);
                    }
                    Logger.Instance.Success($"  Сохранён: {Path.GetFileName(hive)}");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn($"  Не удалось скопировать {Path.GetFileName(hive)}: {ex.Message}");
                    Logger.Instance.Warn($"  (файл занят системой — это нормально для живых кустов)");
                }
            }

            Logger.Instance.Success($"Бэкап реестра завершён. Папка: {backupDir}");
        }

        /// <summary>Сохраняет подключ реестра в файл через RegSaveKeyW.</summary>
        private static void SaveKeyViaApi(UIntPtr rootKey, string subKey, string outputFile)
        {
            try
            {
                if (File.Exists(outputFile)) File.Delete(outputFile);

                using var key = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64).OpenSubKey(subKey);

                if (key == null)
                {
                    Logger.Instance.Warn($"  Подкуст {subKey} не открылся");
                    return;
                }

                // Используем управляемый RegSaveKey через P/Invoke
                bool ok = Kernel32NativeApi.RegSaveKeyW(rootKey, outputFile, IntPtr.Zero);
                if (!ok)
                {
                    int err = Marshal.GetLastWin32Error();
                    Logger.Instance.Warn($"  RegSaveKeyW для {subKey} вернул ошибку {err} (файл может быть занят)");
                }
                else
                {
                    Logger.Instance.Success($"  Сохранён через API: {Path.GetFileName(outputFile)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"  SaveKeyViaApi({subKey}): {ex.Message}");
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegSaveKeyExW(UIntPtr hKey, string lpFile, IntPtr lpSecurityAttributes, int Flags);

        /// <summary>Восстанавливает куст реестра из файла (требует прав).</summary>
        public static void RestoreHive(UIntPtr rootKey, string subKey, string backupFile)
        {
            try
            {
                int hr = Kernel32NativeApi.RegRestoreKeyW(rootKey, subKey, backupFile, 0);
                if (hr == 0) Logger.Instance.Success($"Куст {subKey} восстановлен из {backupFile}");
                else Logger.Instance.Error($"RegRestoreKeyW({subKey}) → 0x{hr:X}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"RestoreHive: {ex.Message}");
            }
        }

        /// <summary>Запрашивает SeBackupPrivilege и SeRestorePrivilege.</summary>
        private static bool EnableBackupRestorePrivileges()
        {
            return EnablePrivilege("SeBackupPrivilege") && EnablePrivilege("SeRestorePrivilege");
        }

        private static bool EnablePrivilege(string privilegeName)
        {
            try
            {
                if (!Kernel32NativeApi.OpenProcessToken(
                    Kernel32NativeApi.GetCurrentProcess(),
                    Kernel32NativeApi.TOKEN_ADJUST_PRIVILEGES | Kernel32NativeApi.TOKEN_QUERY,
                    out var token))
                    return false;

                var luid = new Kernel32NativeApi.LUID();
                if (!Kernel32NativeApi.LookupPrivilegeValueW(null, privilegeName, ref luid))
                    return false;

                var tp = new Kernel32NativeApi.TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new Kernel32NativeApi.LUID_AND_ATTRIBUTES
                    {
                        Luid = luid,
                        Attributes = Kernel32NativeApi.SE_PRIVILEGE_ENABLED
                    }
                };

                return Kernel32NativeApi.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch { return false; }
        }

        /// <summary>Проверяет наличие резервных копий кустов в RegBack (Win10 < 1803).</summary>
        public static void CheckRegBackFolder()
        {
            var regBack = @"C:\Windows\System32\config\RegBack";
            Logger.Instance.Info($"Проверка папки RegBack: {regBack}");

            if (!Directory.Exists(regBack))
            {
                Logger.Instance.Warn("Папка RegBack не существует (нормально для Win10 1803+).");
                return;
            }

            var files = Directory.GetFiles(regBack);
            if (files.Length == 0)
            {
                Logger.Instance.Warn("Папка RegBack пуста. Microsoft отключила автоматический бэкап с Win10 1803.");
                return;
            }

            Logger.Instance.Success("Найдены резервные копии в RegBack:");
            foreach (var f in files)
            {
                var info = new FileInfo(f);
                Logger.Instance.Info($"  {Path.GetFileName(f),-20} {info.Length,12:N0} байт  ({info.LastWriteTime:yyyy-MM-dd HH:mm})");
            }
        }
    }
}
