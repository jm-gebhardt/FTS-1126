using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Provides extension methods for <see cref="TelegramCommunicationConfiguration"/>
/// </summary>
public static class TelegramConfigurationExtension
{
    /// <summary>
    /// Load configuration from appsettings.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="configSection"></param>
    public static void LoadFromConfig(this TelegramCommunicationConfiguration config, IConfigurationSection configSection)
    {
        config.IpAddress = configSection.GetValue("HOST", "127.0.0.1")!;
        config.Port = int.Parse(configSection.GetValue("PORT", "5009")!);
        config.TcpKeepAliveInterval = 1002;
        config.UseTcpKeepAlive = true;
        config.TcpKeepAliveRetries =int.Parse(configSection.GetValue("TCP_KEEP_ALIVE_RETRIES", "5")!);
        config.TelegramWaitingTime =int.Parse(configSection.GetValue("TELEGRAM_WAITING_TIME", "180000")!);
        config.LifeTelegramInterval =int.Parse(configSection.GetValue("LIFE_TELEGRAM_INTERVAL", "60")!);
    }
}