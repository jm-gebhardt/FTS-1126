using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <inheritdoc cref="IMapPublisher"/>
public class MapPublisher : IMapPublisher
{
    private readonly IHubBroadcaster<MapHub, IMapClient> _hubBroadcaster;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="IMapPublisher"/>
    public MapPublisher(
        IHubBroadcaster<MapHub, IMapClient> hubBroadcaster,
        IMapper mapper)
    {
        _hubBroadcaster = hubBroadcaster;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendNodeStateChangedAsync(NodeLock nodeState, string nodeName, Guid? stationId, string? modifyingUser, string? lockReason, string? lockDescription)
    {
        var dto = _mapper.Map<NodeStateDto>(nodeState);
        dto = dto with
        {
            StationId = stationId,
            User = modifyingUser,
            LockReason = lockReason,
            LockDescription = lockDescription,
            NodeId = nodeName
        };

        _hubBroadcaster.Broadcast(c => c.NodeStateChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendNodeApproachingPropertiesUpdated(Guid mapId, string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection)
    {
        var dto = new NodeApproachingPropertiesDto(mapId, nodeId, dropLiftHeight, pickLiftHeight, dockingDirection);
        _hubBroadcaster.Broadcast(c => c.NodeApproachingPropertiesChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendJamStateUpdate(Guid stationId, bool hasPalletJam)
    {
        var dto = new StationJamStateDto(stationId, hasPalletJam);
        _hubBroadcaster.Broadcast(c => c.StationJamStateChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendMapChangedAsync(Messages.Requests.Map.Map map, bool hasBackgroundChanged)
    {
        var dto = _mapper.Map<MapChangedDto>((map, hasBackgroundChanged));
        _hubBroadcaster.Broadcast(c => c.MapChanged(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendLogEntryCreated(Guid correlationId, LogEntry logEntry)
    {
        var dto = new LogEntryCreatedDto(correlationId, _mapper.Map<LogEntryDto>(logEntry));
        _hubBroadcaster.Broadcast(c => c.StationLogCreated(dto));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendStationIsOnlineChanged(Guid stationId, bool isOnline)
    {
        var dto = new StationIsOnlineChangedDto(stationId, isOnline);
        _hubBroadcaster.Broadcast(c => c.StationIsOnlineChanged(dto));
        return Task.CompletedTask;
    }
}