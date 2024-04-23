using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the GetMapList request
/// </summary>
[UsedImplicitly]
public class GetMapRequestHandler :
    IConsumer<GetMapList>,
    IConsumer<GetMap>
{
    private readonly IMapRepository _mapRepository;
    private readonly IMapper _mapper;

    /// <summary/>
    /// <param name="mapRepository"></param>
    /// <param name="mapper"></param>
    public GetMapRequestHandler(IMapRepository mapRepository, IMapper mapper)
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles a GetMapList request
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Maps with an existing AgvGraph</returns>
    public async Task Consume(ConsumeContext<GetMapList> context)
    {
        ICollection<Map> fetchedMaps = await _mapRepository.GetAllMapsAsync();
        // reversing the list is a quick fix that the global map is on that. This needs improvement in the future.
        var maps = _mapper.Map<List<Messages.Requests.Map.Map>>(fetchedMaps.Reverse());
        await context.RespondAsync(new GotMapList { Maps = maps });
    }

    /// <summary>
    /// Handles the <see cref="GetMap"/> request.
    /// </summary>
    /// <exception cref="MapNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetMap> context)
    {
        var correlationId = context.Message.CorrelationId;
        var fetchedMap = await _mapRepository.GetMapAsync(correlationId);
        var map = _mapper.Map<Messages.Requests.Map.Map>(fetchedMap);
        await context.RespondAsync(new GotMap(map));
    }
}