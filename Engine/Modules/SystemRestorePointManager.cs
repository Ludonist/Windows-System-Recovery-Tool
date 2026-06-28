using System;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  SYSTEM RESTORE POINT MANAGER
    //  Создание и управление точками восстановления через srclient.dll API.
    //  Позволяет перед любыми опасными операциями создать точку отката.
    // ===========================================================================

    public static class SystemRestorePointManager
    {
        /// <summary>Создаёт точку восстановления перед восстановлением системы.</summary>
        public static bool CreatePreRestorePoint()
        {
            Logger.Instance.Header(">>> Создание точки восстановления (srclient.dll!SRSetRestorePoint)");

            if (!SrclientNativeApi.IsSystemRestoreEnabled())
            {
                Logger.Instance.Warn("System Restore отключён в системе (gpedit / SystemPropertiesProtection).");
                Logger.Instance.Warn("Точка восстановления НЕ будет создана. Продолжаем без неё.");
                return false;
            }

            try
            {
                string desc = $"System Restore Tool — точка перед восстановлением ({DateTime.Now:yyyy-MM-dd HH:mm})";
                bool ok = SrclientNativeApi.CreateRestorePoint(desc, RestorePointType.ModifySettings);
                if (ok)
                {
                    Logger.Instance.Success("Точка восстановления СОЗДАНА: " + desc);
                    return true;
                }
                Logger.Instance.Warn("SRSetRestorePoint вернул false — возможно достигнут лимит точек.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Не удалось создать точку восстановления: {ex.Message}");
                return false;
            }
        }

        /// <summary>Создаёт точку восстановления с произвольным описанием.</summary>
        public static bool CreateRestorePoint(string description, RestorePointType type = RestorePointType.ModifySettings)
        {
            try
            {
                bool ok = SrclientNativeApi.CreateRestorePoint(description, type);
                if (ok) Logger.Instance.Success("Точка восстановления создана: " + description);
                else Logger.Instance.Warn("Не удалось создать точку (лимит в сутки?)");
                return ok;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>Включает System Restore через реестр.</summary>
        public static bool EnableSystemRestore()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
                key?.SetValue("RPSessionInterval", 1, Microsoft.Win32.RegistryValueKind.DWord);
                key?.SetValue("DisableSR", 0, Microsoft.Win32.RegistryValueKind.DWord);
                Logger.Instance.Success("System Restore включён через реестр (перезапустите srservice).");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Не удалось включить: {ex.Message}");
                return false;
            }
        }
    }
}
