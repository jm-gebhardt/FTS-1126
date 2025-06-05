using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.AspNetCore.SignalR;
using AgvPositionStatus = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvPositionStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related Dtos via <see cref="AgvHub"/>
/// </summary>
public class AgvPublisher : IAgvPublisher
{
    private readonly IHubContext<AgvHub, IAgvClient> _hubContext;
    private readonly IMapper _mapper;
    private readonly IMessageFilter<AgvBatteryStatusDto> _agvBatteryStatusFilter;
    private readonly IMessageFilter<AgvPositionStatusDto> _agvPositionFilter;

    /// <summary/>
    public AgvPublisher(
        IHubContext<AgvHub, IAgvClient> hubContext,
        IMessageFilter<AgvBatteryStatusDto> agvBatteryStatusFilter,
        IMessageFilter<AgvPositionStatusDto> agvPositionFilter,
        IMapper mapper
    )
    {
        _hubContext = hubContext;
        _agvBatteryStatusFilter = agvBatteryStatusFilter;
        _agvPositionFilter = agvPositionFilter;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendAgvStateChangedAsync(Messages.MessageContent.Agv agv)
    {
        var agvStateDto = _mapper.Map<AgvStateDto>(agv);
        return _hubContext.Clients.All.AgvStateChanged(agvStateDto);
    }

    /// <inheritdoc/>
    public Task SendAgvModeChangedAsync(Guid agvCorrelationId, AgvModeDto mode)
    {
        var agvModeChangedDto = new AgvModeChangedDto(agvCorrelationId, mode);
        return _hubContext.Clients.All.AgvModeChanged(agvModeChangedDto);
    }

    /// <inheritdoc />
    public Task SendAgvDiscoveredAsync(Messages.MessageContent.Agv agv)
    {
        var agvDto = _mapper.Map<AgvDto>(agv);
        return _hubContext.Clients.All.AgvDiscovered(agvDto);
    }

    /// <inheritdoc/>
    public async Task SendAgvPositionAsync(AgvPositionStatus agvPosition)
    {
        var dto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
        if (await _agvPositionFilter.ShouldActAsync(dto))
        {
            await _hubContext.Clients.All.AgvPositionChanged(dto);
        }
    }

    /// <inheritdoc/>
    public Task SendAgvRouteChangedAsync(CurrentAgvRouteReadModel currentAgvRouteReadModel)
    {
        var dto = _mapper.Map<CurrentAgvRouteDto>(currentAgvRouteReadModel);
        return _hubContext.Clients.All.AgvRouteChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAssignedActionGroupChangedAsync(Guid correlationId, Guid? assignedActionGroupCorrelationId)
    {
        var dto = new AssignedActionGroupDto(correlationId, assignedActionGroupCorrelationId);
        return _hubContext.Clients.All.AssignedActionGroupChanged(dto);
    }

    /// <inheritdoc/>
    public async Task SendAgvBatteryStatusAsync(AgvBatteryChargeChanged batteryChargeChanged)
    {
        var dto = _mapper.Map<AgvBatteryStatusDto>(batteryChargeChanged.AgvBatteryStatus);
        if (await _agvBatteryStatusFilter.ShouldActAsync(dto))
        {
            await _hubContext.Clients.All.BatteryStatusChanged(dto);
        }
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

    /// <inheritdoc/>
    public Task SendCargoChangeAsync(IReadOnlyCollection<Cargo> cargos)
    {
        var dto = _mapper.Map<ICollection<CargoDto>>(cargos);
        return _hubContext.Clients.All.CargoChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvHasCargoChangedAsync(Guid correlationId, bool hasCargo)
    {
        var dto = new AgvHasCargoDto(correlationId, hasCargo);
        return _hubContext.Clients.All.AgvHasCargoChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvZonesChangedAsync(Guid correlationId, ICollection<string> zones)
    {
        var dto = new AgvZonesDto(correlationId, zones);
        return _hubContext.Clients.All.AgvZonesChanged(dto);
    }
}