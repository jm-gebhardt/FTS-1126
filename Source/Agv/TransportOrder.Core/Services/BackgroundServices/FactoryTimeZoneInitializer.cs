using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the factory time zone.
/// </summary>
public class FactoryTimeZoneInitializer(IFactoryTimeRepository factoryTimeRepository) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await factoryTimeRepository.GetFactoryTime();
        }
        catch (InvalidOperationException)
        {
            await factoryTimeRepository.SetFactoryTime(TimeZoneInfo.Local);
        }
    }
}