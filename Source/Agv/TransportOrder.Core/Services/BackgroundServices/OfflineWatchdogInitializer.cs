using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.BackgroundServices;

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