using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Represents the handler for node state changes.
/// </summary>
[UsedImplicitly]
public class NodeStateChangedConsumer(IMapPublisher mapPublisher, ILogger<NodeStateChangedConsumer> logger) : IConsumer<NodeStateChanged>
{
    /// <summary>
    /// Handles the <see cref="NodeStateChanged"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<NodeStateChanged> context)
    {
        var (nodeState, stationId) = context.Message;
        if (stationId != null)
        {
            logger.LogStationInformation(LogCodes.StationStateChanged, stationId.Value, "State Changed",
                $"State of station changed to '{nodeState.NodeState}'",
                new Dictionary<string, object?> { { LogLabels.NodeState, nodeState.NodeState } });
        }
     
        await mapPublisher.SendNodeStateChangedAsync(nodeState, stationId);
    }
}