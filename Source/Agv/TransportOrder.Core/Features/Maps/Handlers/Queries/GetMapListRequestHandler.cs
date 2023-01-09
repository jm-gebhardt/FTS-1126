using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using MassTransit;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetMapList request
    /// </summary>
    [UsedImplicitly]
    public class GetMapListRequestHandler : IConsumer<GetMapListRequest>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapRegistry _mapRegistry;
        private readonly IMapper _mapper;

        /// <summary/>
        /// <param name="agvRegistry"></param>
        /// <param name="mapRegistry"></param>
        /// <param name="mapper"></param>
        public GetMapListRequestHandler(IAgvRegistry agvRegistry, IMapRegistry mapRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _mapRegistry = mapRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// Handles a GetMapList request
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Maps with an existing AgvGraph</returns>
        public async Task Consume(ConsumeContext<GetMapListRequest> context)
        {
            ImmutableDictionary<Guid, Guid[]> agvMapRelation = _agvRegistry.GetAllAgvs()
                .Where(a => a.AgvGraph != null)
                .Select(a => new { a.AgvGraph!.RelatedMapId, a.CorrelationId })
                .AsEnumerable()
                .GroupBy(a => a.RelatedMapId)
                .ToImmutableDictionary(x => x.Key, x => x.Select(m => m.CorrelationId).ToArray()
                );

            var maps = _mapper.Map<List<MapDto>>(_mapRegistry.GetAllMaps().AsEnumerable(), opt => opt.Items[MappingProfile.AgvMapRelationsName] = agvMapRelation);

            await context.RespondAsync(new GetMapListResult { Maps = maps });
        }
    }
}
