using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Handles events related to changes in the map background.
/// </summary>
[UsedImplicitly]
public class MapChangedConsumer(IMapPublisher mapPublisher, IMapper mapper) : 
    IConsumer<MapChanged>,
    IConsumer<LogEntryCreated>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<MapChanged> context)
    {
        return mapPublisher.SendMapChangedAsync(context.Message.Map, context.Message.HasBackgroundChanged);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LogEntryCreated> context)
    {
        var logEntry = context.Message.LogEntry;

        if (logEntry.Category == Categories.Station)
        {
            await mapPublisher.SendLogEntryCreated(logEntry.StationId.GetValueOrDefault(), mapper.Map<LogEntry>(logEntry));
        }
    }
}