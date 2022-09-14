using System.Collections.Immutable;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers
{
    /// <summary>
    /// Map Rest API Controller
    /// </summary>
    [ApiController]
    public class MapController : Controller
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapRegistry _mapRegistry;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="mapRegistry"></param>
        public MapController(IAgvRegistry agvRegistry, IMapRegistry mapRegistry)
        {
            _agvRegistry = agvRegistry;
            _mapRegistry = mapRegistry;
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
                .Select(a => new {a.AgvGraph!.RelatedMapId, a.CorrelationId})
                .AsEnumerable()
                .GroupBy(a => a.RelatedMapId)
                .ToImmutableDictionary(
                    x => x.Key, 
                    x => x.Select(m => m.CorrelationId).ToArray()
                );
            
            return Ok(_mapRegistry.GetAllMaps().Select(
                m => new Map(
                    m.CorrelationId,
                    m.MapId,
                    m.Resolution,
                    m.XOffset,
                    m.YOffset, 
                    agvMapRelation.GetValueOrDefault(m.CorrelationId) ?? Array.Empty<Guid>()
                )).AsEnumerable());
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