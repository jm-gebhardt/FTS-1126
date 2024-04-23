using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Map;

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
    private readonly IRequestClient<GetMapNodeStates> _getMapNodeStatesClient;
    private readonly IRequestClient<GetStations> _getStationsClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="MapController"/> instance.
    /// </summary>
    public MapController(IMapper mapper,
        IRequestClient<GetMapImage> getMapImageClient,
        IRequestClient<GetMap> getMapClient,
        IRequestClient<GetMapGraph> getMapGraphClient,
        IRequestClient<GetMapNodeStates> getMapNodeStatesClient,
        IRequestClient<GetStations> getStationsClient)
    {
        _getMapImageClient = getMapImageClient;
        _getMapClient = getMapClient;
        _getMapGraphClient = getMapGraphClient;
        _getMapNodeStatesClient = getMapNodeStatesClient;
        _getStationsClient = getStationsClient;
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
    /// <response code="404">Map with given correlationId not found.</response>
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
    /// <response code="404">Map with given correlationId not found.</response>
    [HttpGet]
    [Route("/map/{correlationId:guid}/graph")]
    public async Task<ActionResult<GraphDto>> GetMapGraph(Guid correlationId)
    {
        Response<GotMapGraph> response = await _getMapGraphClient.GetResponse<GotMapGraph>(new GetMapGraph(correlationId));
        var graph = _mapper.Map<GraphDto>(response.Message.Graph);
        return Ok(graph);
    }

    /// <summary>
    /// Get all node states for a given map.
    /// </summary>
    /// <param name="correlationId">The map correlation id.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Map with given correlationId not found.</response>
    [HttpGet]
    [Route("/map/{correlationId:guid}/nodeStates")]
    public async Task<ActionResult<ICollection<NodeStateDto>>> GetNodeStatesForMap(Guid correlationId)
    {
        Response<GotMapNodeStates> response = await _getMapNodeStatesClient.GetResponse<GotMapNodeStates>(new GetMapNodeStates(correlationId));
        var nodeStates = _mapper.Map<List<NodeStateDto>>(response.Message.NodeLocks);
        return Ok(nodeStates);
    }

    /// <summary>
    /// Get all stations for a given map.
    /// </summary>
    /// <param name="correlationId">The correlation id of the map.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("/map/{correlationId:guid}/stations")]
    public async Task<ActionResult<ICollection<StationDto>>> GetStationsForMap(Guid correlationId)
    {
        Response<GotStations> response = await _getStationsClient.GetResponse<GotStations>(new GetStations(correlationId));
        var stationsDto = _mapper.Map<List<StationDto>>(response.Message.Stations);
        return Ok(stationsDto);
    }
}