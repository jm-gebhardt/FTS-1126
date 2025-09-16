using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <summary>
/// Implements an abstract way to simulate host orders.
/// </summary>
public abstract class SimulatorBase(IBus bus, ILogger<SimulatorBase> logger, KaeserGraphConfiguration kaeserGraphConfiguration) : IHostOrderCreator
{
    private readonly Random _random = Random.Shared;
    private bool _isSimulationPaused;

    /// <summary>
    /// List of sources/destinations list of this simulation communicator.
    /// </summary>
    protected abstract List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; }

    /// <inheritdoc cref="IPollable.PollingInterval"/>
    public abstract int PollingInterval { get; }

    private uint _orderIdCounter;

    private string _source = string.Empty;

    /// <inheritdoc cref="IPollable.Poll"/>
    public async Task Poll(CancellationToken token)
    {
        if (ShouldSkipExecution())
        {
            return;
        }

        (List<string> sources, List<string> destinations) = SourceDestinationPools[_random.Next(SourceDestinationPools.Count)];

        _source = sources[_random.Next(sources.Count)];
        string destination = destinations[_random.Next(destinations.Count)];
        string orderId = NextOrderId();
        var createdDate = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        string leNo = HostOrder.GetTransportOrderId(orderId);

        var orderDetails = new OrderDetails(correlationId, orderId, _source, destination, leNo, HostOrderCreator.Simulation, createdDate, 0);

        await bus.Publish(new CreateHostOrder(orderDetails.OrderId, orderDetails), token);
        logger.LogInformation("Creating new host order, {OrderId}", orderId);
    }

    private bool ShouldSkipExecution()
    {
        if (!AllTriggerAreLocalized())
        {
            logger.LogInformation("Waiting for all triggers localized");
            return true;
        }

        return _isSimulationPaused;
    }

    private string NextOrderId()
    {
        return $"{_source}{_orderIdCounter++:D8}";
    }

    /// <summary>
    /// Returns if all trigger have been localized.
    /// </summary>
    private bool AllTriggerAreLocalized()
    {
        bool allTriggerLocalized = true;
        ConcurrentDictionary<string, Guid> triggerPointMapping = kaeserGraphConfiguration.TriggerPointMapping.ConcurrentDict;
        foreach (KeyValuePair<string, Guid> triggerPoint in triggerPointMapping)
        {
            if (triggerPoint.Value != Guid.Empty)
            {
                continue;
            }
            allTriggerLocalized = false;
            logger.LogInformation("{MethodName}: Trigger {TriggerKey} is not assigned to any location", nameof(AllTriggerAreLocalized), triggerPoint.Key);
        }

        return allTriggerLocalized;
    }

    /// <inheritdoc />
    public void Pause()
    {
        _isSimulationPaused = true;
    }

    /// <inheritdoc />
    public void Resume()
    {
        _isSimulationPaused = false;
    }
}