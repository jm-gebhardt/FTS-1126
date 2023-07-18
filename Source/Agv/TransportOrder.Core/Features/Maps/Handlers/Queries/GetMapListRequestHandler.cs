using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using MassTransit;
using System.Collections.Immutable;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using JetBrains.Annotations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Resolvers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetMapList request
    /// </summary>
    [UsedImplicitly]
    public class GetMapListRequestHandler : IConsumer<GetMapListRequest>
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
        public async Task Consume(ConsumeContext<GetMapListRequest> context)
        {
            ImmutableDictionary<Guid, Guid[]> agvMapRelation = (await _agvGraphRegistry.GetAllAgvGraphsAsync())
                .GroupBy(a => a.RelatedMapId)
                .ToImmutableDictionary(x => x.Key, x => x.Select(m => m.CorrelationId).ToArray());
             
            var maps = _mapper.Map<List<MapDto>>(_mapRegistry.GetAllMaps().AsEnumerable(), opt => opt.Items[RelatedMapAgvValueResolver.AgvMapRelationsName] = agvMapRelation);

            await context.RespondAsync(new GetMapListResult { Maps = maps });
        }
    }
}