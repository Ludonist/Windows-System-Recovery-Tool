using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  KERNEL32 / NTDLL NATIVE API
    //  Восстановление файлов, управление WFP, версия Windows, проверки образов.
    // ===========================================================================

    public static class Kernel32NativeApi
    {
        private const string KERNEL32 = "kernel32.dll";
        private const string NTDLL    = "ntdll.dll";
        private const string USERENV  = "userenv.dll";

        // --- Версия Windows ------------------------------------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVersionExW(ref OSVERSIONINFOEX lpVersionInfo);

        // --- SystemParametersInfo (восстановление обоев/скринсейвера) -------

        public const uint SPI_SETDESKWALLPAPER  = 0x0014;
        public const uint SPI_SETSCREENSAVETIMEOUT = 0x000F;
        public const uint SPI_SETSCREENSAVEACTIVE  = 0x0011;
        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE    = 0x02;

        [DllImport(USERENV, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SystemParametersInfoW(
            uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        // --- MoveFileEx (отложенное удаление/переименование) ----------------

        public const uint MOVEFILE_REPLACE_EXISTING      = 0x00000001;
        public const uint MOVEFILE_COPY_ALLOWED          = 0x00000002;
        public const uint MOVEFILE_DELAY_UNTIL_REBOOT    = 0x00000004;
        public const uint MOVEFILE_WRITE_THROUGH         = 0x00000008;

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveFileExW(
            string lpExistingFileName, string lpNewFileName, uint dwFlags);

        // --- Флаги replacing файла при перезагрузке -----------------------

        /// <summary>
        /// Помечает файл на удаление при следующей перезагрузке.
        /// Используется когда файл занят и не может быть удалён сейчас.
        /// </summary>
        public static bool ScheduleDeleteOnReboot(string filePath)
        {
            // Аналог MoveFileEx(..., null, MOVEFILE_DELAY_UNTIL_REBOOT)
            return MoveFileExW(filePath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
        }

        /// <summary>
        /// Помечает файл на замену при следующей перезагрузке.
        /// </summary>
        public static bool ScheduleReplaceOnReboot(string src, string dst)
        {
            return MoveFileExW(src, dst,
                MOVEFILE_REPLACE_EXISTING | MOVEFILE_DELAY_UNTIL_REBOOT);
        }

        // --- Перезагрузка системы -----------------------------------------

        public const uint SE_SHUTDOWN_NAME = 0;
        public const uint EWX_LOGOFF   = 0x00000000;
        public const uint EWX_REBOOT   = 0x00000002;
        public const uint EWX_FORCE    = 0x00000004;
        public const uint EWX_POWEROFF = 0x00000008;
        public const uint SHTDN_REASON_FLAG_PLANNED         = 0x80000000;
        public const uint SHTDN_REASON_MAJOR_OPERATINGSYSTEM = 0x00020000;
        public const uint SHTDN_REASON_MINOR_RECONFIG       = 0x00000004;

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValueW(
            string lpSystemName, string lpName, ref LUID lpLuid);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint Zero,
            IntPtr Null1,
            IntPtr Null2);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        private const string ADVAPI32 = "advapi32.dll";

        /// <summary>Запрос привилегии SeShutdownPrivilege и перезагрузка.</summary>
        public static bool RebootSystem()
        {
            if (!OpenProcessToken(GetCurrentProcess(),
                TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token))
                return false;

            var luid = new LUID();
            if (!LookupPrivilegeValueW(null, "SeShutdownPrivilege", ref luid))
                return false;

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                }
            };

            AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);

            return ExitWindowsEx(EWX_REBOOT | EWX_FORCE,
                SHTDN_REASON_FLAG_PLANNED | SHTDN_REASON_MAJOR_OPERATINGSYSTEM |
                SHTDN_REASON_MINOR_RECONFIG);
        }

        // --- CreateFile / DeviceIoControl (для Raw I/O, разделов) ----------

        public const uint GENERIC_READ       = 0x80000000;
        public const uint GENERIC_WRITE      = 0x40000000;
        public const uint OPEN_EXISTING      = 3;
        public const uint FILE_SHARE_READ    = 0x00000001;
        public const uint FILE_SHARE_WRITE   = 0x00000002;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        public const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        // --- VSS (Volume Shadow Copy) через kernel32!CreateFileW к VSS device --

        public static readonly Guid GUID_DEVINTERFACE_VOLUME =
            new("{53f5630d-b6bf-11d0-94f2-00a0c91efb8b}");

        // --- Загрузка/выгрузка кустов реестра ------------------------------

        public const uint SE_RESTORE_NAME_PRIVILEGE = 0;
        public const uint SE_BACKUP_NAME_PRIVILEGE  = 0;

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegLoadKeyW(
            UIntPtr hKey, string lpSubKey, string lpFile);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegUnLoadKeyW(
            UIntPtr hKey, string lpSubKey);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegSaveKeyW(
            UIntPtr hKey, string lpFile, IntPtr lpSecurityAttributes);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int RegRestoreKeyW(
            UIntPtr hKey, string lpSubKey, string lpFile, uint dwFlags);

        public static readonly UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002);
        public static readonly UIntPtr HKEY_USERS         = new UIntPtr(0x80000003);

        // --- Process modules (для проверки загруженных DLL) ----------------

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryW(string lpLibFileName);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandleW(string lpModuleName);
    }
}
