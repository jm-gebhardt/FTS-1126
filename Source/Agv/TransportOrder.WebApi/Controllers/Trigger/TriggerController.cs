using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.WebApi;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;

/// <summary>
/// Trigger requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class TriggerController : Controller
{
    private readonly IRequestClient<GetTrigger> _getTriggerRequestClient;
    private readonly IRequestClient<AssignTriggerToLocation> _assignTriggerLocationRequestClient;
    private readonly IRequestClient<TryRemoveTriggerLocation> _removeTriggerLocationRequestClient;
    private readonly IRequestClient<TryUpdateDefaultConfigurationParameter> _updateDefaultParameterRequestClient;
    private readonly IRequestClient<TryUpdateLocationConfigurationParameter> _updateLocationParameterRequestClient;
    private readonly IRequestClient<TryResetLocationConfigurationParameter> _resetLocationParameterRequestClient;
    private readonly IRequestClient<TryMarkTriggerForDeletion> _markTriggerForDeletionRequestClient;
    private readonly IRequestClient<TryRestoreTrigger> _tryRestoreTriggerRequestClient;
    private readonly IMapper _mapper;
    private readonly IRequestClient<DuplicateTriggerLocations> _duplicateLocationsRequestClient;
    private readonly IRequestClient<DuplicateTriggerLocation> _duplicateLocationRequestClient;
    private readonly ILogger<TriggerController> _logger;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="TriggerController"/>>
    public TriggerController(
        IRequestClient<GetTrigger> getTriggerRequestClient,
        IRequestClient<AssignTriggerToLocation> assignTriggerLocationRequestClient,
        IRequestClient<TryRemoveTriggerLocation> removeTriggerLocationRequestClient,
        IRequestClient<TryUpdateDefaultConfigurationParameter> updateDefaultParameterRequestClient,
        IRequestClient<TryUpdateLocationConfigurationParameter> updateLocationParameterRequestClient,
        IRequestClient<TryResetLocationConfigurationParameter> resetLocationParameterRequestClient,
        IRequestClient<TryMarkTriggerForDeletion> markTriggerForDeletionRequestClient,
        IRequestClient<TryRestoreTrigger> tryRestoreTriggerRequestClient,
        IMapper mapper,
        ILogger<TriggerController> logger,
        IRequestClient<DuplicateTriggerLocations> duplicateLocationsRequestClient,
        IRequestClient<DuplicateTriggerLocation> duplicateLocationRequestClient,
        ILoggingService loggingService)
    {
        _getTriggerRequestClient = getTriggerRequestClient;
        _assignTriggerLocationRequestClient = assignTriggerLocationRequestClient;
        _removeTriggerLocationRequestClient = removeTriggerLocationRequestClient;
        _updateDefaultParameterRequestClient = updateDefaultParameterRequestClient;
        _updateLocationParameterRequestClient = updateLocationParameterRequestClient;
        _resetLocationParameterRequestClient = resetLocationParameterRequestClient;
        _markTriggerForDeletionRequestClient = markTriggerForDeletionRequestClient;
        _tryRestoreTriggerRequestClient = tryRestoreTriggerRequestClient;
        _duplicateLocationRequestClient = duplicateLocationRequestClient;
        _duplicateLocationsRequestClient = duplicateLocationsRequestClient;
        _mapper = mapper;
        _logger = logger;
        _loggingService = loggingService;
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
        var response = await _getTriggerRequestClient.GetResponse<GotTrigger>(new GetTrigger(id));
        var trigger = _mapper.Map<TriggerDto>(response.Message.Trigger);
        return Ok(trigger);
    }

    /// <summary>
    /// Delete a trigger.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger with given id not found.</response>
    [HttpDelete]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}")]
    public async Task<ActionResult> DeleteTrigger([FromRoute] Guid id)
    {
        await _markTriggerForDeletionRequestClient.GetResponse<Success>(new TryMarkTriggerForDeletion(id));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerMarkedForDeletion } });
        return Ok();
    }

    /// <summary>
    /// Restore a previously deleted trigger.
    /// </summary>
    /// <param name="triggerId">The trigger id to restore.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Trigger found but is not deleted at all</response>
    /// <response code="404">Trigger not found</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/triggers/{triggerId}/restore")]
    public async Task<ActionResult> RestoreTrigger(Guid triggerId)
    {
        await _tryRestoreTriggerRequestClient.GetResponse<Success>(new TryRestoreTrigger(triggerId));
        return Ok();
    }

    /// <summary>
    /// Assign a trigger to a location.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <param name="location">The location of the trigger.</param>
    /// <returns>The new location.</returns>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger with given id not found.</response>
    /// <response code="400">More than one location set.</response>
    [HttpPatch]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location")]
    public async Task<ActionResult<TriggerLocationDto>> AssignTriggerToLocation([FromRoute] Guid id, [FromBody] LocationDto location)
    {
        var polygonPoints = location.Polygon != null ? _mapper.Map<ICollection<PolygonPoint>>(location.Polygon) : [];
        var parameters = location.ConfigurationParameters != null ? _mapper.Map<ICollection<ConfigurationParameter>>(location.ConfigurationParameters) : null;
        var response = await _assignTriggerLocationRequestClient.GetResponse<TriggerLocationAssigned>(new AssignTriggerToLocation(id, location.NodeId?.ToGraphNode(), location.EdgeId, polygonPoints, location.MapId, parameters));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerAssignedToLocation } });

        var triggerLocationDto = _mapper.Map<TriggerLocationDto>(response.Message);
        return Ok(triggerLocationDto);
    }

    /// <summary>
    /// Remove a trigger location.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <param name="locationId">The id of the location.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger or location with given id not found.</response>
    [HttpDelete]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location/{locationId:guid}")]
    public async Task<ActionResult> RemoveTriggerLocation([FromRoute] Guid id, [FromRoute] Guid locationId)
    {
        await _removeTriggerLocationRequestClient.GetResponse<Success>(new TryRemoveTriggerLocation(id, locationId));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerLocationRemoved } });
        return Ok();
    }

    /// <summary>
    /// Duplicate a trigger location from a source trigger to the current trigger.
    /// </summary>
    /// <param name="id">The id of the trigger the location is to be copied to.</param>
    /// <param name="locationId">The id of the location.</param>
    /// <param name="sourceTriggerId">The id of the trigger the location is copied from.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger or location with given id not found.</response>
    [HttpPatch]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location/{locationId:guid}/duplicateFrom/{sourceTriggerId:guid}")]
    public async Task<ActionResult> DuplicateTriggerLocation([FromRoute] Guid id, [FromRoute] Guid locationId, [FromRoute] Guid sourceTriggerId)
    {
        await _duplicateLocationRequestClient.GetResponse<Success>(new DuplicateTriggerLocation(sourceTriggerId, id, locationId));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerAssignedToLocation } });
        return Ok();
    }

    /// <summary>
    /// Duplicate all trigger location from a source trigger to the current trigger.
    /// </summary>
    /// <param name="id">The id of the trigger the location is to be copied to.</param>
    /// <param name="sourceTriggerId">The id of the trigger the location is copied from.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Trigger or location with given id not found.</response>
    [HttpPatch]
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location/duplicateFrom/{sourceTriggerId:guid}")]
    public async Task<ActionResult> DuplicateTriggerLocations([FromRoute] Guid id, [FromRoute] Guid sourceTriggerId)
    {
        await _duplicateLocationsRequestClient.GetResponse<Success>(new DuplicateTriggerLocations(sourceTriggerId, id));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerAssignedToLocation } });
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
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location/{locationId:guid}/parameter/{name}")]
    public async Task<ActionResult> UpdateLocationConfigurationParameter([FromRoute] Guid id, [FromRoute] Guid locationId, [FromRoute] string name, [FromQuery] [Required] string value)
    {
        await _updateLocationParameterRequestClient.GetResponse<Success>(new TryUpdateLocationConfigurationParameter(id, locationId, name, value));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerLocationConfigurationUpdated } });
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
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/location/{locationId:guid}/parameter/{name}/reset")]
    public async Task<ActionResult> ResetLocationConfigurationParameter([FromRoute] Guid id, [FromRoute] Guid locationId, [FromRoute] string name)
    {
        await _resetLocationParameterRequestClient.GetResponse<Success>(new TryResetLocationConfigurationParameter(id, locationId, name));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerLocationConfigurationReset } });
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
    [Authorize(Policy = AuthorizationPolicies.TriggersFullAccess)]
    [Route("/trigger/{id:guid}/parameter/{name}")]
    public async Task<ActionResult> UpdateDefaultConfigurationParameter([FromRoute] Guid id, [FromRoute] string name, [FromQuery] string value)
    {
        await _updateDefaultParameterRequestClient.GetResponse<Success>(new TryUpdateDefaultConfigurationParameter(id, name, value));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemTriggerDefaultConfigurationUpdated } });
        return Ok();
    }
}
