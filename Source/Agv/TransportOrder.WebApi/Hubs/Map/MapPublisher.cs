using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
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
    public Task SendNodeStateChangedAsync(NodeLock nodeState, Guid? stationId, string? modifyingUser, string? lockReason, string? lockDescription)
    {
        var dto = _mapper.Map<NodeStateDto>(nodeState);
        dto = dto with
        {
            StationId = stationId,
            User = modifyingUser,
            LockReason = lockReason,
            LockDescription = lockDescription
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
}