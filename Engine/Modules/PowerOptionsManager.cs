using System;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  POWER OPTIONS MANAGER
    //  Управление питанием через powrprof.dll. Просмотр/смена схем питания,
    //  управление гибернацией, проверка состояния батареи.
    // ===========================================================================

    public static class PowerOptionsManager
    {
        /// <summary>Выводит информацию о питании системы.</summary>
        public static void ShowPowerInfo()
        {
            Logger.Instance.Header(">>> Информация о питании (powrprof.dll)");

            // 1. Активная схема
            var active = PowerNativeApi.GetActiveSchemeGuid();
            if (active.HasValue)
            {
                string name = active.Value switch
                {
                    var g when g == PowerNativeApi.GUID_MAX_POWER_SAVINGS => "Энергосбережение",
                    var g when g == PowerNativeApi.GUID_TYPICAL_POWER_SAVINGS => "Сбалансированная",
                    var g when g == PowerNativeApi.GUID_MIN_POWER_SAVINGS => "Высокая производительность",
                    _ => $"Пользовательская: {active.Value}"
                };
                Logger.Instance.Success($"Активная схема: {name}");
                Logger.Instance.Info($"GUID: {active.Value}");
            }
            else
            {
                Logger.Instance.Warn("Не удалось получить активную схему питания.");
            }

            // 2. Возможности питания
            if (PowerNativeApi.GetPwrCapabilities(out var caps))
            {
                Logger.Instance.Info("");
                Logger.Instance.Info("=== Возможности питания ===");
                Logger.Instance.Info($"  Кнопка питания:       {(caps.PowerButtonPresent != 0 ? "есть" : "нет")}");
                Logger.Instance.Info($"  Кнопка сна:           {(caps.SleepButtonPresent != 0 ? "есть" : "нет")}");
                Logger.Instance.Info($"  Крышка ноутбука:      {(caps.LidPresent != 0 ? "есть" : "нет")}");
                Logger.Instance.Info($"  S1 (Sleep):           {(caps.SystemS1 != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  S2 (Sleep):           {(caps.SystemS2 != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  S3 (Sleep):           {(caps.SystemS3 != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  S4 (Hibernate):       {(caps.SystemS4 != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  S5 (Soft Off):        {(caps.SystemS5 != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  Файл гибернации:      {(caps.HiberFilePresent != 0 ? "есть" : "нет")}");
                Logger.Instance.Info($"  Fast Startup:         {(caps.Hiberboot != 0 ? "включён" : "выключен")}");
                Logger.Instance.Info($"  Термоконтроль:        {(caps.ThermalControl != 0 ? "есть" : "нет")}");
            }

            // 3. Состояние батареи
            var battery = PowerNativeApi.GetBatteryState();
            if (battery.HasValue && battery.Value.BatteryPresent != 0)
            {
                Logger.Instance.Info("");
                Logger.Instance.Info("=== Батарея ===");
                Logger.Instance.Info($"  От сети:              {(battery.Value.AcOnLine != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  Заряжается:           {(battery.Value.Charging != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  Разряжается:          {(battery.Value.Discharging != 0 ? "да" : "нет")}");
                Logger.Instance.Info($"  Макс. ёмкость:        {battery.Value.MaxCapacity} mWh");
                Logger.Instance.Info($"  Текущая ёмкость:      {battery.Value.RemainingCapacity} mWh");
                double pct = battery.Value.MaxCapacity > 0
                    ? 100.0 * battery.Value.RemainingCapacity / battery.Value.MaxCapacity
                    : 0;
                Logger.Instance.Info($"  Уровень заряда:       {pct:F1}%");
                Logger.Instance.Info($"  Скорость разряда:     {battery.Value.Rate} mW");
                Logger.Instance.Info($"  Оставшееся время:     {battery.Value.EstimatedTime} сек");
            }
            else
            {
                Logger.Instance.Info("");
                Logger.Instance.Info("Батарея не обнаружена (стационарный ПК).");
            }
        }

        /// <summary>Включает схему "Высокая производительность".</summary>
        public static void SetHighPerformance()
        {
            if (PowerNativeApi.SetHighPerformance())
                Logger.Instance.Success("Схема питания: Высокая производительность.");
            else
                Logger.Instance.Error("Не удалось сменить схему.");
        }

        /// <summary>Включает схему "Сбалансированная".</summary>
        public static void SetBalanced()
        {
            if (PowerNativeApi.SetBalanced())
                Logger.Instance.Success("Схема питания: Сбалансированная.");
            else
                Logger.Instance.Error("Не удалось сменить схему.");
        }

        /// <summary>Включает схему "Энергосбережение".</summary>
        public static void SetPowerSaver()
        {
            if (PowerNativeApi.SetPowerSaver())
                Logger.Instance.Success("Схема питания: Энергосбережение.");
            else
                Logger.Instance.Error("Не удалось сменить схему.");
        }

        /// <summary>Проверяет и при необходимости восстанавливает схему питания по умолчанию.</summary>
        public static void RestoreDefaultPowerSchemes()
        {
            Logger.Instance.Header(">>> Восстановление схем питания по умолчанию");
            Logger.Instance.Info("Запуск: powercfg -restoredefaultschemes");
            int ec = ConsoleHelper.RunExternal("powercfg.exe", "-restoredefaultschemes", out var so, out var se);
            if (ec == 0)
                Logger.Instance.Success("Схемы питания восстановлены по умолчанию.");
            else
                Logger.Instance.Error($"powercfg вернул код {ec}: {se}");
        }

        /// <summary>Включает/выключает файл гибернации.</summary>
        public static void ToggleHibernate(bool enable)
        {
            Logger.Instance.Header($">>> {(enable ? "Включение" : "Отключение")} гибернации");
            string arg = enable ? "/hibernate on" : "/hibernate off";
            int ec = ConsoleHelper.RunExternal("powercfg.exe", arg, out _, out var se);
            if (ec == 0)
                Logger.Instance.Success($"Гибернация {(enable ? "включена" : "отключена")}.");
            else
                Logger.Instance.Error($"powercfg вернул код {ec}: {se}");
        }
    }
}
