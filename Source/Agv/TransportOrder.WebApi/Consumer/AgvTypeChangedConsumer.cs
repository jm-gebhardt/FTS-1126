using Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Broadcasts agv type related updates
/// </summary>
[UsedImplicitly]
public class AgvTypeChangedConsumer(IAgvTypePublisher publisher):
    IConsumer<AgvTypeAcknowledged>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvTypeAcknowledged> context)
    {
        return publisher.SendAgvTypeAcknowledged(context.Message.CorrelationId);
    }
}