using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;

/// <summary>
/// Trigger requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class TriggerController: Controller
{
    private readonly IRequestClient<GetTrigger> _getTriggerRequestClient;
    private readonly IRequestClient<AssignTriggerToLocation> _assignTriggerLocationRequestClient;
    private readonly IRequestClient<TryRemoveTriggerLocation> _removeTriggerLocationRequestClient;
    private readonly IMapper _mapper;
    private readonly IRequestClient<TryUpdateConfigurationParameter> _updateParameterRequestClient;

    /// <inheritdoc cref="TriggerController"/>>
    public TriggerController(IMapper mapper, 
        IRequestClient<GetTrigger> getTriggerRequestClient, 
        IRequestClient<AssignTriggerToLocation> assignTriggerLocationRequestClient,
        IRequestClient<TryRemoveTriggerLocation> removeTriggerLocationRequestClient,
        IRequestClient<TryUpdateConfigurationParameter> updateParameterRequestClient)
    {
        _getTriggerRequestClient = getTriggerRequestClient;
        _assignTriggerLocationRequestClient = assignTriggerLocationRequestClient;
        _removeTriggerLocationRequestClient = removeTriggerLocationRequestClient;
        _updateParameterRequestClient = updateParameterRequestClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get a trigger.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger with given id not found.</response>
    [HttpGet]
    [Route("/trigger/{id:guid}")]
    public async Task<ActionResult<TriggerDto>> GetTrigger([FromRoute] Guid id)
    {
        Response<GotTrigger> response = await _getTriggerRequestClient.GetResponse<GotTrigger>(new GetTrigger(id));
        var trigger = _mapper.Map<TriggerDto>(response.Message.Trigger);
        return Ok(trigger);
    }

    /// <summary>
    /// Assign a trigger to a location.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <param name="mapId">The id of the map the location is on.</param>
    /// <param name="nodeId">The node id, if the trigger is assigned to a node.</param>
    /// <param name="edgeId">The edge id, if the trigger is assigned to an edge.</param>
    /// <param name="polygon">A polygon, if the trigger is assigned to a polygon.</param>
    /// <returns>The new location.</returns>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger with given id not found.</response>
    /// <response code="400">More than one location set.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/location")]
    public async Task<ActionResult<TriggerLocationDto>> AssignTriggerToLocation([FromRoute] Guid id, [FromQuery] Guid? mapId, [FromQuery] string? nodeId, [FromQuery] Guid? edgeId, [FromBody] ICollection<PolygonPointDto>? polygon)
    {
        ICollection<PolygonPoint> polygonPoints = polygon != null ? _mapper.Map<ICollection<PolygonPoint>>(polygon) : [];
        Response<TriggerLocationAssigned> response = await _assignTriggerLocationRequestClient.GetResponse<TriggerLocationAssigned>(new AssignTriggerToLocation(id, edgeId, nodeId,  polygonPoints, mapId));
        var location = _mapper.Map<TriggerLocationDto>(response.Message.Location);
        return Ok(location);
    }

    /// <summary>
    /// Remove a trigger location.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <param name="locationId">The id of the location.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger or location with given id not found.</response>
    [HttpDelete]
    [Route("/trigger/{id:guid}/location/{locationId:guid}")]
    public async Task<ActionResult> RemoveTriggerLocation([FromRoute] Guid id, [FromRoute] Guid locationId)
    {
        await _removeTriggerLocationRequestClient.GetResponse<Success>(new TryRemoveTriggerLocation(id, locationId));
        return Ok();
    }

    /// <summary>
    /// Update configuration parameter of a trigger.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The new value.</param>
    /// <response code="404">Trigger with given id not found.</response>
    /// <response code="400">Parameter unknown or value does not pass validation.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/parameter/{name}")]
    public async Task<ActionResult> UpdateConfigurationParameter([FromRoute] Guid id, [FromRoute] string name, [FromQuery] string value)
    {
        await _updateParameterRequestClient.GetResponse<Success>(new TryUpdateConfigurationParameter(id, name, value));
        return Ok();
    }
}