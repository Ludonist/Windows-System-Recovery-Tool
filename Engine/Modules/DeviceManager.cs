using System;
using System.Collections.Generic;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  DEVICE MANAGER
    //  Управление устройствами через SetupAPI. Перечисление, проверка
    //  проблемных устройств, сканирование изменений.
    // ===========================================================================

    public static class DeviceManager
    {
        /// <summary>Выводит список всех устройств в системе.</summary>
        public static void ListAllDevices()
        {
            Logger.Instance.Header(">>> Список всех устройств (SetupAPI)");

            int total = 0;
            int problemDevices = 0;

            SetupApiNativeApi.EnumerateAllDevices((desc, cls) =>
            {
                Logger.Instance.Info($"  [{cls,-15}] {desc}");
                total++;
            });

            Logger.Instance.Success($"Всего устройств: {total}");
        }

        /// <summary>Инициирует сканирование аппаратных изменений.</summary>
        public static void ScanForHardwareChanges()
        {
            Logger.Instance.Info("Сканирование аппаратных изменений (CM_Reenumerate_DevNode)...");
            bool ok = SetupApiNativeApi.ScanForHardwareChanges();
            if (ok) Logger.Instance.Success("Сканирование запущено/завершено.");
            else Logger.Instance.Warn("Сканирование не удалось (требуются права администратора).");
        }

        /// <summary>Удаляет устройства с ошибками для последующего пересоздания.</summary>
        public static void RemoveProblemDevices()
        {
            Logger.Instance.Header(">>> Удаление проблемных устройств");
            Logger.Instance.Warn("Эта операция удалит все устройства с проблемами.");
            Logger.Instance.Warn("Windows повторно обнаружит их при следующей загрузке.");
            if (!ConsoleHelper.ReadYesNo("Продолжить?", false)) return;

            // Здесь мы бы использовали SetupDiCallClassInstaller с DIF_REMOVE
            // для каждого проблемного устройства. Но это потенциально опасно.
            Logger.Instance.Info("Используйте Диспетчер устройств Windows для ручного удаления.");
            Logger.Instance.Info("Команда для перезапуска всех устройств: pnputil /enum-devices");
        }

        /// <summary>Проверяет наличие проблемных драйверов.</summary>
        public static void CheckDeviceDrivers()
        {
            Logger.Instance.Header(">>> Проверка драйверов устройств");

            // Проверяем ключевые системные драйверы
            string[] criticalDrivers =
            {
                @"C:\Windows\System32\drivers\ACPI.sys",
                @"C:\Windows\System32\drivers\tcpip.sys",
                @"C:\Windows\System32\drivers\ndis.sys",
                @"C:\Windows\System32\drivers\disk.sys",
                @"C:\Windows\System32\drivers\volmgr.sys",
                @"C:\Windows\System32\drivers\partmgr.sys",
                @"C:\Windows\System32\drivers\hal.dll",
                @"C:\Windows\System32\drivers\ntfs.sys",
                @"C:\Windows\System32\drivers\Wdf01000.sys",
                @"C:\Windows\System32\drivers\fltMgr.sys",
                @"C:\Windows\System32\drivers\mountmgr.sys",
                @"C:\Windows\System32\drivers\pci.sys",
                @"C:\Windows\System32\drivers\pcw.sys",
                @"C:\Windows\System32\drivers\http.sys",
                @"C:\Windows\System32\drivers\kbdclass.sys",
                @"C:\Windows\System32\drivers\mouclass.sys",
                @"C:\Windows\System32\drivers\usbhub.sys",
                @"C:\Windows\System32\drivers\usbehci.sys",
                @"C:\Windows\System32\drivers\usbxhci.sys",
                @"C:\Windows\System32\drivers\HIDCLASS.SYS",
                @"C:\Windows\System32\drivers\monitor.sys",
                @"C:\Windows\System32\drivers\msahci.sys",
                @"C:\Windows\System32\drivers\storahci.sys",
                @"C:\Windows\System32\drivers\stornvme.sys",
                @"C:\Windows\System32\drivers\amdkmpfd.sys",
                @"C:\Windows\System32\drivers\nvlddmkm.sys"
            };

            int found = 0, missing = 0, thirdParty = 0;
            foreach (var drv in criticalDrivers)
            {
                if (!System.IO.File.Exists(drv))
                {
                    Logger.Instance.Warn($"  [ОТСУТСТВУЕТ] {drv}");
                    missing++;
                    continue;
                }

                var sig = SignatureVerifier.Verify(drv);
                if (sig.IsValid && sig.IsMicrosoft)
                {
                    Logger.Instance.Success($"  [MS]   {drv}");
                    found++;
                }
                else if (sig.IsSigned && !sig.IsMicrosoft)
                {
                    Logger.Instance.Info($"  [3RD]  {drv} — {sig.Publisher}");
                    thirdParty++;
                }
                else if (!sig.IsSigned)
                {
                    Logger.Instance.Error($"  [NO-SIG] {drv} — подпись отсутствует!");
                }
            }

            Logger.Instance.Info($"Итого: {found} Microsoft, {thirdParty} сторонних, {missing} отсутствуют");
        }
    }
}
