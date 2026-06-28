using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  SERVICES NATIVE API  —  advapi32.dll (Service Control Manager)
    //  Управление системными службами: запуск/остановка/восстановление.
    //  Эквивалент: sc.exe / net.exe
    // ===========================================================================

    public static class ServicesNativeApi
    {
        private const string ADVAPI32 = "advapi32.dll";

        public const uint SC_MANAGER_CONNECT          = 0x0001;
        public const uint SC_MANAGER_CREATE_SERVICE   = 0x0002;
        public const uint SC_MANAGER_ENUMERATE_SERVICE= 0x0004;
        public const uint SC_MANAGER_ALL_ACCESS       = 0xF003F;

        public const uint SERVICE_QUERY_CONFIG        = 0x0001;
        public const uint SERVICE_CHANGE_CONFIG       = 0x0002;
        public const uint SERVICE_QUERY_STATUS        = 0x0004;
        public const uint SERVICE_ENUMERATE_DEPENDENTS= 0x0008;
        public const uint SERVICE_START               = 0x0010;
        public const uint SERVICE_STOP                = 0x0020;
        public const uint SERVICE_PAUSE_CONTINUE      = 0x0040;
        public const uint SERVICE_INTERROGATE         = 0x0080;
        public const uint SERVICE_USER_DEFINED_CONTROL= 0x0100;
        public const uint SERVICE_ALL_ACCESS          = 0xF01FF;

        public const uint SERVICE_KERNEL_DRIVER       = 0x00000001;
        public const uint SERVICE_FILE_SYSTEM_DRIVER  = 0x00000002;
        public const uint SERVICE_WIN32_OWN_PROCESS   = 0x00000010;
        public const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;
        public const uint SERVICE_INTERACTIVE_PROCESS = 0x00000100;

        public const uint SERVICE_BOOT_START          = 0x00000000;
        public const uint SERVICE_SYSTEM_START        = 0x00000001;
        public const uint SERVICE_AUTO_START          = 0x00000002;
        public const uint SERVICE_DEMAND_START        = 0x00000003;
        public const uint SERVICE_DISABLED            = 0x00000004;

        public const uint SERVICE_ERROR_IGNORE        = 0x00000000;
        public const uint SERVICE_ERROR_NORMAL        = 0x00000001;
        public const uint SERVICE_ERROR_SEVERE        = 0x00000002;
        public const uint SERVICE_ERROR_CRITICAL      = 0x00000003;

        // Состояния службы
        public const uint SERVICE_STOPPED          = 0x00000001;
        public const uint SERVICE_START_PENDING    = 0x00000002;
        public const uint SERVICE_STOP_PENDING     = 0x00000003;
        public const uint SERVICE_RUNNING          = 0x00000004;
        public const uint SERVICE_CONTINUE_PENDING = 0x00000005;
        public const uint SERVICE_PAUSE_PENDING    = 0x00000006;
        public const uint SERVICE_PAUSED           = 0x00000007;

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SERVICE_STATUS_PROCESS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
            public uint dwProcessId;
            public uint dwServiceFlags;
        }

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManagerW(
            string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenServiceW(
            IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartServiceW(
            IntPtr hService, uint dwNumServiceArgs, IntPtr lpServiceArgVectors);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService(
            IntPtr hService, uint dwControl, ref SERVICE_STATUS lpServiceStatus);

        public const uint SERVICE_CONTROL_STOP    = 0x00000001;
        public const uint SERVICE_CONTROL_PAUSE   = 0x00000002;
        public const uint SERVICE_CONTROL_CONTINUE= 0x00000003;
        public const uint SERVICE_CONTROL_INTERROGATE = 0x00000004;

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryServiceStatus(
            IntPtr hService, ref SERVICE_STATUS lpServiceStatus);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryServiceStatusEx(
            IntPtr hService, int InfoLevel,
            IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);

        public const int SC_STATUS_PROCESS_INFO = 0;

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfigW(
            IntPtr hService,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword,
            string lpDisplayName);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService(IntPtr hService);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateServiceW(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Запускает службу по имени. Возвращает true, если работает.</summary>
        public static bool StartService(string name)
        {
            IntPtr scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero) return false;
            try
            {
                IntPtr svc = OpenServiceW(scm, name, SERVICE_START | SERVICE_QUERY_STATUS);
                if (svc == IntPtr.Zero) return false;
                try
                {
                    if (!StartServiceW(svc, 0, IntPtr.Zero))
                    {
                        // Если уже запущена — это OK
                        var st = new SERVICE_STATUS();
                        QueryServiceStatus(svc, ref st);
                        return st.dwCurrentState == SERVICE_RUNNING;
                    }
                    return true;
                }
                finally { CloseServiceHandle(svc); }
            }
            finally { CloseServiceHandle(scm); }
        }

        /// <summary>Останавливает службу.</summary>
        public static bool StopService(string name)
        {
            IntPtr scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero) return false;
            try
            {
                IntPtr svc = OpenServiceW(scm, name, SERVICE_STOP | SERVICE_QUERY_STATUS);
                if (svc == IntPtr.Zero) return false;
                try
                {
                    var st = new SERVICE_STATUS();
                    return ControlService(svc, SERVICE_CONTROL_STOP, ref st);
                }
                finally { CloseServiceHandle(svc); }
            }
            finally { CloseServiceHandle(scm); }
        }

        /// <summary>Перезапускает службу (Stop + Start).</summary>
        public static bool RestartService(string name)
        {
            StopService(name);
            System.Threading.Thread.Sleep(2000);
            return StartService(name);
        }

        /// <summary>Возвращает состояние службы по имени.</summary>
        public static uint GetServiceState(string name)
        {
            IntPtr scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero) return 0;
            try
            {
                IntPtr svc = OpenServiceW(scm, name, SERVICE_QUERY_STATUS);
                if (svc == IntPtr.Zero) return 0;
                try
                {
                    var st = new SERVICE_STATUS();
                    return QueryServiceStatus(svc, ref st) ? st.dwCurrentState : 0;
                }
                finally { CloseServiceHandle(svc); }
            }
            finally { CloseServiceHandle(scm); }
        }

        /// <summary>Строковое представление состояния службы.</summary>
        public static string StateToString(uint state)
        {
            return state switch
            {
                SERVICE_STOPPED          => "Stopped",
                SERVICE_START_PENDING    => "Start Pending",
                SERVICE_STOP_PENDING     => "Stop Pending",
                SERVICE_RUNNING          => "Running",
                SERVICE_CONTINUE_PENDING => "Continue Pending",
                SERVICE_PAUSE_PENDING    => "Pause Pending",
                SERVICE_PAUSED           => "Paused",
                _ => $"Unknown ({state})"
            };
        }
    }
}
