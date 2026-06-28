using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  POWER NATIVE API  —  powrprof.dll  +  kernel32.dll
    //  Управление питанием Windows, точки восстановления питания, схемы.
    //  Документация: https://learn.microsoft.com/windows/win32/power/power-management
    // ===========================================================================

    public static class PowerNativeApi
    {
        private const string POWRPROF = "powrprof.dll";
        private const string KERNEL32 = "kernel32.dll";

        // --- Схемы питания -------------------------------------------------

        [DllImport(POWRPROF, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PowerGetActiveScheme(
            IntPtr UserRootPowerKey,
            out IntPtr ActivePolicyGuid);

        [DllImport(POWRPROF, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PowerSetActiveScheme(
            IntPtr UserRootPowerKey,
            IntPtr SchemeGuid);

        [DllImport(POWRPROF, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PowerEnumerate(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupOfPowerSettingsGuid,
            uint AccessFlags,
            uint Index,
            IntPtr Buffer,
            ref uint BufferSize);

        public const uint ACCESS_SCHEME = 16;
        public const uint ACCESS_SUBGROUP = 17;
        public const uint ACCESS_INDIVIDUAL_SETTING = 18;

        // Схемы по умолчанию
        public static readonly Guid GUID_MAX_POWER_SAVINGS =
            new("a1841308-3541-4fab-bc81-f71556f20b4a"); // Power saver
        public static readonly Guid GUID_MIN_POWER_SAVINGS =
            new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // High performance
        public static readonly Guid GUID_TYPICAL_POWER_SAVINGS =
            new("381b4222-f694-41f0-9685-ff5bb260df2e"); // Balanced

        [DllImport(POWRPROF, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PowerReadFriendlyName(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupOfPowerSettingGuid,
            IntPtr SettingGuid,
            IntPtr Buffer,
            ref uint BufferSize);

        [DllImport(POWRPROF, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PowerReadDescription(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupOfPowerSettingGuid,
            IntPtr SettingGuid,
            IntPtr Buffer,
            ref uint BufferSize);

        // --- Состояние питания системы ------------------------------------

        public const uint SYSTEM_POWER_INFORMATION = 12;

        [DllImport(POWRPROF, SetLastError = true)]
        public static extern uint CallNtPowerInformation(
            uint InformationLevel,
            IntPtr InputBuffer,
            uint InputBufferLength,
            IntPtr OutputBuffer,
            uint OutputBufferLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_BATTERY_STATE
        {
            public byte AcOnLine;
            public byte BatteryPresent;
            public byte Charging;
            public byte Discharging;
            public byte Spare1;
            public byte Spare2;
            public byte Spare3;
            public byte Spare4;
            public uint MaxCapacity;
            public uint RemainingCapacity;
            public uint Rate;
            public uint EstimatedTime;
            public uint DefaultAlert1;
            public uint DefaultAlert2;
        }

        public const uint SystemBatteryState = 5;

        [DllImport(POWRPROF, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES lpSystemPowerCapabilities);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_CAPABILITIES
        {
            public byte PowerButtonPresent;
            public byte SleepButtonPresent;
            public byte LidPresent;
            public byte SystemS1;
            public byte SystemS2;
            public byte SystemS3;
            public byte SystemS4;
            public byte SystemS5;
            public byte HiberFilePresent;
            public byte FullWake;
            public byte VideoDimPresent;
            public byte ApmPresent;
            public byte UpsPresent;
            public byte ThermalControl;
            public byte ProcessorThrottle;
            public byte ProcessorMinThrottle;
            public byte ProcessorMaxThrottle;
            public byte FastSystemS4;
            public byte Hiberboot;
            public byte WakeAlarmPresent;
            public byte AiPresent;
            public byte DiskSpinDown;
            public byte HiberFileType;
            public byte AoAc;
            public uint MinDeviceWakeState;
            public uint DefaultLowLatencyWake;
        }

        // --- LastSleepTime / LastWakeTime ---------------------------------

        [DllImport(POWRPROF, SetLastError = true)]
        public static extern uint CallNtPowerInformationForSleepWake(uint InformationLevel,
            IntPtr InputBuffer, uint InputBufferLength,
            IntPtr OutputBuffer, uint OutputBufferLength);

        // --- Управление гибернацией (kernel32) ---------------------------

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsPwrSuspendAllowed();

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsPwrHibernateAllowed();

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSuspendState(
            [MarshalAs(UnmanagedType.Bool)] bool Hibernate,
            [MarshalAs(UnmanagedType.Bool)] bool ForceCritical,
            [MarshalAs(UnmanagedType.Bool)] bool DisableWakeEvent);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Возвращает GUID активной схемы питания.</summary>
        public static Guid? GetActiveSchemeGuid()
        {
            uint hr = PowerGetActiveScheme(IntPtr.Zero, out IntPtr buf);
            if (hr != 0 || buf == IntPtr.Zero) return null;
            try { return Marshal.PtrToStructure<Guid>(buf); }
            finally { Marshal.FreeHGlobal(buf); }
        }

        /// <summary>Устанавливает схему питания по GUID.</summary>
        public static bool SetActiveScheme(Guid scheme)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
            try
            {
                Marshal.StructureToPtr(scheme, ptr, false);
                return PowerSetActiveScheme(IntPtr.Zero, ptr) == 0;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        /// <summary>Включает схему "Максимальная производительность".</summary>
        public static bool SetHighPerformance()
            => SetActiveScheme(GUID_MIN_POWER_SAVINGS);

        /// <summary>Включает схему "Сбалансированная".</summary>
        public static bool SetBalanced()
            => SetActiveScheme(GUID_TYPICAL_POWER_SAVINGS);

        /// <summary>Включает схему "Энергосбережение".</summary>
        public static bool SetPowerSaver()
            => SetActiveScheme(GUID_MAX_POWER_SAVINGS);

        /// <summary>Получает состояние батареи.</summary>
        public static SYSTEM_BATTERY_STATE? GetBatteryState()
        {
            int size = Marshal.SizeOf<SYSTEM_BATTERY_STATE>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                uint hr = CallNtPowerInformation(SystemBatteryState, IntPtr.Zero, 0, buf, (uint)size);
                if (hr != 0) return null;
                return Marshal.PtrToStructure<SYSTEM_BATTERY_STATE>(buf);
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
    }
}
