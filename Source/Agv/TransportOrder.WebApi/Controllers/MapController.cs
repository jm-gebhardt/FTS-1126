using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;

/// <summary>
/// Map Rest API Controller
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class MapController : Controller
{
    private readonly IRequestClient<GetMapList> _getMapListClient;
    private readonly IRequestClient<GetMapImage> _getMapImageClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mapper">AutoMapper.</param>
    /// <param name="getMapListClient"></param>
    /// <param name="getMapImageClient"></param>
    public MapController(IMapper mapper, IRequestClient<GetMapList> getMapListClient, IRequestClient<GetMapImage> getMapImageClient)
    {
        _getMapListClient = getMapListClient;
        _getMapImageClient = getMapImageClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all available maps
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/maps")]
    public async Task<ActionResult<IEnumerable<MapDto>>> GetMaps()
    {
        Response<GotMapList> response = await _getMapListClient.GetResponse<GotMapList>(new GetMapList());
        var maps = _mapper.Map<List<MapDto>>(response.Message.Maps);

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
        Response<GotMapImage> response = await _getMapImageClient.GetResponse<GotMapImage>(new GetMapImage(correlationId));
        byte[] image = response.Message.Image;

        return File(image, "image/png");
    }
}