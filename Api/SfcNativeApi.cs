using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  SFC NATIVE API  —  sfc.dll  +  sfc_os.dll  +  srclient.dll
    //  Прямые P/Invoke вызовы для проверки защиты файлов и запуска SFC-скана.
    //
    //  Sfc /ScanNow фактически вызывает внутреннюю функцию sfc_os.dll!
    //  SfcSynchronousScan — недокументированная, но стабильно работающая с Win8+.
    // ===========================================================================

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProtectedFileData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;

        public int FileNumber;
    }

    /// <summary>Тип SFC-сканирования (для sfc_os.dll!SfcSynchronousScan).</summary>
    public enum SfcScanType : uint
    {
        /// <summary>Только проверка, без восстановления (аналог /verifyonly).</summary>
        VerifyOnly = 0,
        /// <summary>Проверка и восстановление (аналог /scannow).</summary>
        ScanAndRepair = 1,
        /// <summary>Проверка при следующей загрузке (аналог /scanboot).</summary>
        ScanAtBoot = 2
    }

    public static class SfcNativeApi
    {
        private const string SFC_DLL     = "sfc.dll";
        private const string SFC_OS_DLL  = "sfc_os.dll";
        private const string SRCLIENT    = "srclient.dll";

        // --- sfc.dll: проверка конкретного файла --------------------------

        /// <summary>
        /// Проверяет, защищён ли файл механизмом WFP (Windows File Protection).
        /// </summary>
        [DllImport(SFC_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcIsFileProtected(
            IntPtr rpcHandle,
            string protFileName);

        // --- sfc.dll: перечисление всех защищённых файлов -----------------

        /// <summary>
        /// Итератор по списку защищённых файлов. Handle = IntPtr.Zero для старта.
        /// </summary>
        [DllImport(SFC_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcGetNextProtectedFile(
            IntPtr handle,
            ref ProtectedFileData protFileData);

        // --- sfc_os.dll: добавление файла в исключения WFP ----------------

        /// <summary>
        /// Временно отключает защиту для файла (применяется установщиками).
        /// НЕ ИСПОЛЬЗОВАТЬ в продуктиве без необходимости!
        /// </summary>
        [DllImport(SFC_OS_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcFileException(
            IntPtr rpcHandle,
            string fileName,
            int reserved);

        // --- sfc_os.dll: ЗАПУСК SFC-СКАНИРОВАНИЯ --------------------------
        //  Это внутренний вызов, который делает sfc.exe /scannow.
        //  Недокументировано, но стабильно присутствует в Windows 7–11.
        // -------------------------------------------------------------------

        /// <summary>
        /// Запускает синхронное SFC-сканирование (блокирует поток до завершения).
        /// Эквивалент sfc.exe /scannow, но без запуска внешнего процесса.
        /// </summary>
        [DllImport(SFC_OS_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcSynchronousScan(
            IntPtr hWnd,
            SfcScanType scanType,
            IntPtr reserved);

        /// <summary>Альтернативное имя — присутствует в старых сборках Win10.</summary>
        [DllImport(SFC_OS_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcStartScan(
            IntPtr hWnd,
            SfcScanType scanType,
            IntPtr reserved);

        // --- srclient.dll: восстановление конкретного файла ---------------

        /// <summary>
        /// Запрашивает у WFP немедленное восстановление указанного файла.
        /// Полезно после ручного удаления/повреждения системного файла.
        /// </summary>
        [DllImport(SRCLIENT, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SfcRestoreFile(
            IntPtr hWnd,
            string fileName,
            int flags);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники
        // -----------------------------------------------------------------

        /// <summary>Запускает SFC-скан (Sfc /ScanNow) через нативный API.</summary>
        public static bool RunSfcScan(SfcScanType type = SfcScanType.ScanAndRepair)
        {
            // Пробуем SfcSynchronousScan (Win10/11), если не вышло — SfcStartScan.
            try
            {
                if (SfcSynchronousScan(IntPtr.Zero, type, IntPtr.Zero))
                    return true;
            }
            catch { /* функция может отсутствовать */ }

            try
            {
                return SfcStartScan(IntPtr.Zero, type, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Проверяет, защищён ли указанный файл механизмом WFP.</summary>
        public static bool IsFileProtected(string filePath)
        {
            try
            {
                return SfcIsFileProtected(IntPtr.Zero, filePath);
            }
            catch
            {
                return false;
            }
        }
    }
}
