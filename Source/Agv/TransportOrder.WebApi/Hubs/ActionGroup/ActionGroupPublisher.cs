using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Publishes ActionGroup related Dtos via <see cref="ActionGroupHub"/>
/// </summary>
public class ActionGroupPublisher : IActionGroupPublisher
{
    private readonly IHubContext<ActionGroupHub, IActionGroupClient> _hubContext;
    private readonly IMapper _mapper;

    /// <summary/>
    public ActionGroupPublisher(IHubContext<ActionGroupHub, IActionGroupClient> hubContext, IMapper mapper)
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendActionGroupCreatedAsync(ActionGroupReadModel actionGroup)
    {
        var actionGroupDto = _mapper.Map<ActionGroupDto>(actionGroup);
        return _hubContext.Clients.All.ActionGroupCreated(actionGroupDto);
    }

    /// <inheritdoc/>
    public Task SendActionGroupChangedAsync(ActionGroupReadModel actionGroup)
    {
        var actionGroupDto = _mapper.Map<ActionGroupDto>(actionGroup);
        return _hubContext.Clients.All.ActionGroupChanged(actionGroupDto);
    }
}