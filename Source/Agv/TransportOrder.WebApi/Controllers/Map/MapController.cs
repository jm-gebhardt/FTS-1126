using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Map;

/// <summary>
/// Single map requests.
/// </summary>
[ApiController]
// [Authorize]
// TODO: FE can't handle
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class MapController : Controller
{
    private readonly IRequestClient<SetBackground> _setBackgroundClient;
    private readonly IRequestClient<GetBackgroundImage> _getBackgroundImageClient;
    private readonly IRequestClient<DeleteBackground> _deleteBackgroundClient;
    private readonly IRequestClient<GetMap> _getMapClient;
    private readonly IRequestClient<UpdateMap> _updateMapClient;
    private readonly IRequestClient<GetMapGraph> _getMapGraphClient;
    private readonly IRequestClient<GetMapNodeStates> _getMapNodeStatesClient;
    private readonly IRequestClient<GetMapStations> _getMapStationsClient;
    private readonly IRequestClient<TrySettingNodeApproachingProperties> _nodeApproachingPropertiesClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="MapController"/> instance.
    /// </summary>
    public MapController(IMapper mapper,
        IRequestClient<GetBackgroundImage> getBackgroundImageClient,
        IRequestClient<SetBackground> setBackgroundClient,
        IRequestClient<DeleteBackground> deleteBackgroundClient,
        IRequestClient<GetMap> getMapClient,
        IRequestClient<UpdateMap> updateMapClient,
        IRequestClient<GetMapGraph> getMapGraphClient,
        IRequestClient<GetMapNodeStates> getMapNodeStatesClient,
        IRequestClient<GetMapStations> getMapStationsClient,
        IRequestClient<TrySettingNodeApproachingProperties> nodeApproachingPropertiesClient)
    {
        _mapper = mapper;
        _getBackgroundImageClient = getBackgroundImageClient;
        _setBackgroundClient = setBackgroundClient;
        _deleteBackgroundClient = deleteBackgroundClient;
        _getMapClient = getMapClient;
        _updateMapClient = updateMapClient;
        _getMapGraphClient = getMapGraphClient;
        _getMapNodeStatesClient = getMapNodeStatesClient;
        _getMapStationsClient = getMapStationsClient;
        _nodeApproachingPropertiesClient = nodeApproachingPropertiesClient;
    }

    /// <summary>
    /// Retrieves the PNG bytes of the current background image for the map.
    /// </summary>
    /// <param name="correlationId">Correlation‑Id of the map.</param>
    /// <remarks>
    /// If no custom background is set, the handler will return the base default image.
    /// </remarks>
    /// <response code="200">PNG bytes returned.</response>
    /// <response code="404">Map or no background is available.</response>
    [HttpGet]
    [Route("/map/{correlationId:guid}/background.png")]
    [Produces("image/png")]
    public async Task<ActionResult> GetBackgroundImage(Guid correlationId)
    {
        Response<GotBackgroundImage> response = await _getBackgroundImageClient.GetResponse<GotBackgroundImage>(new GetBackgroundImage(correlationId));

        return File(response.Message.Image, "image/png");
    }

    /// <summary>
    /// Uploads (or replaces) a custom background image for the given map.
    /// </summary>
    /// <param name="correlationId">Correlation‑Id of the map.</param>
    /// <param name="dto">The background parameters and PNG image.</param>
    /// <response code="200">Background stored successfully.</response>
    /// <response code="404">Map with the given correlationId not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpPut]
    [Route("/map/{correlationId:guid}/background")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> SetBackground(Guid correlationId, [FromForm] SetBackgroundDto dto)
    {
        await using var ms = new MemoryStream();
        await dto.File.CopyToAsync(ms);

        byte[] imageBytes = ms.ToArray();
        var setBackground = _mapper.Map<SetBackground>(dto, opts =>
        {
            opts.Items[nameof(Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image.SetBackground.CorrelationId)] = correlationId;
            opts.Items[nameof(Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image.SetBackground.Image)] = imageBytes;
        });
        await _setBackgroundClient.GetResponse<Success>(setBackground);

        return Ok();
    }

    /// <summary>
    /// Deletes the custom background image for the map and reverts to the
    /// default background provided by the AGV.
    /// </summary>
    /// <param name="correlationId">Correlation‑Id of the map.</param>
    /// <response code="204">Custom Background deleted (or none existed).</response>
    /// <response code="404">Map with the given correlationId not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpDelete]
    [Route("/map/{correlationId:guid}/background")]
    public async Task<ActionResult> DeleteBackground(Guid correlationId)
    {
        await _deleteBackgroundClient.GetResponse<Success>(new DeleteBackground(correlationId));
        return NoContent();
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
    /// Update map properties of an existing map.
    /// </summary>
    /// <param name="correlationId">Correlation‑Id of the map.</param>
    /// <param name="dto">The new background parameters.</param>
    /// <response code="200">Background parameters stored successfully.</response>
    /// <response code="404">Map with the given correlationId not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpPatch]
    [Route("/map/{correlationId:guid}")]
    public async Task<ActionResult<MapDto>> PatchMap(Guid correlationId, UpdateMapDto dto)
    {
        var updateMap = _mapper.Map<UpdateMap>(dto, opts => { opts.Items[nameof(Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image.SetBackground.CorrelationId)] = correlationId; });

        await _updateMapClient.GetResponse<Success>(updateMap);
        return NoContent();
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

        List<NodeStateDto> nodeStateDtos = response.Message.NodeStates.Select(x =>
            {
                var dto = _mapper.Map<NodeStateDto>(x.NodeLock) with
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
    [Authorize(Policy = AuthorizationPolicies.NodePropertiesUpdate)]
    [HttpPatch]
    [Route("/map/{correlationId:guid}/nodes/setApproachingProperties")]
    public async Task<ActionResult> SetApproachProperties(Guid correlationId, [FromQuery] string nodeId, [FromQuery] double? pickLiftHeight, [FromQuery] double? dropLiftHeight, [FromQuery] DockingDirection? dockingDirection)
    {
        await _nodeApproachingPropertiesClient.GetResponse<Success>(new TrySettingNodeApproachingProperties(correlationId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection));
        return Ok();
    }
}