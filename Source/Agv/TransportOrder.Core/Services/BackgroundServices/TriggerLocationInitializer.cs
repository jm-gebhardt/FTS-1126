using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the trigger locations (after restart of transport order).
/// </summary>
/// <param name="triggerRepository"></param>
public class TriggerLocationsInitializer(ITriggerRepository triggerRepository) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await triggerRepository.LoadTriggerToLocationDictionary();
    }
}