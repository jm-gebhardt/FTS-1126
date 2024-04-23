using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the offline watchdog of agvs.
/// </summary>
public class OfflineWatchdogInitializer(IBus bus, IAgvDb agvDb) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var agv in await agvDb.Agvs.GetAllAgvsAsync())
        {
            await bus.Publish(new ResetOfflineWatchdog(agv.CorrelationId), stoppingToken);
        }
    }
}