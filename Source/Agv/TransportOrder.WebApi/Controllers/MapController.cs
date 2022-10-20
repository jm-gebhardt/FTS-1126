using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Map Rest API Controller
    /// </summary>
    [ApiController]
    public class MapController : Controller
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapRegistry _mapRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="mapRegistry"></param>
        /// <param name="mapper"></param>
        public MapController(IAgvRegistry agvRegistry, IMapRegistry mapRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _mapRegistry = mapRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all available maps
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/maps")]
        public ActionResult<IEnumerable<Map>> GetMaps()
        {
            ImmutableDictionary<Guid, Guid[]> agvMapRelation = _agvRegistry.GetAllAgvs()
                .Where(a => a.AgvGraph != null)
                .Select(a => new { a.AgvGraph!.RelatedMapId, a.CorrelationId })
                .AsEnumerable()
                .GroupBy(a => a.RelatedMapId)
                .ToImmutableDictionary(
                    x => x.Key,
                    x => x.Select(m => m.CorrelationId).ToArray()
                );

            var map = _mapper.Map<List<Map>>(_mapRegistry.GetAllMaps().AsEnumerable(), opt => opt.Items[MappingProfile.AgvMapRelationsName] = agvMapRelation);

            return Ok(map);
        }

        /// <summary>
        /// Get Map Image
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/maps/{correlationId:guid}/map.png")]
        [Produces("image/png")]
        public ActionResult GetMap(Guid correlationId)
        {
            byte[]? image = _mapRegistry.GetMap(correlationId);
            if (image == null)
            {
                return NotFound();
            }
            return File(image, "image/png");
        }
    }
}
