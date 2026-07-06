using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the map.
/// </summary>
public class MapInitializer(IBus bus) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bus.Publish(new GenerateDefaultMap(), stoppingToken);
    }
}