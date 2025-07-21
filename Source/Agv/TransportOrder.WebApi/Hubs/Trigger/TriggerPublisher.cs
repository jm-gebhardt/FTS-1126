using AutoMapper;
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
}