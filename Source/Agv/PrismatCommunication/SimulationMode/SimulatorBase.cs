using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <summary>
/// Implements an abstract way to simulate host orders.
/// </summary>
public abstract class SimulatorBase(IBus bus, ILogger<SimulatorBase> logger, KaeserGraphConfiguration kaeserGraphConfiguration) : IPollable
{
    /// <summary>
    /// List of sources/destinations list of this simulation communicator.
    /// </summary>
    protected abstract List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; }

    /// <inheritdoc cref="IPollable.PollingInterval"/>
    public abstract int PollingInterval { get; }

    private uint _orderIdCounter;

    private string _source = string.Empty;

    private readonly Random _random = new ();

    /// <inheritdoc cref="IPollable.Poll"/>
    public async Task Poll(CancellationToken token)
    {
        if (AllTriggerAreLocalized())
        {
            (List<string> sources, List<string> destinations) = SourceDestinationPools[_random.Next(SourceDestinationPools.Count)];
            List<string> rotationValues = ["2", "4"];

            _source = sources[_random.Next(sources.Count)];
            string destination = destinations[_random.Next(destinations.Count)];
            string orderId = NextOrderId();
            var correlationId = orderId.ToGuid();
            const string leNo = "9999";
            string leRotation = rotationValues[_random.Next(rotationValues.Count)];

            var orderDetails = new OrderDetails(correlationId, orderId, _source, destination, leNo, HostOrderCreator.Simulation, leRotation);

            await bus.Publish(new CreateHostOrder("handlingUnit", orderDetails), token);
            logger.LogInformation("Creating new host order, {orderId}", orderId);
        }
        else
        {
            logger.LogInformation("Waiting for all triggers localized.");
        }
    }

    private string NextOrderId()
    {
        return $"SIM_{DateTime.Now:yyyy-MM-ddTHH:mm}_{_source}_{_orderIdCounter++:D8}";
    }

    /// <summary>
    /// Returns if all trigger have been localized.
    /// </summary>
    private bool AllTriggerAreLocalized()
    {
        bool allTriggerLocalized = true;
        ConcurrentDictionary<string, string> triggerPointMapping = kaeserGraphConfiguration.TriggerPointMapping.ConcurrentDict;
        foreach (KeyValuePair<string, string> triggerPoint in triggerPointMapping)
        {
            if (!triggerPoint.Value.IsNullOrEmpty())
            {
                continue;
            }
            allTriggerLocalized = false;
            logger.LogDebug("{MethodName}: Trigger {TriggerKey} is not assigned to any location.", nameof(AllTriggerAreLocalized), triggerPoint.Key);
        }

        return allTriggerLocalized;
    }
}