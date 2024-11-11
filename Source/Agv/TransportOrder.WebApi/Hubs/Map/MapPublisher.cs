using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <inheritdoc cref="IMapPublisher"/>
public class MapPublisher : IMapPublisher
{
    private readonly IHubContext<MapHub, IMapClient> _hubContext;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="IMapPublisher"/>
    public MapPublisher(IHubContext<MapHub, IMapClient> hubContext, IMapper mapper)
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendNodeStateChangedAsync(NodeLock nodeState, Guid? stationId)
    {
        var nodeStateDto = _mapper.Map<NodeStateDto>(nodeState);
        nodeStateDto = nodeStateDto with { StationId = stationId };
        return _hubContext.Clients.All.NodeStateChanged(nodeStateDto);
    }
}