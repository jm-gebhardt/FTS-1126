using Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Tick;

/// <summary>
/// Publishes a <see cref="TrafficManagementTick"/> to the bus on a fixed interval, unconditionally.
/// The <see cref="TrafficManagementEventHandler"/> consumer is responsible for idempotently discarding
/// ticks that arrive too soon after the previous one was processed (see <see cref="TrafficManagementTickState"/>).
/// </summary>
internal sealed class TrafficManagementTickPublisher(IBus bus) : BackgroundService
{
    /// <summary>
    /// The interval at which to publish <see cref="TrafficManagementTick"/> messages.
    /// </summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await bus.Publish(new TrafficManagementTick(), stoppingToken);
        }
    }
}
