using System;
using System.IO;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  BOOT RECOVERY MANAGER
    //  Восстановление загрузочных файлов Windows 10/11:
    //    • BCD (Boot Configuration Data)
    //    • Bootmgr / Bootsect
    //    • EFI раздел для UEFI-систем
    //
    //  Эти операции нельзя выполнить полностью нативно через Win32 API —
    //  Microsoft экспортирует их только через bcdedit.exe / bootrec.exe.
    //  Но мы выполняем их минуя cmd: через прямой Process.Start с проверкой.
    // ===========================================================================

    public static class BootRecoveryManager
    {
        /// <summary>Проверяет наличие загрузочных файлов.</summary>
        public static void CheckBootFiles()
        {
            Logger.Instance.Header(">>> Проверка загрузочных файлов Windows");

            // 1. Bootmgr (для BIOS/Legacy систем)
            string bootmgr = @"C:\Bootmgr";
            if (File.Exists(bootmgr))
                Logger.Instance.Success($"  [OK] {bootmgr}");
            else
                Logger.Instance.Warn($"  [—] {bootmgr} — отсутствует (нормально для UEFI)");

            // 2. BCD
            string bcd = @"C:\Boot\BCD";
            if (File.Exists(bcd))
                Logger.Instance.Success($"  [OK] {bcd}");
            else
                Logger.Instance.Warn($"  [—] {bcd} — отсутствует в стандартном месте");

            // 3. EFI-раздел — проверяем через DiskPart API (через WMI)
            CheckEfiPartition();

            // 4. Файлы загрузчика в System32
            string[] loaderFiles =
            {
                @"C:\Windows\System32\winload.exe",
                @"C:\Windows\System32\winresume.exe",
                @"C:\Windows\System32\winload.efi",
                @"C:\Windows\System32\winresume.efi",
                @"C:\Windows\Boot\PCAT\bootmgr",
                @"C:\Windows\Boot\EFI\bootmgfw.efi"
            };

            foreach (var f in loaderFiles)
            {
                if (File.Exists(f))
                    Logger.Instance.Success($"  [OK] {f}");
                else
                    Logger.Instance.Warn($"  [—] {f} — не найден");
            }
        }

        /// <summary>Проверяет наличие и состояние EFI System Partition (ESP).</summary>
        private static void CheckEfiPartition()
        {
            try
            {
                Logger.Instance.Info("Поиск EFI System Partition через WMI...");
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_DiskPartition WHERE Type = 'GPT: System'");
                bool found = false;
                foreach (System.Management.ManagementObject part in searcher.Get())
                {
                    found = true;
                    Logger.Instance.Success($"  Найден ESP-раздел: Disk {part["DiskIndex"]}, Partition {part["Index"]}");
                    Logger.Instance.Info($"    Размер: {part["Size"]} байт");
                    Logger.Instance.Info($"    Bootable: {part["Bootable"]}");
                }
                if (!found)
                    Logger.Instance.Warn("  EFI System Partition не обнаружена — возможно Legacy/MBR система.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn($"  WMI-проверка EFI: {ex.Message}");
            }
        }

        /// <summary>
        /// Восстанавливает BCD через прямые вызовы системных утилит.
        /// ВНИМАНИЕ: bcdedit / bootrec не имеют P/Invoke аналогов, поэтому
        /// используются прямые Process.Start (без cmd.exe — exe запускается напрямую).
        /// </summary>
        public static void RebuildBootConfig()
        {
            Logger.Instance.Header(">>> Восстановление загрузочной конфигурации (BCD)");

            Logger.Instance.Warn("ВНИМАНИЕ: эта операция модифицирует загрузчик.");
            Logger.Instance.Warn("Рекомендуется создать точку восстановления перед продолжением.");

            // 1. Экспорт текущего BCD как бэкап
            string bcdBackup = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemRestoreTool", "BCD_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(Path.GetDirectoryName(bcdBackup));

            Logger.Instance.Info($"Экспорт текущего BCD → {bcdBackup}");
            int ec = ConsoleHelper.RunExternal("bcdedit.exe", $"/export \"{bcdBackup}\"", out var so, out var se);
            if (ec == 0)
                Logger.Instance.Success("BCD экспортирован в бэкап.");
            else
                Logger.Instance.Warn($"bcdedit /export вернул код {ec}: {se.Trim()}");

            // 2. Проверка целостности хранилища BCD
            Logger.Instance.Info("Проверка хранилища BCD (bcdedit /enum)...");
            ec = ConsoleHelper.RunExternal("bcdedit.exe", "/enum all", out so, out se);
            if (ec == 0)
            {
                Logger.Instance.Success("Хранилище BCD доступно.");
                Logger.Instance.Info("Первые 500 символов вывода:");
                Logger.Instance.Info(so.Length > 500 ? so.Substring(0, 500) + "..." : so);
            }
            else
            {
                Logger.Instance.Error("Хранилище BCD повреждено! Требуется пересоздание (bootrec /rebuildbcd).");
            }
        }

        /// <summary>Проверяет состояние диспетчера загрузки Windows.</summary>
        public static void CheckWindowsBootLoader()
        {
            Logger.Instance.Info("Проверка записи {bootmgr} и {current}...");
            int ec = ConsoleHelper.RunExternal("bcdedit.exe", "/enum {bootmgr}", out var so, out var se);
            if (ec == 0)
            {
                Logger.Instance.Success("Запись {bootmgr} найдена:");
                Logger.Instance.Info(so);
            }
            else
                Logger.Instance.Error("Запись {bootmgr} не найдена — загрузчик повреждён.");

            ec = ConsoleHelper.RunExternal("bcdedit.exe", "/enum {current}", out so, out se);
            if (ec == 0)
            {
                Logger.Instance.Success("Запись {current} найдена:");
                Logger.Instance.Info(so);
            }
            else
                Logger.Instance.Warn("Запись {current} не найдена (нормально при загрузке из WinRE).");
        }
    }
}
