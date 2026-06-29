using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  MINIDUMP MANAGER
    //  Анализ процессов через MiniDump:
    //    • Создание dump-файлов процессов (mini/full)
    //    • Анализ заголовка dump-файла
    //    • Извлечение базовой информации (PID, потоки, модули)
    //    • Поддержка всех типов dump (Mini, Full, WithFullMemory, etc.)
    //  Эквивалент: Task Manager → Create dump file / procdump.exe
    // ===========================================================================

    public static class MiniDumpManager
    {
        // --- dbghelp.dll!MiniDumpWriteDump --------------------------------

        [Flags]
        public enum MINIDUMP_TYPE : uint
        {
            MiniDumpNormal                          = 0x00000000,
            MiniDumpWithDataSegs                    = 0x00000001,
            MiniDumpWithFullMemory                  = 0x00000002,
            MiniDumpWithHandleData                  = 0x00000004,
            MiniDumpFilterMemory                    = 0x00000008,
            MiniDumpScanMemory                      = 0x00000010,
            MiniDumpWithUnloadedModules             = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory  = 0x00000040,
            MiniDumpFilterModulePaths               = 0x00000080,
            MiniDumpWithProcessThreadData           = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory      = 0x00000200,
            MiniDumpWithoutOptionalData             = 0x00000400,
            MiniDumpWithFullMemoryInfo              = 0x00000800,
            MiniDumpWithThreadInfo                  = 0x00001000,
            MiniDumpWithCodeSegs                    = 0x00002000,
            MiniDumpWithoutAuxiliaryState           = 0x00004000,
            MiniDumpWithFullAuxiliaryState          = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory      = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory        = 0x00020000,
            MiniDumpWithTokenInformation            = 0x00040000,
            MiniDumpWithModuleHeaders               = 0x00080000,
            MiniDumpFilterTriage                    = 0x00100000,
            MiniDumpWithAvxXStateContext            = 0x00200000,
            MiniDumpWithIptTrace                    = 0x00400000,
            MiniDumpScanInaccessiblePartialPages    = 0x00800000
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MINIDUMP_EXCEPTION_INFORMATION
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)]
            public bool ClientPointers;
        }

        [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallbackParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct MINIDUMP_DIRECTORY
        {
            public uint StreamType;
            public uint LocationOfStream;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINIDUMP_LOCATION_DESCRIPTOR
        {
            public uint DataSize;
            public uint Rva;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINIDUMP_STREAM_DESCRIPTOR
        {
            public uint StreamType;
            public MINIDUMP_LOCATION_DESCRIPTOR Location;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINIDUMP_HEADER
        {
            public uint Signature;          // 'MDMP' = 0x504D444D
            public uint Version;            // 0xA793
            public uint NumberOfStreams;
            public uint StreamDirectoryRva;
            public uint CheckSum;
            public uint TimeDateStamp;
            public ulong Flags;
        }

        [DllImport("dbghelp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MiniDumpReadDumpStream(
            IntPtr BaseOfDump,
            uint StreamNumber,
            out IntPtr Dir,
            out IntPtr StreamPointer,
            out uint StreamSize);

        public const uint ThreadListStream          = 3;
        public const uint ModuleListStream          = 4;
        public const uint MemoryListStream          = 5;
        public const uint ExceptionStream           = 6;
        public const uint SystemInfoStream          = 7;
        public const uint ThreadExListStream        = 8;
        public const uint Memory64ListStream        = 9;
        public const uint CommentStreamA            = 10;
        public const uint CommentStreamW            = 11;
        public const uint HandleDataStream          = 12;
        public const uint FunctionTableStream       = 13;
        public const uint UnloadedModuleListStream  = 14;
        public const uint MiscInfoStream            = 15;
        public const uint MemoryInfoListStream      = 16;
        public const uint ThreadInfoListStream      = 17;
        public const uint HandleOperationListStream = 18;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileW(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ           = 0x0010;
        public const uint GENERIC_WRITE             = 0x40000000;
        public const uint GENERIC_READ              = 0x80000000;
        public const uint CREATE_ALWAYS             = 2;
        public const uint FILE_SHARE_READ           = 1;

        // -----------------------------------------------------------------
        //  Высокоуровневые операции
        // -----------------------------------------------------------------

        /// <summary>Создаёт minidump указанного процесса.</summary>
        public static bool CreateDump(uint processId, string outputPath,
            MINIDUMP_TYPE dumpType = MINIDUMP_TYPE.MiniDumpNormal)
        {
            Logger.Instance.Info($"Создание dump для PID {processId} → {outputPath}");
            Logger.Instance.Info($"Тип дампа: {dumpType}");

            IntPtr hProcess = OpenProcess(
                PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                Logger.Instance.Error($"OpenProcess failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            IntPtr hFile = CreateFileW(outputPath,
                GENERIC_WRITE, FILE_SHARE_READ, IntPtr.Zero,
                CREATE_ALWAYS, 0, IntPtr.Zero);
            if (hFile == new IntPtr(-1))
            {
                Logger.Instance.Error($"CreateFile failed: {Marshal.GetLastWin32Error()}");
                CloseHandle(hProcess);
                return false;
            }

            try
            {
                bool ok = MiniDumpWriteDump(hProcess, processId, hFile, dumpType,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (!ok)
                {
                    Logger.Instance.Error($"MiniDumpWriteDump failed: {Marshal.GetLastWin32Error()}");
                    return false;
                }
                Logger.Instance.Success($"Dump создан: {outputPath}");
                return true;
            }
            finally
            {
                CloseHandle(hFile);
                CloseHandle(hProcess);
            }
        }

        /// <summary>Создаёт minidump для всех запущенных процессов.</summary>
        public static void CreateDumpForAllProcesses(string outputDir,
            MINIDUMP_TYPE dumpType = MINIDUMP_TYPE.MiniDumpNormal)
        {
            Logger.Instance.Header($">>> Создание minidumps для всех процессов → {outputDir}");
            Directory.CreateDirectory(outputDir);

            var procs = Process.GetProcesses();
            int success = 0, failed = 0;

            foreach (var p in procs)
            {
                if (p.Id == 0 || p.Id == 4) continue; // System, Idle
                string file = Path.Combine(outputDir, $"{p.Id}_{SafeName(p.ProcessName)}.dmp");
                Logger.Instance.Info($"  → PID {p.Id} {p.ProcessName}");
                try
                {
                    if (CreateDump((uint)p.Id, file, dumpType))
                        success++;
                    else
                        failed++;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn($"    {ex.Message}");
                    failed++;
                }
            }

            Logger.Instance.Info($"Итого: успешно {success}, неудач {failed} из {procs.Length}");
        }

        /// <summary>Интерактивный выбор процесса и создание дампа.</summary>
        public static void CreateDumpInteractive()
        {
            Logger.Instance.Header(">>> Создание minidump для выбранного процесса");

            // Список процессов
            var procs = Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray();
            Logger.Instance.Info($"Всего процессов: {procs.Length}");
            Logger.Instance.Raw("");
            Logger.Instance.Raw($"  {"#",-5} {"PID",-8} {"Name",-30} {"Memory",-15}");
            Logger.Instance.Raw($"  {new string('-', 70)}");

            for (int i = 0; i < procs.Length; i++)
            {
                var p = procs[i];
                long mem = 0;
                try { mem = p.WorkingSet64; } catch { }
                Logger.Instance.Raw($"  {i + 1,-5} {p.Id,-8} {Trunc(p.ProcessName, 30),-30} {mem / 1024 / 1024,10} MB");
            }

            Logger.Instance.Raw("");
            int choice = ConsoleHelper.ReadInt("Введите номер процесса: ", 1, procs.Length, 1);
            var selected = procs[choice - 1];

            Logger.Instance.Raw("");
            Logger.Instance.Raw("Тип дампа:");
            Logger.Instance.Raw("  1) MiniDumpNormal (минимальный, ~KB)");
            Logger.Instance.Raw("  2) MiniDumpWithDataSegs (с данными, ~MB)");
            Logger.Instance.Raw("  3) MiniDumpWithFullMemory (полный, ~GB!)");
            Logger.Instance.Raw("  4) MiniDumpWithHandleData (с хендлами)");
            Logger.Instance.Raw("  5) MiniDumpWithThreadInfo (с инфо о потоках)");
            Logger.Instance.Raw("  6) MiniDumpWithUnloadedModules (с выгруженными DLL)");
            int typeChoice = ConsoleHelper.ReadInt("Тип (1-6): ", 1, 6, 1);

            var dumpType = typeChoice switch
            {
                1 => MINIDUMP_TYPE.MiniDumpNormal,
                2 => MINIDUMP_TYPE.MiniDumpWithDataSegs,
                3 => MINIDUMP_TYPE.MiniDumpWithFullMemory,
                4 => MINIDUMP_TYPE.MiniDumpWithHandleData,
                5 => MINIDUMP_TYPE.MiniDumpWithThreadInfo,
                6 => MINIDUMP_TYPE.MiniDumpWithUnloadedModules,
                _ => MINIDUMP_TYPE.MiniDumpNormal
            };

            string outFile = Path.Combine(
                Path.GetDirectoryName(Logger.Instance.LogFilePath),
                $"dump_{selected.Id}_{SafeName(selected.ProcessName)}_{DateTime.Now:yyyyMMdd_HHmmss}.dmp");

            CreateDump((uint)selected.Id, outFile, dumpType);

            // Анализируем созданный дамп
            Logger.Instance.Raw("");
            AnalyzeDumpFile(outFile);
        }

        /// <summary>Анализ заголовка dump-файла.</summary>
        public static void AnalyzeDumpFile(string dumpPath)
        {
            Logger.Instance.Header($">>> Анализ dump-файла: {dumpPath}");

            if (!File.Exists(dumpPath))
            {
                Logger.Instance.Error("Файл не найден");
                return;
            }

            var info = new FileInfo(dumpPath);
            Logger.Instance.Info($"Размер: {info.Length / 1024.0 / 1024:F2} MB");
            Logger.Instance.Info($"Создан: {info.CreationTime}");

            try
            {
                using var fs = File.OpenRead(dumpPath);
                using var br = new BinaryReader(fs);

                // MINIDUMP_HEADER (32 bytes)
                uint signature = br.ReadUInt32();
                if (signature != 0x504D444D) // 'MDMP'
                {
                    Logger.Instance.Error($"Неверная сигнатура: 0x{signature:X8} (ожидался 'MDMP')");
                    return;
                }

                uint version = br.ReadUInt32();
                uint numStreams = br.ReadUInt32();
                uint streamDirRva = br.ReadUInt32();
                uint checkSum = br.ReadUInt32();
                uint timestamp = br.ReadUInt32();
                ulong flags = br.ReadUInt64();

                Logger.Instance.Success($"Сигнатура: MDMP (валидный)");
                Logger.Instance.Info($"Version: 0x{version:X8}");
                Logger.Instance.Info($"Streams: {numStreams}");
                Logger.Instance.Info($"Stream directory RVA: 0x{streamDirRva:X8}");
                Logger.Instance.Info($"Checksum: 0x{checkSum:X8}");
                Logger.Instance.Info($"Timestamp: {DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime:yyyy-MM-dd HH:mm:ss}");
                Logger.Instance.Info($"Flags: 0x{flags:X16}");

                // Читаем поток SystemInfo
                fs.Seek(streamDirRva, SeekOrigin.Begin);
                var streams = new List<(uint Type, uint Size, uint Rva)>();
                for (int i = 0; i < numStreams; i++)
                {
                    uint type = br.ReadUInt32();
                    uint dataSize = br.ReadUInt32();
                    uint rva = br.ReadUInt32();
                    streams.Add((type, dataSize, rva));
                }

                Logger.Instance.Raw("");
                Logger.Instance.Info($"Потоки в дампе ({streams.Count}):");
                foreach (var (type, size, rva) in streams)
                {
                    string name = type switch
                    {
                        ThreadListStream          => "ThreadList",
                        ModuleListStream          => "ModuleList",
                        MemoryListStream          => "MemoryList",
                        ExceptionStream           => "Exception",
                        SystemInfoStream          => "SystemInfo",
                        ThreadExListStream        => "ThreadExList",
                        Memory64ListStream        => "Memory64List",
                        CommentStreamA            => "CommentA",
                        CommentStreamW            => "CommentW",
                        HandleDataStream          => "HandleData",
                        FunctionTableStream       => "FunctionTable",
                        UnloadedModuleListStream  => "UnloadedModuleList",
                        MiscInfoStream            => "MiscInfo",
                        MemoryInfoListStream      => "MemoryInfoList",
                        ThreadInfoListStream      => "ThreadInfoList",
                        HandleOperationListStream => "HandleOperationList",
                        _ => $"Unknown({type})"
                    };
                    Logger.Instance.Raw($"  • {name,-25} size={size,12}  rva=0x{rva:X8}");
                }

                // Извлекаем SystemInfo
                var sysInfo = streams.FirstOrDefault(s => s.Type == SystemInfoStream);
                if (sysInfo.Size > 0)
                {
                    Logger.Instance.Raw("");
                    Logger.Instance.Info("--- SystemInfo ---");
                    fs.Seek(sysInfo.Rva, SeekOrigin.Begin);

                    // MINIDUMP_SYSTEM_INFO
                    ushort processorArch = br.ReadUInt16();
                    ushort processorLevel = br.ReadUInt16();
                    ushort processorRev = br.ReadUInt16();
                    byte numProcessors = br.ReadByte();
                    byte productType = br.ReadByte();
                    uint majorVer = br.ReadUInt32();
                    uint minorVer = br.ReadUInt32();
                    uint buildNo = br.ReadUInt32();
                    uint platId = br.ReadUInt32();

                    string arch = processorArch switch
                    {
                        0 => "x86",
                        5 => "ARM",
                        6 => "x64 (Intel/AMD)",
                        12 => "ARM64",
                        _ => $"Unknown({processorArch})"
                    };
                    string pt = productType switch
                    {
                        1 => "Workstation",
                        2 => "Domain Controller",
                        3 => "Server",
                        _ => $"Unknown({productType})"
                    };
                    Logger.Instance.Info($"  Architecture: {arch}");
                    Logger.Instance.Info($"  Processor level: {processorLevel}, rev: 0x{processorRev:X}");
                    Logger.Instance.Info($"  Number of processors: {numProcessors}");
                    Logger.Instance.Info($"  Product type: {pt}");
                    Logger.Instance.Info($"  OS version: {majorVer}.{minorVer}.{buildNo}");
                }

                // Извлекаем ModuleList
                var moduleStream = streams.FirstOrDefault(s => s.Type == ModuleListStream);
                if (moduleStream.Size > 0)
                {
                    Logger.Instance.Raw("");
                    Logger.Instance.Info("--- Modules ---");
                    fs.Seek(moduleStream.Rva, SeekOrigin.Begin);
                    uint numModules = br.ReadUInt32();
                    Logger.Instance.Info($"Загружено модулей: {numModules}");

                    int shown = 0;
                    for (int i = 0; i < Math.Min(numModules, 20); i++)
                    {
                        // MINIDUMP_MODULE: BaseOfImage, SizeOfImage, CheckSum, TimeDateStamp, ModuleNameRva, VersionInfo, Location
                        ulong baseOfImage = br.ReadUInt64();
                        uint sizeOfImage = br.ReadUInt32();
                        uint checkSumMod = br.ReadUInt32();
                        uint timeDate = br.ReadUInt32();
                        uint moduleNameRva = br.ReadUInt32();
                        // VS_FIXEDFILEINFO (52 bytes)
                        br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
                        br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
                        br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
                        br.ReadUInt32();
                        // MINIDUMP_LOCATION_DESCRIPTOR (8 bytes)
                        uint dataSize = br.ReadUInt32();
                        uint rva = br.ReadUInt32();

                        // Читаем имя модуля (MINIDUMP_STRING: uint length + UTF-16 string)
                        long pos = fs.Position;
                        fs.Seek(moduleNameRva, SeekOrigin.Begin);
                        uint nameLen = br.ReadUInt32();
                        var nameBytes = br.ReadBytes((int)nameLen);
                        string name = System.Text.Encoding.Unicode.GetString(nameBytes);
                        fs.Seek(pos, SeekOrigin.Begin);

                        Logger.Instance.Raw($"  {Trunc(Path.GetFileName(name), 35),-35} base=0x{baseOfImage:X12} size={sizeOfImage / 1024,8}KB");
                        shown++;
                    }
                    if (numModules > 20)
                        Logger.Instance.Raw($"  ... и ещё {numModules - 20} модулей");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка анализа: {ex.Message}");
            }
        }

        /// <summary>Создаёт дамп для всех процессов и сохраняет в указанную папку.</summary>
        public static void DumpAllProcesses()
        {
            string dir = Path.Combine(
                Path.GetDirectoryName(Logger.Instance.LogFilePath),
                $"ProcessDumps_{DateTime.Now:yyyyMMdd_HHmmss}");
            CreateDumpForAllProcesses(dir, MINIDUMP_TYPE.MiniDumpNormal);
        }

        private static string SafeName(string s)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (var c in s)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString();
        }

        private static string Trunc(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max - 3) + "...");
    }
}
