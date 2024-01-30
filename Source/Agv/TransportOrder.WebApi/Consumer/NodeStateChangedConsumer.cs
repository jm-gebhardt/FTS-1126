using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

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
        await _mapPublisher.SendNodeStateChangedAsync(nodeState);
    }
}