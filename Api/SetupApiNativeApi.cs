using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  SETUPAPI NATIVE API  —  setupapi.dll
    //  Установщик устройств и управление драйверами.
    //  Документация: https://learn.microsoft.com/windows/win32/api/setupapi/
    // ===========================================================================

    public static class SetupApiNativeApi
    {
        private const string SETUPAPI = "setupapi.dll";

        // --- Флаги SetupDiGetClassDevs -----------------------------------

        public const uint DIGCF_DEFAULT         = 0x00000001;
        public const uint DIGCF_PRESENT         = 0x00000002;
        public const uint DIGCF_ALLCLASSES      = 0x00000004;
        public const uint DIGCF_PROFILE         = 0x00000008;
        public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        // --- Флаги состояния устройства (SPDRP_*) -------------------------

        public const uint SPDRP_DEVICEDESC                  = 0x00000000;
        public const uint SPDRP_HARDWAREID                  = 0x00000001;
        public const uint SPDRP_COMPATIBLEIDS               = 0x00000002;
        public const uint SPDRP_SERVICE                     = 0x00000004;
        public const uint SPDRP_CLASS                       = 0x00000007;
        public const uint SPDRP_CLASSGUID                   = 0x00000008;
        public const uint SPDRP_DRIVER                      = 0x00000009;
        public const uint SPDRP_CONFIGFLAGS                 = 0x0000000A;
        public const uint SPDRP_MFG                         = 0x0000000B;
        public const uint SPDRP_FRIENDLYNAME                = 0x0000000C;
        public const uint SPDRP_LOCATION_INFORMATION        = 0x0000000D;
        public const uint SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E;
        public const uint SPDRP_UI_NUMBER                   = 0x00000010;
        public const uint SPDRP_UPPERFILTERS                = 0x00000011;
        public const uint SPDRP_LOWERFILTERS                = 0x00000012;
        public const uint SPDRP_INSTALL_STATE               = 0x00000022;

        // --- Структуры ----------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        // --- SetupDiGetClassDevs / SetupDiEnumDeviceInfo / SetupDiGetDeviceRegistryProperty ---

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            string Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport(SETUPAPI, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport(SETUPAPI, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        // --- SetupDiBuildDriverInfoList / SetupDiCallClassInstaller -------

        public const uint DIF_REMOVE          = 0x00000005;
        public const uint DIF_INSTALLDEVICE   = 0x00000002;
        public const uint DIF_UPDATEDRIVER    = 0x00000019;
        public const uint DIF_SCANFORHARDWARECHANGES = 0x00000020;

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiCallClassInstaller(
            uint InstallFunction,
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        // --- SetupScanForHardwareChanges (через CM_* функции) ------------

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Reenumerate_DevNode(uint dnDevInst, uint ulFlags);

        // --- SetupCopyOEMInf / SetupUninstallOEMInf ----------------------

        public const uint SPOST_NONE = 0;
        public const uint SPOST_PATH = 1;
        public const uint SPOST_URL  = 2;
        public const uint SPOST_MAX  = 3;

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupUninstallOEMInfW(
            string InfName,
            uint Flags,
            IntPtr Reserved);

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupCopyOEMInfW(
            string SourceInfFileName,
            string OEMSourceMediaLocation,
            uint OEMSourceMediaType,
            uint CopyStyle,
            StringBuilder DestinationInfFileName,
            uint DestinationInfFileNameSize,
            out uint RequiredSize,
            IntPtr DestinationInfFileNameComponent);

        // --- SetupAPI generic setup functions ----------------------------

        [DllImport(SETUPAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiRestartDevices(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Запрашивает переперечисление устройств (Scan for hardware changes).</summary>
        public static bool ScanForHardwareChanges()
        {
            // CM_Reenumerate_DevNode с флагом 0 = обычное переперечисление
            uint hr = CM_Reenumerate_DevNode(0, 0x00000001 /* CM_REENUMERATE_NORMAL */);
            return hr == 0;
        }

        /// <summary>Возвращает количество устройств в системе.</summary>
        public static int EnumerateAllDevices(Action<string, string> onDevice = null)
        {
            IntPtr hDevInfo = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (hDevInfo == new IntPtr(-1) || hDevInfo == IntPtr.Zero) return 0;

            try
            {
                int count = 0;
                var did = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                for (uint i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, ref did); i++)
                {
                    string desc = GetDeviceProperty(hDevInfo, ref did, SPDRP_DEVICEDESC);
                    string cls  = GetDeviceProperty(hDevInfo, ref did, SPDRP_CLASS);
                    onDevice?.Invoke(desc ?? "(без описания)", cls ?? "?");
                    count++;
                }
                return count;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
        }

        private static string GetDeviceProperty(IntPtr hDevInfo, ref SP_DEVINFO_DATA did, uint propId)
        {
            byte[] buffer = new byte[2048];
            if (!SetupDiGetDeviceRegistryProperty(hDevInfo, ref did, propId,
                out _, buffer, (uint)buffer.Length, out _))
                return null;

            // REG_SZ = 1, REG_MULTI_SZ = 7
            int len = 0;
            for (int i = 0; i < buffer.Length - 1; i += 2)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0) { len = i; break; }
                len = i + 2;
            }
            return System.Text.Encoding.Unicode.GetString(buffer, 0, len).TrimEnd('\0');
        }
    }
}
