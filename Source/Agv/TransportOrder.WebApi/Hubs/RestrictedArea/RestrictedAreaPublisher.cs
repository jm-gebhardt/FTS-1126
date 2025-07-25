using AutoMapper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;

/// <inheritdoc cref="IRestrictedAreaPublisher"/>>
public class RestrictedAreaPublisher(IHubBroadcaster<RestrictedAreaHub, IRestrictedAreaClient> hubBroadcaster, IMapper mapper) : IRestrictedAreaPublisher
{
    /// <inheritdoc cref="IRestrictedAreaPublisher"/>>
    public Task SendRestrictedAreaChangedAsync(Domain.Models.RestrictedArea.RestrictedArea restrictedArea)
    {
        var dto = mapper.Map<RestrictedAreaDto>(restrictedArea);
        hubBroadcaster.Broadcast(c => c.RestrictedAreaChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IRestrictedAreaPublisher"/>>
    public Task SendRestrictedAreaDeletedAsync(Guid id)
    {
        var dto = new RestrictedAreaDeletedDto(id);
        hubBroadcaster.Broadcast(c => c.RestrictedAreaDeleted(dto));
        return Task.CompletedTask;
    }
}