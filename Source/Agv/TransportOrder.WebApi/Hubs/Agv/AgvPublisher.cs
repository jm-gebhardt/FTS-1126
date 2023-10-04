using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.AspNetCore.SignalR;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.AgvPositionStatus;

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
        var dto = _mapper.Map<AgvDto>(agv);
        return isInitialState ? _hubContext.Clients.All.AgvDiscovered(dto) : _hubContext.Clients.All.AgvChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvPositionAsync(AgvPositionStatus agvPosition)
    {
        var dto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
        return _hubContext.Clients.All.AgvPositionChanged(dto);
    }

    /// <inheritdoc/>
    public Task SendAgvRouteAsync(AgvRouteReadModel agvRouteReadModel)
    {
        var dto = _mapper.Map<AgvRouteDto>(agvRouteReadModel);
        return _hubContext.Clients.All.AgvRouteChanged(dto);
    }
}