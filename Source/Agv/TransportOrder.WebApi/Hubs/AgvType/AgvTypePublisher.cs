using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;

/// <inheritdoc/>
public class AgvTypePublisher(IHubBroadcaster<AgvTypeHub, IAgvTypeClient> hubBroadcaster) : IAgvTypePublisher
{
    /// <inheritdoc/>
    public Task SendAgvTypeAcknowledged(Guid id)
    {
        var dto = new AgvTypeAcknowledgedDto(id);
        hubBroadcaster.Broadcast(c => c.AgvTypeAcknowledged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvTypeCreated(AgvTypeDto agvType)
    {
        hubBroadcaster.Broadcast(c => c.AgvTypeCreated(agvType));
        return Task.CompletedTask;
    }
}