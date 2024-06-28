using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <summary>
/// Implements an abstract way to simulate host orders.
/// </summary>
public abstract class SimulatorBase(IBus bus, ILogger<SimulatorBase> logger) : IPollable
{
    /// <summary>
    /// List of sources/destinations list of this simulation communicator.
    /// </summary>
    protected abstract List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; }

    /// <inheritdoc cref="IPollable.PollingInterval"/>
    public abstract int PollingInterval { get; }

    private uint _orderIdCounter;

    private readonly Random _random = new ();

    /// <inheritdoc cref="IPollable.Poll"/>
    public async Task Poll(CancellationToken token)
    {
        (List<string> sources, List<string> destinations) = SourceDestinationPools[_random.Next(SourceDestinationPools.Count)];
        
        string orderId = NextOrderId();
        var correlationId = orderId.ToGuid();
        string source = sources[_random.Next(sources.Count)];
        string destination = destinations[_random.Next(destinations.Count)];
        const string leNo = "9999";
        var orderDetails = new OrderDetails(correlationId, orderId, source, destination, leNo, HostOrderCreator.Simulation);

        await bus.Publish(new CreateHostOrder("handlingUnit", orderDetails), token);
        logger.LogInformation("Creating new host order, {orderId}", orderId);
    }

    private string NextOrderId()
    {
        return $"SIM_{DateTime.Now:yyyy-MM-ddTHH:mm}_{_orderIdCounter++:D8}";
    }
}