using AutoMapper;
using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using AgvPositionStatus = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvPositionStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related Dtos via <see cref="AgvHub"/>
/// </summary>
public class AgvPublisher : IAgvPublisher
{
    private readonly IMapper _mapper;
    private readonly IHubBroadcaster<AgvHub, IAgvClient> _hubBroadcaster;
    private readonly IMessageFilter<AgvBatteryStatusDto> _agvBatteryStatusFilter;
    private readonly IMessageFilter<AgvPositionStatusDto> _agvPositionFilter;
    private readonly IAgvDb _agvDb;

    /// <summary/>
    public AgvPublisher(
        IHubBroadcaster<AgvHub, IAgvClient> hubBroadcaster,
        IMessageFilter<AgvBatteryStatusDto> agvBatteryStatusFilter,
        IMessageFilter<AgvPositionStatusDto> agvPositionFilter,
        IAgvDb agvDb,
        IMapper mapper
    )
    {
        _hubBroadcaster = hubBroadcaster;
        _agvBatteryStatusFilter = agvBatteryStatusFilter;
        _agvPositionFilter = agvPositionFilter;
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendAgvStateChangedAsync(Messages.MessageContent.Agv agv)
    {
        var dto = _mapper.Map<AgvStateDto>(agv);
        _hubBroadcaster.Broadcast(c => c.AgvStateChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvModeChangedAsync(Guid agvCorrelationId, AgvModeDto mode)
    {
        var dto = new AgvModeChangedDto(agvCorrelationId, mode);
        _hubBroadcaster.Broadcast(c => c.AgvModeChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAgvDiscoveredAsync(Messages.MessageContent.Agv agv)
    {
        var dto = _mapper.Map<AgvDto>(agv);
        _hubBroadcaster.Broadcast(c => c.AgvDiscovered(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SendAgvPositionAsync(AgvPositionStatus agvPosition)
    {
        var dto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
        if (await _agvPositionFilter.ShouldActAsync(dto))
        {
            _hubBroadcaster.Broadcast(c => c.AgvPositionChanged(dto));
        }
    }

    /// <inheritdoc/>
    public Task SendAgvRouteChangedAsync(CurrentAgvRouteReadModel currentAgvRouteReadModel)
    {
        var dto = _mapper.Map<CurrentAgvRouteDto>(currentAgvRouteReadModel);
        _hubBroadcaster.Broadcast(c => c.AgvRouteChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAssignedActionGroupChangedAsync(Guid correlationId, Guid? assignedActionGroupCorrelationId)
    {
        var dto = new AssignedActionGroupDto(correlationId, assignedActionGroupCorrelationId);
        _hubBroadcaster.Broadcast(c => c.AssignedActionGroupChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SendAgvBatteryStatusAsync(AgvBatteryStatusChanged batteryStatusChanged)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(batteryStatusChanged.CorrelationId);
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        var dto = _mapper.Map<AgvBatteryStatusDto>((batteryStatusChanged.AgvBatteryStatus, agvSettings));
        if (await _agvBatteryStatusFilter.ShouldActAsync(dto))
        {
            _hubBroadcaster.Broadcast(c => c.BatteryStatusChanged(dto));
        }
    }

    /// <inheritdoc/>
    public Task SendAgvLockedChangedAsync(Guid correlationId, bool isLocked)
    {
        var dto = new AgvLockedStatusDto(correlationId, isLocked);
        _hubBroadcaster.Broadcast(c => c.AgvLockedChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvOnlineChangedAsync(Guid correlationId, bool isOnline)
    {
        var dto = new AgvOnlineStatusDto(correlationId, isOnline);
        _hubBroadcaster.Broadcast(c => c.AgvOnlineChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvErrorChangedAsync(Guid correlationId, List<AgvError> errors)
    {
        var dto = _mapper.Map<AgvErrorStatusDto>(errors);
        _mapper.Map(correlationId, dto);
        _hubBroadcaster.Broadcast(c => c.AgvErrorChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendCargoChangeAsync(IReadOnlyCollection<Cargo> cargos)
    {
        var dto = _mapper.Map<ICollection<CargoDto>>(cargos);
        _hubBroadcaster.Broadcast(c => c.CargoChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvHasCargoChangedAsync(Guid correlationId, bool hasCargo)
    {
        var dto = new AgvHasCargoDto(correlationId, hasCargo);
        _hubBroadcaster.Broadcast(c => c.AgvHasCargoChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvZonesChangedAsync(Guid correlationId, ICollection<string> zones)
    {
        var dto = new AgvZonesDto(correlationId, zones);
        _hubBroadcaster.Broadcast(c => c.AgvZonesChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvStationsChangedAsync(Guid correlationId, ICollection<string> stations)
    {
        var dto = new AgvStationsDto(correlationId, stations);
        _hubBroadcaster.Broadcast(c => c.AgvStationsChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvOperatingDataStatusAsync(AgvOperatingDataStatusChanged agvOperatingDataStatusChanged)
    {
        var dto = _mapper.Map<AgvOperatingDataStatusDto>(agvOperatingDataStatusChanged.AgvOperatingDataStatus);
        _hubBroadcaster.Broadcast(c => c.AgvOperatingDataStatusChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAgvMaintainedAsync(Guid correlationId, Maintenance maintenance)
    {
        var dto = new AgvMaintainedDto(correlationId, _mapper.Map<MaintenanceDto>(maintenance));
        _hubBroadcaster.Broadcast(c => c.AgvMaintained(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendMaintenanceLogCreated(Guid correlationId, MaintenanceLog maintenanceLog)
    {
        var dto = new MaintenanceLogCreatedDto(_mapper.Map<AgvMaintenanceLogDto>(maintenanceLog), correlationId);
        _hubBroadcaster.Broadcast(c => c.AgvMaintenanceLogCreated(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendLogEntryCreated(Guid correlationId, LogEntry logEntry)
    {
        var dto = new LogEntryCreatedDto(correlationId, _mapper.Map<LogEntryDto>(logEntry));
        _hubBroadcaster.Broadcast(c => c.AgvLogCreated(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendBatteryLogEntryCreated(Guid correlationId, LogEntry logEntry)
    {
        var dto = new LogEntryCreatedDto(correlationId, _mapper.Map<LogEntryDto>(logEntry));
        _hubBroadcaster.Broadcast(c => c.BatteryLogCreated(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendOperationModeChanged(Messages.MessageContent.Agv agv)
    {
        var dto = _mapper.Map<AgvDto>(agv);
        _hubBroadcaster.Broadcast(c => c.AgvOperationModeChanged(dto));
        return Task.CompletedTask;
    }
}