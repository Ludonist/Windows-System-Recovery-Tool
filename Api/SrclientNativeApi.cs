using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  SRCLIENT NATIVE API  —  srclient.dll
    //  Управление точками восстановления системы (System Restore Points).
    //  Эквивалент: SystemPropertiesProtection.exe / rstrui.exe
    // ===========================================================================

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct RestorePointInfo
    {
        public uint dwRestorePtType;     // Тип точки (см. RestorePointType)
        public uint dwEventType;         // BEGIN_SYSTEM_CHANGE / END_SYSTEM_CHANGE
        public long llSequenceNumber;    // 0 для новой точки
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct StateMgrStatus
    {
        public uint nStatus;             // Код ошибки
        public long llSequenceNumber;    // Присвоенный номер
    }

    /// <summary>Тип точки восстановления.</summary>
    public enum RestorePointType : uint
    {
        /// <summary>Установка приложения (откат возможен).</summary>
        ApplicationInstall = 0,
        /// <summary>Удаление приложения.</summary>
        ApplicationUninstall = 1,
        /// <summary>Изменение устройства.</summary>
        DeviceDriverInstall = 10,
        /// <summary>Изменение настроек (только для просмотра).</summary>
        ModifySettings = 12,
        /// <summary>Отмена последней операции.</summary>
        CancelledOperation = 13
    }

    /// <summary>Тип события.</summary>
    public enum EventType : uint
    {
        BeginSystemChange = 100,
        EndSystemChange   = 101,
        BeginNestedSystemChange = 102,
        EndNestedSystemChange   = 103
    }

    public static class SrclientNativeApi
    {
        private const string SRCLIENT = "srclient.dll";

        /// <summary>Создаёт новую точку восстановления системы.</summary>
        [DllImport(SRCLIENT, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SRSetRestorePointW(
            ref RestorePointInfo restorePointInfo,
            out StateMgrStatus smgrStatus);

        /// <summary>Удаляет все точки восстановления старше указанной даты.</summary>
        [DllImport(SRCLIENT, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SRRemoveRestorePoint(int dwRPNum);

        // Дополнительные неэкспортируемые напрямую функции доступны через COM:
        // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore
        // CLSID {39DBE5A0-1FB6-11d2-B838-00C04FCD7422} → ISystemRestore

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Создаёт точку восстановления с указанным описанием.</summary>
        public static bool CreateRestorePoint(string description,
            RestorePointType type = RestorePointType.ModifySettings)
        {
            var info = new RestorePointInfo
            {
                dwRestorePtType = (uint)type,
                dwEventType = (uint)EventType.BeginSystemChange,
                llSequenceNumber = 0,
                szDescription = description ?? "System Restore Tool"
            };

            bool ok = SRSetRestorePointW(ref info, out var status);
            if (ok)
            {
                // Завершаем создание точки
                info.dwEventType = (uint)EventType.EndSystemChange;
                info.llSequenceNumber = status.llSequenceNumber;
                SRSetRestorePointW(ref info, out _);
            }
            return ok;
        }

        /// <summary>Проверяет, включена ли защита системы на диске C:.</summary>
        public static bool IsSystemRestoreEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
                if (key == null) return false;

                // "RPSessionInterval" > 0 означает, что восстановление включено
                var val = key.GetValue("RPSessionInterval");
                return val != null && (int)val >= 0;
            }
            catch { return false; }
        }
    }
}
