using System.Collections.Immutable;
using AutoMapper;
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
public class GetMapListRequestHandler : IConsumer<GetMapList>
{
    private readonly IMapRegistry _mapRegistry;
    private readonly IMapper _mapper;
    private readonly IAgvGraphRegistry _agvGraphRegistry;

    /// <summary/>
    /// <param name="mapRegistry"></param>
    /// <param name="mapper"></param>
    /// <param name="agvGraphRegistry"></param>
    public GetMapListRequestHandler(IMapRegistry mapRegistry, IMapper mapper, IAgvGraphRegistry agvGraphRegistry)
    {
        _mapRegistry = mapRegistry;
        _mapper = mapper;
        _agvGraphRegistry = agvGraphRegistry;
    }

    /// <summary>
    /// Handles a GetMapList request
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Maps with an existing AgvGraph</returns>
    public async Task Consume(ConsumeContext<GetMapList> context)
    {
        ImmutableDictionary<Guid, Guid[]> agvMapRelation = (await _agvGraphRegistry.GetAllAgvGraphsAsync())
            .GroupBy(a => a.RelatedMapId)
            .ToImmutableDictionary(x => x.Key, x => x.Select(m => m.CorrelationId).ToArray());
             
        var maps = _mapper.Map<List<Map>>(_mapRegistry.GetAllMaps().AsEnumerable(), 
            opt => opt.Items[RelatedMapAgvValueResolver.AgvMapRelationsName] = agvMapRelation);

        await context.RespondAsync(new GotMapList { Maps = maps });
    }
}