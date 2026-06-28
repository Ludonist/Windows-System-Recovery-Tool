using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  DISM NATIVE API  —  dismapi.dll
    //  Прямые P/Invoke вызовы. Содержит ВСЕ функции, нужные для CheckHealth,
    //  ScanHealth, RestoreHealth, StartComponentCleanup, GetImageInfo и т.д.
    //
    //  Документация: https://learn.microsoft.com/windows-hardware/manufacture/desktop/dism/dism-api-reference
    // ===========================================================================

    /// <summary>Уровень детализации лога DISM.</summary>
    public enum DismLogLevel : uint
    {
        Errors = 0,
        Warnings = 1,
        Verbose = 2,
        Debug = 3
    }

    /// <summary>Состояние здоровья образа после DismCheckImageHealth.</summary>
    public enum DismImageHealthState : uint
    {
        Healthy = 0,
        Repairable = 1,
        NonRepairable = 2
    }

    /// <summary>Тип образа (из DismGetImageInfo).</summary>
    public enum DismImageType : uint
    {
        Unsupported = 0,
        Wim = 1,
        Vhd = 2,
        Vhdx = 3
    }

    /// <summary>Архитектура образа.</summary>
    public enum DismImageArchitecture : uint
    {
        X86 = 0,
        X64 = 6,
        Arm = 5,
        Arm64 = 12
    }

    /// <summary>Делегат для прогресс-колбэка DISM (RestoreImageHealth, etc.).</summary>
    public delegate void DismProgressCallback(uint current, uint total, IntPtr userData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DismImageInfo
    {
        public uint Size;
        public DismImageType ImageType;
        public DismImageArchitecture ImageArchitecture;
        public string ImageName;
        public string ImageDescription;
        public ulong InstallationType;
        public string Hal;
        public string ProductType;
        public uint Edition;
        public uint InstallationId;
        public ulong DefaultLanguageId;
        public uint ProductVersion;
        public uint ProductInfo;
        public uint BootId;
        public uint Flags;
        public IntPtr CustomizedInfo;
    }

    /// <summary>
    /// Полный набор P/Invoke для dismapi.dll с корректной обработкой HRESULT
    /// (PreserveSig=false → COMException с реальным HRESULT).
    /// </summary>
    public static class DismNativeApi
    {
        private const string DLL = "dismapi.dll";

        // --- Инициализация / завершение -----------------------------------

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismInitialize(
            DismLogLevel logLevel,
            string logFilePath,
            string scratchDirectory);

        [DllImport(DLL, PreserveSig = false)]
        public static extern void DismShutdown();

        // --- Сессии -------------------------------------------------------

        /// <summary>
        /// Открытие сессии. Для Online (текущая система) все 3 параметра = null.
        /// Возвращает валидный IntPtr или выбрасывает COMException.
        /// </summary>
        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern IntPtr DismOpenSession(
            string imagePath,
            string windowsDirectory,
            string systemDrive);

        [DllImport(DLL, PreserveSig = false)]
        public static extern void DismCloseSession(IntPtr session);

        // --- Здоровье образа: CheckHealth / ScanHealth / RestoreHealth ----

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismCheckImageHealth(
            IntPtr session,
            string sourcePath,
            [MarshalAs(UnmanagedType.Bool)] bool limitAccess,
            out DismImageHealthState state,
            out IntPtr cancelEvent);

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismScanImageHealth(
            IntPtr session,
            string sourcePath,
            [MarshalAs(UnmanagedType.Bool)] bool limitAccess,
            out IntPtr cancelEvent);

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismRestoreImageHealth(
            IntPtr session,
            string sourcePath,
            [MarshalAs(UnmanagedType.Bool)] bool limitAccess,
            out IntPtr cancelEvent,
            DismProgressCallback progress,
            IntPtr userData);

        // --- Очистка хранилища компонентов (StartComponentCleanup) --------

        /// <summary>
        /// Запускает очистку хранилища компонентов.
        /// Если resetBase == true — удаляет все заменённые версии компонентов (аналог /ResetBase).
        /// </summary>
        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismStartComponentCleanup(
            IntPtr session,
            [MarshalAs(UnmanagedType.Bool)] bool resetBase,
            out IntPtr cancelEvent,
            DismProgressCallback progress,
            IntPtr userData);

        // --- Анализ хранилища компонентов (AnalyzeComponentStore) ---------

        [StructLayout(LayoutKind.Sequential)]
        public struct DismComponentStoreInfo
        {
            public uint Size;
            public ulong ActualSize;
            public ulong Reserved;
            public ulong SystemCenteredSizeMB;
            public uint SharedWithWindows;
            public ulong BackedUp;
            public ulong Disabled;
            public ulong etc;
        }

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismAnalyzeComponentStore(
            IntPtr session,
            out IntPtr info);

        // --- Информация об образе -----------------------------------------

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismGetImageInfo(
            string imagePath,
            out IntPtr imageInfo,
            out uint count);

        // --- Пакеты / функции (для диагностики) ---------------------------

        [DllImport(DLL, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DismGetPackages(
            IntPtr session,
            out IntPtr packageInfo,
            out uint count);

        [DllImport(DLL, PreserveSig = false)]
        public static extern void DismDelete(IntPtr dismStructure);

        // -----------------------------------------------------------------
        //  Высокоуровневые помощники, оборачивающие вызовы в try/catch
        //  и предоставляющие понятный интерфейс для Engine.
        // -----------------------------------------------------------------

        /// <summary>Полный цикл CheckHealth. Возвращает строковый статус.</summary>
        public static string RunCheckHealth(IntPtr session)
        {
            DismCheckImageHealth(session, null, false, out var state, out _);
            return state switch
            {
                DismImageHealthState.Healthy => "Healthy — образ здоров",
                DismImageHealthState.Repairable => "Repairable — обнаружены повреждения, восстановимо",
                DismImageHealthState.NonRepairable => "NonRepairable — критические повреждения",
                _ => $"Unknown ({state})"
            };
        }

        /// <summary>Полный цикл ScanHealth.</summary>
        public static void RunScanHealth(IntPtr session)
            => DismScanImageHealth(session, null, false, out _);

        /// <summary>Полный цикл RestoreHealth с прогресс-колбэком.</summary>
        public static void RunRestoreHealth(IntPtr session, DismProgressCallback cb)
            => DismRestoreImageHealth(session, null, false, out _, cb, IntPtr.Zero);

        /// <summary>Полный цикл StartComponentCleanup.</summary>
        public static void RunComponentCleanup(IntPtr session, bool resetBase, DismProgressCallback cb)
            => DismStartComponentCleanup(session, resetBase, out _, cb, IntPtr.Zero);
    }
}
