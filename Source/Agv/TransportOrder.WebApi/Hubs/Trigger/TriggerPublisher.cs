using AutoMapper;
using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <inheritdoc cref="ITriggerPublisher"/>>
public class TriggerPublisher(IHubBroadcaster<TriggerHub, ITriggerClient> hubBroadcaster, IMapper mapper) : ITriggerPublisher
{
    /// <inheritdoc cref="ITriggerPublisher"/>>
    public Task SendTriggerChangedAsync(Domain.Models.Trigger.Trigger trigger)
    {
        var dto = mapper.Map<TriggerDto>(trigger);
        hubBroadcaster.Broadcast(c => c.TriggerChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="ITriggerPublisher"/>>
    public Task SendTriggerDeletedAsync(Guid triggerId)
    {
        hubBroadcaster.Broadcast(c => c.TriggerDeleted(triggerId));
        return Task.CompletedTask;
    }
}