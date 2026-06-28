using System;
using System.Collections.Generic;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  SERVICES REPAIR MANAGER
    //  Восстановление ключевых служб Windows через Service Control Manager
    //  (advapi32.dll). Перезапуск критических служб после восстановления.
    // ===========================================================================

    public static class ServicesRepairManager
    {
        /// <summary>
        /// Критические службы Windows, которые нужно перезапустить после
        /// восстановления системных файлов.
        /// </summary>
        public static readonly string[] CriticalServices =
        {
            "EventLog",            // Windows Event Log
            "Schedule",            // Task Scheduler
            "Spooler",             // Print Spooler
            "Winmgmt",             // Windows Management Instrumentation
            "wuauserv",            // Windows Update
            "BITS",                // Background Intelligent Transfer Service
            "CryptSvc",            // Cryptographic Services
            "MSIServer",           // Windows Installer
            "TrustedInstaller",    // Windows Modules Installer
            "PlugPlay",            // Plug and Play
            "RpcSs",               // Remote Procedure Call (RPC)
            "RpcEptMapper",        // RPC Endpoint Mapper
            "DcomLaunch",          // DCOM Server Process Launcher
            "SystemRestoreService",// System Restore Service
            "VSS",                 // Volume Shadow Copy
            "swprv",               // Microsoft Software Shadow Copy provider
            "Defragsvc",           // Optimize drives
            "WinDefend",           // Windows Defender Antivirus Service
            "Sense",               // Windows Defender Advanced Threat Protection
            "WdNisSvc",            // Windows Defender Network Inspection Service
            "mpssvc",              // Windows Firewall
            "BFE",                 // Base Filtering Engine
            "Dnscache",            // DNS Client
            "Dhcp",                // DHCP Client
            "LanmanServer",        // Server
            "LanmanWorkstation",   // Workstation
            "WlanSvc",             // WLAN AutoConfig
            "Themes",              // Themes
            "UserManager",         // User Manager
            "ProfSvc",             // User Profile Service
            "StateRepository",     // State Repository Service
            "InstallService",      // Microsoft Store Install Service
            "AppXSvc",             // AppX Deployment Service
            "ClipSVC",             // Client License Service
            "wlidsvc",             // Microsoft Account Sign-in Assistant
            "tiledatamodelsvc",    // Tile Data model server
            "WpnService",          // Windows Push Notifications
            "wercplsupport",       // Problem Reports and Solutions Control Panel Support
            "WerSvc"               // Windows Error Reporting Service
        };

        /// <summary>Перезапуск всех критических служб Windows.</summary>
        public static void RestartAllCriticalServices()
        {
            Logger.Instance.Header(">>> Перезапуск критических служб Windows (advapi32.dll SCM)");

            int restarted = 0, failed = 0, skipped = 0;

            foreach (var svc in CriticalServices)
            {
                uint state = ServicesNativeApi.GetServiceState(svc);
                if (state == 0)
                {
                    Logger.Instance.Warn($"  [{svc}] — служба не найдена или недоступна");
                    failed++;
                    continue;
                }

                if (state == ServicesNativeApi.SERVICE_RUNNING)
                {
                    // Нельзя перезапускать RpcSs / DcomLaunch — это крашнет систему
                    if (svc == "RpcSs" || svc == "DcomLaunch" || svc == "RpcEptMapper")
                    {
                        Logger.Instance.Info($"  [{svc}] — RUNNING, пропускаем (критичная системная)");
                        skipped++;
                        continue;
                    }

                    Logger.Instance.Info($"  [{svc}] — остановка...");
                    if (ServicesNativeApi.StopService(svc))
                    {
                        System.Threading.Thread.Sleep(1500);
                        Logger.Instance.Info($"  [{svc}] — запуск...");
                        if (ServicesNativeApi.StartService(svc))
                        {
                            Logger.Instance.Success($"  [{svc}] — RUNNING (перезапущен)");
                            restarted++;
                        }
                        else
                        {
                            Logger.Instance.Error($"  [{svc}] — не удалось запустить после остановки!");
                            failed++;
                        }
                    }
                    else
                    {
                        Logger.Instance.Warn($"  [{svc}] — не удалось остановить");
                        failed++;
                    }
                }
                else if (state == ServicesNativeApi.SERVICE_STOPPED)
                {
                    Logger.Instance.Info($"  [{svc}] — STOPPED, запуск...");
                    if (ServicesNativeApi.StartService(svc))
                    {
                        Logger.Instance.Success($"  [{svc}] — RUNNING (запущен)");
                        restarted++;
                    }
                    else
                    {
                        Logger.Instance.Warn($"  [{svc}] — не удалось запустить (возможно DISABLED)");
                        failed++;
                    }
                }
                else
                {
                    Logger.Instance.Info($"  [{svc}] — {ServicesNativeApi.StateToString(state)}, пропускаем");
                    skipped++;
                }
            }

            Logger.Instance.Info($"Итого: перезапущено {restarted}, неудач {failed}, пропущено {skipped}");
        }

        /// <summary>Запуск всех критических служб (без остановки).</summary>
        public static void StartAllCriticalServices()
        {
            Logger.Instance.Header(">>> Запуск всех критических служб");
            int started = 0, failed = 0;

            foreach (var svc in CriticalServices)
            {
                if (ServicesNativeApi.StartService(svc))
                {
                    Logger.Instance.Success($"  [{svc}] — RUNNING");
                    started++;
                }
                else
                {
                    Logger.Instance.Warn($"  [{svc}] — не запущен (DISABLED или занят)");
                    failed++;
                }
            }

            Logger.Instance.Info($"Итого: запущено {started}, неудач {failed}");
        }

        /// <summary>Выводит текущее состояние всех критических служб.</summary>
        public static void PrintCriticalServicesStatus()
        {
            Logger.Instance.Header(">>> Состояние критических служб Windows");

            foreach (var svc in CriticalServices)
            {
                uint state = ServicesNativeApi.GetServiceState(svc);
                string stateStr = state == 0 ? "Не найдена" : ServicesNativeApi.StateToString(state);

                if (state == ServicesNativeApi.SERVICE_RUNNING)
                    Logger.Instance.Success($"  [{svc,-25}] {stateStr}");
                else if (state == ServicesNativeApi.SERVICE_STOPPED)
                    Logger.Instance.Warn($"  [{svc,-25}] {stateStr}");
                else if (state == 0)
                    Logger.Instance.Error($"  [{svc,-25}] {stateStr}");
                else
                    Logger.Instance.Info($"  [{svc,-25}] {stateStr}");
            }
        }

        /// <summary>Перезапуск конкретной службы по имени.</summary>
        public static bool RestartOne(string serviceName)
        {
            Logger.Instance.Info($"Перезапуск службы '{serviceName}'...");
            bool ok = ServicesNativeApi.RestartService(serviceName);
            if (ok) Logger.Instance.Success($"  {serviceName} перезапущена");
            else Logger.Instance.Error($"  Не удалось перезапустить {serviceName}");
            return ok;
        }
    }
}
