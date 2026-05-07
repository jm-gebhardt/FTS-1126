using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the factory time zone.
/// </summary>
/// <remarks>
/// On first boot (when the factory time has not been persisted yet) the seed is determined in this order:
/// <list type="number">
///   <item>Configuration key <c>FACTORY:TIMEZONE</c> (settable via container env var <c>FACTORY__TIMEZONE</c>), if it resolves to a known IANA/Windows time zone id.</item>
///   <item><see cref="TimeZoneInfo.Local"/> as a fallback. Inside containers this matches the host when <c>/etc/localtime</c> is mounted or the <c>TZ</c> env var is set; otherwise it is UTC.</item>
/// </list>
/// After the seed exists, the value is managed through the API and this initializer does nothing.
/// </remarks>
public class FactoryTimeZoneInitializer(
    IFactoryTimeRepository factoryTimeRepository,
    IConfiguration configuration,
    ILogger<FactoryTimeZoneInitializer> logger) : BackgroundService
{
    private const string TimeZoneConfigKey = "FACTORY:TIMEZONE";

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await factoryTimeRepository.GetFactoryTime();
        }
        catch (InvalidOperationException)
        {
            await factoryTimeRepository.SetFactoryTime(ResolveSeedTimeZone());
        }
    }

    private TimeZoneInfo ResolveSeedTimeZone()
    {
        string? configured = configuration.GetValue<string>(TimeZoneConfigKey);
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogInformation(
                "No '{ConfigKey}' configured; seeding factory time zone from host local '{LocalId}'.",
                TimeZoneConfigKey, TimeZoneInfo.Local.Id);
            return TimeZoneInfo.Local;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(configured, out var resolved))
        {
            logger.LogInformation(
                "Seeding factory time zone from configuration '{ConfigKey}' = '{TimeZoneId}'.",
                TimeZoneConfigKey, resolved.Id);
            return resolved;
        }

        logger.LogWarning(
            "Configured '{ConfigKey}' = '{TimeZoneId}' could not be resolved; falling back to host local '{LocalId}'.",
            TimeZoneConfigKey, configured, TimeZoneInfo.Local.Id);
        return TimeZoneInfo.Local;
    }
}
