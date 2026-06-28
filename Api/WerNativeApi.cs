using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  WER NATIVE API  —  wer.dll  +  kernel32.dll (Application Recovery/Restart)
    //  Windows Error Reporting и восстановление приложений.
    //  Документация: https://learn.microsoft.com/windows/win32/wer/wer-reference
    // ===========================================================================

    public static class WerNativeApi
    {
        private const string WER      = "wer.dll";
        private const string KERNEL32 = "kernel32.dll";

        // --- WerAddExcludedApplicationBlock --------------------------------

        [DllImport(WER, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WerAddExcludedApplicationBlock(string pwzExeName);

        [DllImport(WER, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WerRemoveExcludedApplicationBlock(string pwzExeName);

        // --- WerReportCreate / WerReportSubmit -----------------------------

        public enum WER_REPORT_TYPE : uint
        {
            NonFatal = 1,
            Critical = 2,
            ApplicationHang = 3,
            ApplicationCrash = 4,
            Kernel = 5,
            Invalid = 6
        }

        [DllImport(WER, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WerReportCreate(
            string pwzEventType,
            WER_REPORT_TYPE repType,
            IntPtr pReportInformation,
            out IntPtr phReportHandle);

        [DllImport(WER, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WerReportSubmit(
            IntPtr hReportHandle,
            uint consent,
            uint dwFlags,
            IntPtr pSubmitResult);

        [DllImport(WER, SetLastError = true)]
        public static extern int WerReportCloseHandle(IntPtr hReportHandle);

        // --- WerGetConfiguration / WerSetConfiguration ---------------------

        public enum WER_CONSENT : uint
        {
            NotAsked = 1,
            Approved = 2,
            Denied = 3,
            AlwaysPrompt = 4
        }

        // --- Application Recovery / Restart (kernel32) ---------------------

        public delegate int RECOVERY_CALLBACK(IntPtr pvParameter);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECOVERY_CONTEXT
        {
            public uint dwPid;
            public uint dwFlags;
        }

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterApplicationRecoveryCallback(
            RECOVERY_CALLBACK pRecoveyCallback,
            IntPtr pvParameter,
            uint dwPingInterval,
            uint dwFlags);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterApplicationRecoveryCallback();

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterApplicationRestart(
            string pwzCommandline,
            uint dwFlags);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterApplicationRestart();

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ApplicationRecoveryInProgress(out bool pbCancelled);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern void ApplicationRecoveryFinished([MarshalAs(UnmanagedType.Bool)] bool bSuccess);

        // --- Расширенные настройки WER через реестр -----------------------

        /// <summary>
        /// Получает/устанавливает режим отчётов об ошибках Windows через
        /// реестр: HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting
        /// </summary>
        public static bool SetWerDisabled(bool disabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
                if (key == null) return false;

                key.SetValue("Disabled", disabled ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        public static bool IsWerDisabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
                var v = key?.GetValue("Disabled");
                return v != null && (int)v == 1;
            }
            catch { return false; }
        }

        // --- Высокоуровневые операции ------------------------------------

        /// <summary>Включает Windows Error Reporting (WER).</summary>
        public static bool EnableWer()
        {
            try
            {
                SetWerDisabled(false);

                // Удаляем Disabled-флаг в подразделе
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
                key?.DeleteValue("Disabled", false);
                key?.DeleteValue("DontShowUI", false);
                key?.DeleteValue("DontSendAdditionalData", false);

                return true;
            }
            catch { return false; }
        }

        /// <summary>Сбрасывает очередь отчётов об ошибках (очистка ReportQueue).</summary>
        public static void ClearWerQueue()
        {
            try
            {
                string queueDir = @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue";
                if (System.IO.Directory.Exists(queueDir))
                {
                    foreach (var d in System.IO.Directory.GetDirectories(queueDir))
                    {
                        try { System.IO.Directory.Delete(d, recursive: true); }
                        catch { /* может быть занят */ }
                    }
                    foreach (var f in System.IO.Directory.GetFiles(queueDir))
                    {
                        try { System.IO.File.Delete(f); } catch { }
                    }
                }

                string archiveDir = @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive";
                if (System.IO.Directory.Exists(archiveDir))
                {
                    foreach (var d in System.IO.Directory.GetDirectories(archiveDir))
                    {
                        try { System.IO.Directory.Delete(d, recursive: true); } catch { }
                    }
                }

                string tempDir = @"C:\ProgramData\Microsoft\Windows\WER\Temp";
                if (System.IO.Directory.Exists(tempDir))
                {
                    foreach (var d in System.IO.Directory.GetDirectories(tempDir))
                    {
                        try { System.IO.Directory.Delete(d, recursive: true); } catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>Регистрирует текущий процесс для автоматического перезапуска при сбое.</summary>
        public static bool EnableAutoRestart(string commandLine = "")
        {
            return RegisterApplicationRestart(commandLine, 0);
        }
    }
}
