using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IRequestClient<GetMapStations> _getMapStationsClient;
    private readonly IRequestClient<TrySettingNodeApproachingProperties> _nodeApproachingPropertiesClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="MapController"/> instance.
    /// </summary>
    public MapController(IMapper mapper,
        IRequestClient<GetMapImage> getMapImageClient,
        IRequestClient<GetMap> getMapClient,
        IRequestClient<GetMapGraph> getMapGraphClient,
        IRequestClient<GetMapNodeStates> getMapNodeStatesClient,
        IRequestClient<GetMapStations> getMapStationsClient,
        IRequestClient<TrySettingNodeApproachingProperties> nodeApproachingPropertiesClient)
    {
        _getMapImageClient = getMapImageClient;
        _getMapClient = getMapClient;
        _getMapGraphClient = getMapGraphClient;
        _getMapNodeStatesClient = getMapNodeStatesClient;
        _getMapStationsClient = getMapStationsClient;
        _nodeApproachingPropertiesClient = nodeApproachingPropertiesClient;
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
    [Authorize]
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
    [Authorize]
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
    [Authorize]
    [HttpGet]
    [Route("/map/{correlationId:guid}/nodeStates")]
    public async Task<ActionResult<ICollection<NodeStateDto>>> GetNodeStatesForMap(Guid correlationId)
    {
        Response<GotMapNodeStates> response = await _getMapNodeStatesClient.GetResponse<GotMapNodeStates>(new GetMapNodeStates(correlationId));

        List<NodeStateDto> nodeStateDtos = response.Message.NodeStates.Select(x => {
            var dto = _mapper.Map<NodeStateDto>(x.NodeLock);
            dto = dto with
            {
                StationId = x.StationId,
                User = x.LockedByUser,
                LockReason = x.LockReason,
                LockDescription = x.LockDescription
            };

            return dto;
        })
        .ToList();

        return Ok(nodeStateDtos);
    }

    /// <summary>
    /// Get all stations for a given map.
    /// </summary>
    /// <param name="correlationId">The correlation id of the map.</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet]
    [Route("/map/{correlationId:guid}/stations")]
    public async Task<ActionResult<ICollection<StationDto>>> GetStationsForMap(Guid correlationId)
    {
        Response<GotMapStations> response = await _getMapStationsClient.GetResponse<GotMapStations>(new GetMapStations(correlationId));
        var stationsDto = _mapper.Map<List<StationDto>>(response.Message.Stations);
        return Ok(stationsDto);
    }

    /// <summary>
    /// Set approaching properties for a node.
    /// </summary>
    [Authorize]
    [HttpPatch]
    [Route("/map/{correlationId:guid}/nodes/{nodeId}/setApproachingProperties")]
    public async Task<ActionResult> SetApproachProperties(Guid correlationId, string nodeId, [FromQuery] double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection)
    {
        await _nodeApproachingPropertiesClient.GetResponse<Success>(new TrySettingNodeApproachingProperties(correlationId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection));
        return Ok();
    }
}