using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
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
    private readonly IRequestClient<TryUpdateDefaultConfigurationParameter> _updateDefaultParameterRequestClient;
    private readonly IRequestClient<TryUpdateLocationConfigurationParameter> _updateLocationParameterRequestClient;
    private readonly IRequestClient<TryResetLocationConfigurationParameter> _resetLocationParameterRequestClient;

    /// <inheritdoc cref="TriggerController"/>>
    public TriggerController(IMapper mapper, 
        IRequestClient<GetTrigger> getTriggerRequestClient, 
        IRequestClient<AssignTriggerToLocation> assignTriggerLocationRequestClient,
        IRequestClient<TryRemoveTriggerLocation> removeTriggerLocationRequestClient,
        IRequestClient<TryUpdateDefaultConfigurationParameter> updateDefaultParameterRequestClient,
        IRequestClient<TryUpdateLocationConfigurationParameter> updateLocationParameterRequestClient,
        IRequestClient<TryResetLocationConfigurationParameter> resetLocationParameterRequestClient)
    {
        _getTriggerRequestClient = getTriggerRequestClient;
        _assignTriggerLocationRequestClient = assignTriggerLocationRequestClient;
        _removeTriggerLocationRequestClient = removeTriggerLocationRequestClient;
        _updateDefaultParameterRequestClient = updateDefaultParameterRequestClient;
        _updateLocationParameterRequestClient = updateLocationParameterRequestClient;
        _resetLocationParameterRequestClient = resetLocationParameterRequestClient;
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
    /// <param name="nodeId">The node id, if the trigger is assigned to a node.</param>
    /// <param name="location">The location of the trigger.</param>
    /// <returns>The new location.</returns>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger with given id not found.</response>
    /// <response code="400">More than one location set.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/location")]
    public async Task<ActionResult<TriggerLocationDto>> AssignTriggerToLocation([FromRoute] Guid id, [FromQuery] string? nodeId, [FromBody] TriggerLocationDto? location)
    {
        ICollection<PolygonPoint> polygonPoints = location?.Polygon != null ? _mapper.Map<ICollection<PolygonPoint>>(location.Polygon) : [];
        ICollection<ConfigurationParameter>? parameters = location?.ConfigurationParameters != null ? _mapper.Map<ICollection<ConfigurationParameter>>(location.ConfigurationParameters) : null;
        string? node = location?.NodeId ?? nodeId;
        Response<TriggerLocationAssigned> response = await _assignTriggerLocationRequestClient.GetResponse<TriggerLocationAssigned>(new AssignTriggerToLocation(id, node, location?.EdgeId, polygonPoints, location?.MapId, parameters));
        return Ok(_mapper.Map<TriggerLocationDto>(response.Message.Location));
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
    /// Update a configuration parameter of a trigger at a location.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="locationId">The location id.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The new value.</param>
    /// <response code="404">Trigger with given id not found or location not found.</response>
    /// <response code="400">Parameter unknown or value does not pass validation.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/location/{locationId:guid}/parameter/{name}")]
    public async Task<ActionResult> UpdateLocationConfigurationParameter([FromRoute] Guid id, [FromRoute] Guid locationId, [FromRoute] string name, [FromQuery] [Required] string value)
    {
        await _updateLocationParameterRequestClient.GetResponse<Success>(new TryUpdateLocationConfigurationParameter(id, locationId, name, value));
        return Ok();
    }

    /// <summary>
    /// Reset a configuration parameter of a trigger at a location to the default value.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="locationId">The location id.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <response code="404">Trigger with given id not found or location not found.</response>
    /// <response code="400">Parameter unknown or value does not pass validation.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/location/{locationId:guid}/parameter/{name}/reset")]
    public async Task<ActionResult> ResetLocationConfigurationParameter([FromRoute] Guid id, [FromRoute] Guid locationId, [FromRoute] string name)
    {
        await _resetLocationParameterRequestClient.GetResponse<Success>(new TryResetLocationConfigurationParameter(id, locationId, name));
        return Ok();
    }

    /// <summary>
    /// Update a default configuration parameter of a trigger.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The new value.</param>
    /// <response code="404">Trigger with given id not found.</response>
    /// <response code="400">Parameter unknown or value does not pass validation.</response>
    [HttpPatch]
    [Route("/trigger/{id:guid}/parameter/{name}")]
    public async Task<ActionResult> UpdateDefaultConfigurationParameter([FromRoute] Guid id, [FromRoute] string name, [FromQuery] string value)
    {
        await _updateDefaultParameterRequestClient.GetResponse<Success>(new TryUpdateDefaultConfigurationParameter(id, name, value));
        return Ok();
    }
}