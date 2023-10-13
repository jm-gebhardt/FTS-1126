using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Map;

/// <summary>
/// Single map requests.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class MapController : Controller
{
    private readonly IRequestClient<GetMapImage> _getMapImageClient;
    private readonly IRequestClient<GetMap> _getMapClient;
    private readonly IRequestClient<GetMapGraph> _getMapGraphClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="MapController"/> instance.
    /// </summary>
    public MapController(IMapper mapper, 
        IRequestClient<GetMapImage> getMapImageClient, 
        IRequestClient<GetMap> getMapClient,
        IRequestClient<GetMapGraph> getMapGraphClient)
    {
        _getMapImageClient = getMapImageClient;
        _getMapClient = getMapClient;
        _getMapGraphClient = getMapGraphClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get map background image.
    /// </summary>
    /// <param name="correlationId">The map correlation id.</param>
    [HttpGet]
    [Route("/map/{correlationId:guid}/background.png")]
    [Produces("image/png")]
    public async Task<ActionResult> GetBackgroundImage(Guid correlationId)
    {
        Response<GotMapImage> response = await _getMapImageClient.GetResponse<GotMapImage>(new GetMapImage(correlationId));
        byte[] image = response.Message.Image;

        return File(image, "image/png");
    }

    /// <summary>
    /// Get map.
    /// </summary>
    /// <param name="correlationId">The map correlation id.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Map with given correlationId doesn't exist</response>
    [HttpGet]
    [Route("/map/{correlationId:guid}")]
    public async Task<ActionResult<MapDto>> GetMap(Guid correlationId)
    {
        Response<GotMap> response = await _getMapClient.GetResponse<GotMap>(new GetMap(correlationId));
        var map = _mapper.Map<MapDto>(response.Message.Map);
        return Ok(map);
    }

    /// <summary>
    /// Get map graph.
    /// </summary>
    /// <param name="correlationId">The map correlation id.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Map with given correlationId doesn't exist</response>
    [HttpGet]
    [Route("/map/{correlationId:guid}/graph")]
    public async Task<ActionResult<GraphDto>> GetMapGraph(Guid correlationId)
    {
        Response<GotMapGraph> response = await _getMapGraphClient.GetResponse<GotMapGraph>(new GetMapGraph(correlationId));
        var graph = _mapper.Map<GraphDto>(response.Message.Graph);
        return Ok(graph);
    }
}