using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Represents the handler for node changes.
/// </summary>
[UsedImplicitly]
public class NodeChangedConsumer(
    IMapPublisher mapPublisher, 
    ILogger<NodeChangedConsumer> logger) : 
    IConsumer<NodeStateChanged>,
    IConsumer<NodeApproachingPropertiesUpdated>,
    IConsumer<JamStateChanged>
{
    /// <summary>
    /// Handles the <see cref="NodeStateChanged"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<NodeStateChanged> context)
    {
        var (nodeLock, stationId, modifyingUser, lockReason, lockDescription) = context.Message;
        
        if (stationId != null)
        {
            logger.LogStationInformation(
                LogCodes.StationStateChanged, 
                stationId.Value, 
                "State Changed",
                $"State of station changed to '{nodeLock.NodeState}'",
                new Dictionary<string, object?> { { LogLabels.NodeState, nodeLock.NodeState } });
        }
        
        await mapPublisher.SendNodeStateChangedAsync(nodeLock, stationId, modifyingUser, lockReason, lockDescription);
    }

    /// <summary>
    /// Handles the <see cref="NodeApproachingPropertiesUpdated"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<NodeApproachingPropertiesUpdated> context)
    {
        (var mapId, string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;
        await mapPublisher.SendNodeApproachingPropertiesUpdated(mapId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection);
    }

    /// <summary>
    /// Handles the <see cref="JamStateChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<JamStateChanged> context)
    {
        await mapPublisher.SendJamStateUpdate(context.Message.CorrelationId, context.Message.HasPalletJam);
    }
}