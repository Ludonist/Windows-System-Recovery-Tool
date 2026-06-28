using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  EVENT LOG NATIVE API  —  advapi32.dll + wevtapi.dll
    //  Резервное копирование, очистка и восстановление журналов событий.
    //  Эквивалент: wevtutil.exe / wmic.exe
    // ===========================================================================

    public static class EventLogNativeApi
    {
        private const string ADVAPI32 = "advapi32.dll";
        private const string WEVTAPI  = "wevtapi.dll";

        // --- OpenBackupEventLog / BackupEventLog / ClearEventLog (advapi32) --

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenBackupEventLogW(string lpUNCServerName, string lpFileName);

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenEventLogW(string lpUNCServerName, string lpSourceName);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BackupEventLogW(IntPtr hEventLog, string lpBackupFileName);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClearEventLogW(IntPtr hEventLog, string lpBackupFileName);

        [DllImport(ADVAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseEventLog(IntPtr hEventLog);

        // --- Wevtapi.dll — современный API для журналов (Win Vista+) -------

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int EvtClearLog(
            IntPtr Session,
            string ChannelPath,
            string TargetFilePath,
            IntPtr Query);

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int EvtExportLog(
            IntPtr Session,
            string Path,
            string Query,
            string TargetFilePath,
            int Flags);

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int EvtQuery(
            IntPtr Session,
            string Path,
            string Query,
            int Flags,
            out IntPtr ResultSet);

        [DllImport(WEVTAPI, SetLastError = true)]
        public static extern bool EvtClose(IntPtr Object);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Бэкап канала журнала в .evtx файл.</summary>
        public static bool BackupChannel(string channelName, string outputFile)
        {
            // Сначала пробуем современный wevtapi (Win 10+)
            int hr = EvtClearLog(IntPtr.Zero, channelName, outputFile, IntPtr.Zero);
            if (hr == 0) return true;

            // Фолбэк на старый advapi32
            IntPtr hLog = OpenEventLogW(null, channelName);
            if (hLog == IntPtr.Zero) return false;
            try
            {
                return BackupEventLogW(hLog, outputFile);
            }
            finally { CloseEventLog(hLog); }
        }

        /// <summary>Очистка канала журнала (с опциональным бэкапом).</summary>
        public static bool ClearChannel(string channelName, string backupFile = null)
        {
            // Wevtapi
            int hr = EvtClearLog(IntPtr.Zero, channelName, backupFile, IntPtr.Zero);
            if (hr == 0) return true;

            // Advapi32 fallback
            IntPtr hLog = OpenEventLogW(null, channelName);
            if (hLog == IntPtr.Zero) return false;
            try
            {
                return ClearEventLogW(hLog, backupFile);
            }
            finally { CloseEventLog(hLog); }
        }
    }
}
