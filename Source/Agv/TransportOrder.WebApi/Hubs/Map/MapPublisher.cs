using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;

/// <inheritdoc cref="IMapPublisher"/>
public class MapPublisher : IMapPublisher
{
    private readonly IHubContext<MapHub, IMapClient> _hubContext;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="IMapPublisher"/>
    public MapPublisher(IHubContext<MapHub, IMapClient> hubContext, IMapper mapper)
    {
        _hubContext=hubContext;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public Task SendNodeStateChangedAsync(NodeLock nodeState)
    {
       var nodeStateDto = _mapper.Map<NodeStateDto>(nodeState);
       return _hubContext.Clients.All.NodeStateChanged(nodeStateDto);
    }
}