using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  NET NATIVE API  —  winhttp.dll  +  wininet.dll  +  winsock.dll
    //  Диагностика сети: HTTP-проверки, прокси, Winsock-состояние.
    // ===========================================================================

    public static class NetNativeApi
    {
        private const string WINHTTP  = "winhttp.dll";
        private const string WININET  = "wininet.dll";
        private const string WINSOCK  = "ws2_32.dll";

        // --- WinHTTP: проверка доступности URL ----------------------------

        public const uint WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
        public const uint WINHTTP_ACCESS_TYPE_NO_PROXY      = 1;
        public const uint WINHTTP_ACCESS_TYPE_NAMED_PROXY   = 3;
        public const uint WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY = 4;

        public const uint WINHTTP_FLAG_SECURE              = 0x00800000;

        public const uint WINHTTP_QUERY_STATUS_CODE         = 19;
        public const uint WINHTTP_QUERY_STATUS_TEXT         = 20;
        public const uint WINHTTP_QUERY_RAW_HEADERS         = 21;
        public const uint WINHTTP_QUERY_SERVER              = 17;

        [DllImport(WINHTTP, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpOpen(
            string pszAgentW,
            uint dwAccessType,
            string pszProxyW,
            string pszProxyBypassW,
            uint dwFlags);

        [DllImport(WINHTTP, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpCloseHandle(IntPtr hInternet);

        [DllImport(WINHTTP, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpConnect(
            IntPtr hSession,
            string pswzServerName,
            ushort nServerPort,
            uint dwReserved);

        public const ushort INTERNET_DEFAULT_HTTP_PORT  = 80;
        public const ushort INTERNET_DEFAULT_HTTPS_PORT = 443;

        [DllImport(WINHTTP, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpOpenRequest(
            IntPtr hConnect,
            string pwszVerb,
            string pwszObjectName,
            string pwszVersion,
            string pwszReferrer,
            string[] ppwszAcceptTypes,
            uint dwFlags);

        [DllImport(WINHTTP, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSendRequest(
            IntPtr hRequest,
            string pwszHeaders,
            uint dwHeadersLength,
            IntPtr lpOptional,
            uint dwOptionalLength,
            uint dwTotalLength,
            IntPtr dwContext);

        [DllImport(WINHTTP, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReceiveResponse(IntPtr hRequest, IntPtr lpReserved);

        [DllImport(WINHTTP, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryHeaders(
            IntPtr hRequest,
            uint dwInfoLevel,
            string pwszName,
            IntPtr lpBuffer,
            ref uint lpdwBufferLength,
            ref uint lpdwIndex);

        // --- WinINet: проверка подключения --------------------------------

        public const uint INTERNET_CONNECTION_MODEM      = 0x01;
        public const uint INTERNET_CONNECTION_LAN        = 0x02;
        public const uint INTERNET_CONNECTION_PROXY      = 0x04;
        public const uint INTERNET_CONNECTION_MODEM_BUSY = 0x08;
        public const uint INTERNET_CONNECTION_OFFLINE    = 0x20;
        public const uint INTERNET_CONNECTION_CONFIGURED = 0x40;

        [DllImport(WININET, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InternetGetConnectedState(out uint lpdwFlags, uint dwReserved);

        // --- Winsock: WSAStartup / WSACleanup / getaddrinfo --------------

        [StructLayout(LayoutKind.Sequential)]
        public struct WSAData
        {
            public ushort wVersion;
            public ushort wHighVersion;
            public ushort iMaxSockets;
            public ushort iMaxUdpDg;
            public IntPtr lpVendorInfo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public string szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            public string szSystemStatus;
        }

        [DllImport(WINSOCK, SetLastError = true)]
        public static extern int WSAStartup(ushort wVersionRequested, out WSAData lpWSAData);

        [DllImport(WINSOCK, SetLastError = true)]
        public static extern int WSACleanup();

        [DllImport(WINSOCK, SetLastError = true)]
        public static extern int WSAGetLastError();

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Проверяет подключение к интернету через WinINet.</summary>
        public static bool IsConnectedToInternet(out uint flags)
            => InternetGetConnectedState(out flags, 0);

        /// <summary>Проверяет доступность сервера через WinHTTP (HTTP GET).</summary>
        public static int? CheckUrl(string host, ushort port, string path, bool useHttps)
        {
            IntPtr hSession = IntPtr.Zero, hConnect = IntPtr.Zero, hRequest = IntPtr.Zero;
            try
            {
                hSession = WinHttpOpen("SystemRestoreTool", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, null, null, 0);
                if (hSession == IntPtr.Zero) return null;

                hConnect = WinHttpConnect(hSession, host, port, 0);
                if (hConnect == IntPtr.Zero) return null;

                hRequest = WinHttpOpenRequest(hConnect, "HEAD", path, "HTTP/1.1", null, null,
                    useHttps ? WINHTTP_FLAG_SECURE : 0);
                if (hRequest == IntPtr.Zero) return null;

                if (!WinHttpSendRequest(hRequest, null, 0, IntPtr.Zero, 0, 0, IntPtr.Zero))
                    return null;

                if (!WinHttpReceiveResponse(hRequest, IntPtr.Zero))
                    return null;

                // Получаем код статуса (HTTP 200, 404 и т.д.)
                uint bufSize = 4;
                IntPtr buf = Marshal.AllocHGlobal((int)bufSize);
                try
                {
                    uint idx = 0;
                    if (!WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_STATUS_CODE, null, buf, ref bufSize, ref idx))
                        return null;

                    return (int)Marshal.PtrToStructure<uint>(buf);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            finally
            {
                if (hRequest != IntPtr.Zero) WinHttpCloseHandle(hRequest);
                if (hConnect != IntPtr.Zero) WinHttpCloseHandle(hConnect);
                if (hSession != IntPtr.Zero) WinHttpCloseHandle(hSession);
            }
        }

        /// <summary>Инициализирует Winsock.</summary>
        public static bool InitWinsock()
        {
            var data = new WSAData();
            return WSAStartup(0x0202, out data) == 0;
        }

        /// <summary>Деинициализирует Winsock.</summary>
        public static void CleanupWinsock()
        {
            try { WSACleanup(); } catch { }
        }
    }
}
