using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related Dtos via <see cref="AgvHub"/>
/// </summary>
public class AgvPublisher : IAgvPublisher
{
    private readonly IHubContext<AgvHub, IAgvClient> _hubContext;
    private readonly IMapper _mapper;

    /// <summary/>
    public AgvPublisher(IHubContext<AgvHub, IAgvClient> hubContext, IMapper mapper)
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendAgvStateChangedAsync(Messages.MessageContent.Agv agv)
    {
        var agvStateDto = _mapper.Map<AgvStateDto>(agv);
        return _hubContext.Clients.All.AgvStateChanged(agvStateDto);
    }

    /// <inheritdoc />
    public Task SendAgvDiscoveredAsync(Messages.MessageContent.Agv agv)
    {
        var agvDto = _mapper.Map<AgvDto>(agv);
        return _hubContext.Clients.All.AgvDiscovered(agvDto);
    }

    /// <inheritdoc/>
    public Task SendAgvPositionAsync(AgvPositionStatus agvPosition)
    {
        var dto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
        return _hubContext.Clients.All.AgvPositionChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvRouteChangedAsync(CurrentAgvRouteReadModel currentAgvRouteReadModel)
    {
        var dto = _mapper.Map<CurrentAgvRouteDto>(currentAgvRouteReadModel);
        return _hubContext.Clients.All.AgvRouteChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAssignedActionGroupChangedAsync(Guid correlationId, Common.Messages.ActionGroups.ActionGroup? assignedActionGroup)
    {
        var dto = new AssignedActionGroupDto(correlationId, _mapper.Map<ActionGroupDto>(assignedActionGroup));
        return _hubContext.Clients.All.AssignedActionGroupChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvBatteryStatus(AgvBatteryChargeChanged batteryChargeChanged)
    {
        var dto = _mapper.Map<AgvBatteryStatusDto>(batteryChargeChanged);
        return _hubContext.Clients.All.SendBatteryStatus(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvLockedChangedAsync(Guid correlationId, bool isLocked)
    {
        var dto = new AgvLockedStatusDto(correlationId, isLocked);
        return _hubContext.Clients.All.AgvLockedChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvOnlineChangedAsync(Guid correlationId, bool isOnline)
    {
        var dto = new AgvOnlineStatusDto(correlationId, isOnline);
        return _hubContext.Clients.All.AgvOnlineChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvErrorChangedAsync(Guid correlationId, List<AgvError> errors)
    {
        var agvErrorDtos = _mapper.Map<AgvErrorStatusDto>(errors);
        _mapper.Map(correlationId, agvErrorDtos);
        return _hubContext.Clients.All.AgvErrorChanged(agvErrorDtos);
    }

    /// <inheritdoc/>
    public Task SendAgvRemovedAsync(Guid correlationId)
    {
        var dto = new AgvRemovedDto(correlationId);
        return _hubContext.Clients.All.AgvRemoved(dto);
    }
}