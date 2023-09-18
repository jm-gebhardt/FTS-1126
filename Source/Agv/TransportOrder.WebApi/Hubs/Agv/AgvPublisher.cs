using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
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
    public Task SendAgvStateAsync(Messages.MessageContent.Agv agv, bool isInitialState)
    {
        var agvDto = _mapper.Map<AgvDto>(agv);
        return isInitialState ? _hubContext.Clients.All.AgvDiscovered(agvDto) : _hubContext.Clients.All.AgvChanged(agvDto);
    }

    /// <inheritdoc/>
    public Task SendAgvPositionAsync(AgvPositionStatus agvPosition)
    {
        var agvPositionDto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
        return _hubContext.Clients.All.AgvPositionChanged(agvPositionDto);
    }
}