using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Represents the handler for node state changes.
/// </summary>
[UsedImplicitly]
public class NodeStateChangedConsumer : IConsumer<NodeStateChanged>
{
    private readonly IMapPublisher _mapPublisher;

    /// <inheritdoc cref="NodeStateChangedConsumer"/>>.
    public NodeStateChangedConsumer(IMapPublisher mapPublisher)
    {
        _mapPublisher = mapPublisher;
    }

    /// <summary>
    /// Handles the <see cref="NodeStateChanged"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<NodeStateChanged> context)
    {
        var nodeState = context.Message.NodeState;
        await _mapPublisher.SendNodeStateChangedAsync(nodeState, context.Message.StationId);
    }
}