using AutoMapper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <inheritdoc cref="ITriggerPublisher"/>>
public class TriggerPublisher(IHubContext<TriggerHub, ITriggerClient> hubContext, IMapper mapper) : ITriggerPublisher
{
    /// <inheritdoc cref="ITriggerPublisher"/>>
    public Task SendTriggerChangedAsync(Domain.Models.Trigger.Trigger trigger)
    {
        var triggerDto = mapper.Map<TriggerDto>(trigger);
        return hubContext.Clients.All.TriggerChanged(triggerDto);
    }
}