using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Publishes ActionGroup related Dtos via <see cref="ActionGroupHub"/>
/// </summary>
public class ActionGroupPublisher : IActionGroupPublisher
{
    private readonly IHubBroadcaster<ActionGroupHub, IActionGroupClient> _hubBroadcaster;
    private readonly IMapper _mapper;

    /// <summary/>
    public ActionGroupPublisher(IHubBroadcaster<ActionGroupHub, IActionGroupClient> hubBroadcaster, IMapper mapper)
    {
        _hubBroadcaster = hubBroadcaster;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendActionGroupCreatedAsync(ActionGroupReadModel actionGroup)
    {
        var dto = _mapper.Map<ActionGroupDto>(actionGroup);
        _hubBroadcaster.Broadcast(c => c.ActionGroupCreated(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendActionGroupChangedAsync(ActionGroupReadModel actionGroup)
    {
        var dto = _mapper.Map<ActionGroupDto>(actionGroup);
        _hubBroadcaster.Broadcast(c => c.ActionGroupChanged(dto));
        return Task.CompletedTask;
    }
}