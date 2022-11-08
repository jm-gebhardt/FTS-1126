using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Map Rest API Controller
    /// </summary>
    [ApiController]
    public class MapController : Controller
    {

        private readonly IMediator _mediator;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mediator"></param>
        public MapController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all available maps
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/maps")]
        public async Task<ActionResult<IEnumerable<MapDto>>> GetMaps()
        {
            List<MapDto> maps = await _mediator.Send(new GetMapListRequest());

            return Ok(maps);
        }

        /// <summary>
        /// Get Map Image
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/maps/{correlationId:guid}/map.png")]
        [Produces("image/png")]
        public async Task<ActionResult> GetMap(Guid correlationId)
        {
            byte[] image = await _mediator.Send(new GetMapImageRequest(correlationId));

            return File(image, "image/png");
        }
    }
}
