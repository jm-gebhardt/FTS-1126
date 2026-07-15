using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Broadcasts agv type related updates
/// </summary>
[UsedImplicitly]
public class AgvTypeChangedConsumer(IAgvTypePublisher publisher, IMapper mapper):
    IConsumer<AgvTypeAcknowledged>,
    IConsumer<AgvTypeAdded>,
    IConsumer<AgvTypeUpdated>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvTypeAdded> context)
    {
        var dto = mapper.Map<AgvTypeDto>(context.Message.AgvType) with { Id = context.Message.CorrelationId };
        return publisher.SendAgvTypeCreated(dto);
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvTypeAcknowledged> context)
    {
        return publisher.SendAgvTypeAcknowledged(context.Message.CorrelationId);
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvTypeUpdated> context)
    {
        var dto = mapper.Map<AgvTypeDto>(context.Message.AgvType) with { Id = context.Message.CorrelationId };

        return publisher.SendAgvTypeUpdated(dto);
    }
}