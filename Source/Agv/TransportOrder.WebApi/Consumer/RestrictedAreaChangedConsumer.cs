using Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Handles restricted area related events.
/// </summary>
[UsedImplicitly]
public class RestrictedAreaChangedConsumer(IRestrictedAreaPublisher publisher):
    IConsumer<RestrictedAreaUpdated>,
    IConsumer<RestrictedAreaDeleted>
{
    /// <summary>
    /// Handles the <see cref="RestrictedAreaUpdated"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<RestrictedAreaUpdated> context)
    {
        return publisher.SendRestrictedAreaChangedAsync(context.Message.RestrictedArea);
    }

    /// <summary>
    /// Handles the <see cref="RestrictedAreaDeleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<RestrictedAreaDeleted> context)
    {
        await publisher.SendRestrictedAreaDeletedAsync(context.Message.CorrelationId);
    }
}