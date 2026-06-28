using System;
using System.Collections.Generic;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine.Modules
{
    // ===========================================================================
    //  NETWORK DIAGNOSTIC MANAGER
    //  Диагностика сети через WinHTTP/WinINet/Winsock. Проверка
    //  доступности серверов Microsoft, состояния сети, прокси.
    // ===========================================================================

    public static class NetworkDiagnosticManager
    {
        /// <summary>Сервера, которые должны быть доступны для Windows Update.</summary>
        public static readonly (string Host, ushort Port, string Path, bool Https)[] WU_Servers =
        {
            ("windowsupdate.microsoft.com", 443, "/", true),
            ("update.microsoft.com", 443, "/", true),
            ("download.windowsupdate.com", 443, "/", true),
            ("dl.delivery.mp.microsoft.com", 443, "/", true),
            ("tsfe.trafficshaping.dsp.mp.microsoft.com", 443, "/", true),
            ("sls.update.microsoft.com", 443, "/", true)
        };

        /// <summary>Проверяет доступность серверов Windows Update.</summary>
        public static void CheckWindowsUpdateConnectivity()
        {
            Logger.Instance.Header(">>> Проверка доступности Windows Update (WinHTTP)");

            // 1. Проверка подключения через WinINet
            bool online = NetNativeApi.IsConnectedToInternet(out var flags);
            if (online)
            {
                Logger.Instance.Success("Подключение к интернету: активно");
                if ((flags & NetNativeApi.INTERNET_CONNECTION_LAN) != 0)   Logger.Instance.Info("  через LAN");
                if ((flags & NetNativeApi.INTERNET_CONNECTION_MODEM) != 0)  Logger.Instance.Info("  через модем");
                if ((flags & NetNativeApi.INTERNET_CONNECTION_PROXY) != 0)  Logger.Instance.Info("  через прокси");
                if ((flags & NetNativeApi.INTERNET_CONNECTION_OFFLINE) != 0) Logger.Instance.Warn("  режим OFFLINE!");
            }
            else
            {
                Logger.Instance.Error("Нет подключения к интернету.");
                return;
            }

            // 2. Инициализация Winsock
            if (!NetNativeApi.InitWinsock())
            {
                Logger.Instance.Error("Не удалось инициализировать Winsock.");
                return;
            }

            // 3. Проверка доступности серверов WU
            int ok = 0, fail = 0;
            foreach (var srv in WU_Servers)
            {
                Logger.Instance.Info($"Проверка: {srv.Host}...");
                int? status = NetNativeApi.CheckUrl(srv.Host, srv.Port, srv.Path, srv.Https);
                if (status.HasValue)
                {
                    if (status >= 200 && status < 400)
                    {
                        Logger.Instance.Success($"  {srv.Host} → HTTP {status}");
                        ok++;
                    }
                    else
                    {
                        Logger.Instance.Warn($"  {srv.Host} → HTTP {status}");
                        ok++; // Сервер доступен, но вернул ошибочный код
                    }
                }
                else
                {
                    Logger.Instance.Error($"  {srv.Host} → НЕДОСТУПЕН");
                    fail++;
                }
            }

            NetNativeApi.CleanupWinsock();

            Logger.Instance.Info($"Итого: {ok} серверов доступно, {fail} недоступно");
        }

        /// <summary>Сбрасывает Winsock и TCP/IP стек.</summary>
        public static void ResetNetworkStack()
        {
            Logger.Instance.Header(">>> Сброс сетевого стека (Winsock + TCP/IP + DNS)");
            Logger.Instance.Warn("Эта операция переустановит Winsock и TCP/IP.");
            Logger.Instance.Warn("Требуется перезагрузка для применения изменений.");
            if (!ConsoleHelper.ReadYesNo("Продолжить?", false)) return;

            // 1. Сброс Winsock: netsh winsock reset (прямой вызов netsh.exe)
            Logger.Instance.Info("Сброс Winsock...");
            int ec = ConsoleHelper.RunExternal("netsh.exe", "winsock reset", out _, out var se);
            Logger.Instance.Info($"  netsh winsock reset → код {ec}");

            // 2. Сброс TCP/IP
            Logger.Instance.Info("Сброс TCP/IP...");
            ec = ConsoleHelper.RunExternal("netsh.exe", "int ip reset", out _, out se);
            Logger.Instance.Info($"  netsh int ip reset → код {ec}");

            // 3. Сброс DNS-кэша
            Logger.Instance.Info("Очистка DNS-кэша (ipconfig /flushdns)...");
            ec = ConsoleHelper.RunExternal("ipconfig.exe", "/flushdns", out var so, out se);
            if (ec == 0)
                Logger.Instance.Success("DNS-кэш очищен.");
            else
                Logger.Instance.Error($"ipconfig вернул {ec}: {se}");

            // 4. Регистрация DNS
            Logger.Instance.Info("Перерегистрация DNS (ipconfig /registerdns)...");
            ec = ConsoleHelper.RunExternal("ipconfig.exe", "/registerdns", out so, out se);
            Logger.Instance.Info($"  ipconfig /registerdns → код {ec}");

            // 5. Сброс брандмауэра
            Logger.Instance.Info("Сброс брандмауэра Windows (netsh advfirewall reset)...");
            ec = ConsoleHelper.RunExternal("netsh.exe", "advfirewall reset", out _, out se);
            if (ec == 0)
                Logger.Instance.Success("Брандмауэр сброшен к настройкам по умолчанию.");
            else
                Logger.Instance.Error($"netsh advfirewall reset → код {ec}");

            Logger.Instance.Warn("ТРЕБУЕТСЯ ПЕРЕЗАГРУЗКА для применения изменений.");
        }

        /// <summary>Выводит конфигурацию сети.</summary>
        public static void ShowNetworkConfig()
        {
            Logger.Instance.Header(">>> Конфигурация сети (ipconfig /all)");
            int ec = ConsoleHelper.RunExternal("ipconfig.exe", "/all", out var so, out var se);
            if (ec == 0)
                Logger.Instance.Info(so);
            else
                Logger.Instance.Error($"ipconfig вернул {ec}: {se}");
        }

        /// <summary>Очищает кэш WinHTTP-прокси.</summary>
        public static void ResetWinHttpProxy()
        {
            Logger.Instance.Header(">>> Сброс WinHTTP-прокси (netsh winhttp reset proxy)");
            int ec = ConsoleHelper.RunExternal("netsh.exe", "winhttp reset proxy", out var so, out var se);
            if (ec == 0)
            {
                Logger.Instance.Success("WinHTTP-прокси сброшен (теперь 'Direct access').");
                Logger.Instance.Info(so);
            }
            else
                Logger.Instance.Error($"netsh вернул {ec}: {se}");
        }
    }
}
