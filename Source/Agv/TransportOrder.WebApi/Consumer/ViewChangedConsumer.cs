using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Events.Views;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Broadcasts created, changed and deleted views.
/// </summary>
[UsedImplicitly]
public class ViewChangedConsumer(IGraphPublisher publisher, IMapper mapper) :
    IConsumer<ViewChanged>,
    IConsumer<ViewDeleted>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ViewChanged> context)
    {
        return publisher.SendViewChanged(mapper.Map<ViewDto>(context.Message.View));
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ViewDeleted> context)
    {
        return publisher.SendViewDeleted(context.Message.CorrelationId);
    }
}