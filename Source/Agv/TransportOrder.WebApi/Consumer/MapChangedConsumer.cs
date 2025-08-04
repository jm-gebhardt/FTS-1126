using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Handles events related to changes in the map background.
/// </summary>
/// <param name="mapPublisher">Service for publishing map updates.</param>
[UsedImplicitly]
public class MapChangedConsumer(IMapPublisher mapPublisher) : IConsumer<MapChanged>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<MapChanged> context)
    {
        return mapPublisher.SendMapChangedAsync(context.Message.Map, context.Message.HasBackgroundChanged);
    }
}