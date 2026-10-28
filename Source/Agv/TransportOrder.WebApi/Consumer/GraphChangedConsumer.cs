using Gebhardt.Acc.Common.Messages.NewGraph.Events;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Broadcasts that the graph has changed.
/// </summary>
[UsedImplicitly]
public class GraphChangedConsumer(IGraphPublisher publisher) : IConsumer<GraphUpdated>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<GraphUpdated> context)
    {
        return publisher.SendGraphChanged();
    }
}