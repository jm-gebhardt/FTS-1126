using System.Collections.Immutable;
using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

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
    private readonly IAgvGraphRepository _agvGraphRepository;

    /// <summary/>
    /// <param name="mapRepository"></param>
    /// <param name="mapper"></param>
    /// <param name="agvGraphRepository"></param>
    public GetMapRequestHandler(IMapRepository mapRepository, IMapper mapper, IAgvGraphRepository agvGraphRepository)
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
        _agvGraphRepository = agvGraphRepository;
    }

    /// <summary>
    /// Handles a GetMapList request
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Maps with an existing AgvGraph</returns>
    public async Task Consume(ConsumeContext<GetMapList> context)
    {
        ImmutableDictionary<Guid, Guid[]> agvMapRelation = (await _agvGraphRepository.GetAllAgvGraphsAsync())
            .GroupBy(a => a.RelatedMapId)
            .ToImmutableDictionary(x => x.Key, x => x.Select(m => m.CorrelationId).ToArray());

        var maps = _mapper.Map<List<Map>>(_mapRepository.GetAllMaps().AsEnumerable(),
            opt => opt.Items[RelatedMapAgvValueResolver.AgvMapRelationsName] = agvMapRelation);

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
        ICollection<Guid> relatedAgvs = await _agvGraphRepository.GetAgvsRelatedToMap(fetchedMap.CorrelationId);
        var map = new Map(fetchedMap.CorrelationId, fetchedMap.MapId, fetchedMap.Resolution, fetchedMap.XOffset, fetchedMap.YOffset, relatedAgvs.ToArray(), fetchedMap.Width, fetchedMap.Height);
        await context.RespondAsync(new GotMap(map));
    }
}