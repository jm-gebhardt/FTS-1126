using System.Collections.Immutable;
using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using MediatR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetMapList request
    /// </summary>
    public class GetMapListRequestHandler : IRequestHandler<GetMapListRequest, List<MapDto>>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapRegistry _mapRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetMapListRequestHandler"/>
        /// </summary>
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
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Maps with an existing AgvGraph</returns>
        public Task<List<MapDto>> Handle(GetMapListRequest request, CancellationToken cancellationToken)
        {
            ImmutableDictionary<Guid, Guid[]> agvMapRelation = _agvRegistry.GetAllAgvs()
                .Where(a => a.AgvGraph != null)
                .Select(a => new { a.AgvGraph!.RelatedMapId, a.CorrelationId })
                .AsEnumerable()
                .GroupBy(a => a.RelatedMapId)
                .ToImmutableDictionary(x => x.Key, x => x.Select(m => m.CorrelationId).ToArray()
            );

            var maps = _mapper.Map<List<MapDto>>(_mapRegistry.GetAllMaps().AsEnumerable(), opt => opt.Items[MappingProfile.AgvMapRelationsName] = agvMapRelation);

            return Task.FromResult(maps);
        }
    }
}
