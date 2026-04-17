using Gebhardt.Acc.Common.Messages.Stations.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Requests station online states on startup.
/// </summary>
[UsedImplicitly]
public class StationOnlineSyncService(
    IBus bus,
    ILogger<StationOnlineSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(_startupDelay, stoppingToken);

        logger.LogInformation("Requesting station online states");
        await bus.Publish(new StationOnlineStatesRequested(), stoppingToken);
    }
}