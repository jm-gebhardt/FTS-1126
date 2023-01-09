using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Map Rest API Controller
    /// </summary>
    [ApiController]
    public class MapController : Controller
    {
        private readonly IRequestClient<GetMapListRequest> _getMapListClient;
        private readonly IRequestClient<GetMapImageRequest> _getMapImageClient;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="getMapListClient"></param>
        /// <param name="getMapImageClient"></param>
        public MapController(IRequestClient<GetMapListRequest> getMapListClient, IRequestClient<GetMapImageRequest> getMapImageClient)
        {
            _getMapListClient = getMapListClient;
            _getMapImageClient = getMapImageClient;
        }

        /// <summary>
        /// Get all available maps
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/maps")]
        public async Task<ActionResult<IEnumerable<MapDto>>> GetMaps()
        {
            Response<GetMapListResult> response = await _getMapListClient.GetResponse<GetMapListResult>(new GetMapListRequest());
            List<MapDto> maps = response.Message.Maps;

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
            Response<GetMapImageResult> response = await _getMapImageClient.GetResponse<GetMapImageResult>(new GetMapImageRequest(correlationId));
            byte[] image = response.Message.Image;

            return File(image, "image/png");
        }
    }
}
