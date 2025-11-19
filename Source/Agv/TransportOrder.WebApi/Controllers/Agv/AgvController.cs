using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.Logging;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using RequestAgvZones = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvZones;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
using StartInstantAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.StartInstantAction;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Agv;

/// <summary>
/// Single agv requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvController : Controller
{
    private readonly IRequestClient<GetAgv> _getAgvClient;
    private readonly IRequestClient<GetAgvGraph> _getAgvGraphClient;
    private readonly IRequestClient<GetAgvData> _getAgvDataClient;
    private readonly IRequestClient<GetAgvBattery> _getAgvBatteryClient;
    private readonly IRequestClient<GetCurrentAgvRoute> _getCurrentAgvRouteClient;
    private readonly IRequestClient<GetRoute> _getRouteClient;
    private readonly IRequestClient<GetAgvPosition> _getAgvPositionClient;
    private readonly IRequestClient<AcceptAgvCommand> _acceptAgvClient;
    private readonly IRequestClient<LockAgv> _lockAgvClient;
    private readonly IRequestClient<RequestAgvData> _emitRequestDataClient;
    private readonly IRequestClient<RequestAgvGraph> _emitRequestGraphClient;
    private readonly IRequestClient<RequestAgvZones> _emitRequestZonesClient;
    private readonly IRequestClient<GetActiveAgvErrors> _getAgvErrorsRequestClient;
    private readonly IRequestClient<GetAgvErrors> _getAgvErrorHistoryRequestClient;
    private readonly IRequestClient<ChargeAgv> _chargeAgvClient;
    private readonly IRequestClient<MoveAgv> _moveAgvClient;
    private readonly IRequestClient<MoveAgvToCoordinate> _moveAgvToCoordinateClient;
    private readonly IRequestClient<SetAgvForceIdle> _setForceIdleClient;
    private readonly IRequestClient<GetAgvHasCargo> _getAgvHasCargoClient;
    private readonly IRequestClient<GetCargosOfAgv> _getCargosOfAgvClient;
    private readonly IRequestClient<ResetAgvErrors> _resetAgvErrorsClient;
    private readonly IRequestClient<GetAgvSettings> _getAgvSettingsClient;
    private readonly IRequestClient<UpdateAgvSettings> _updateAgvSettingsClient;
    private readonly IRequestClient<SendAgvToCharger> _sendAgvToChargerClient;
    private readonly IRequestClient<ResetAgvSettingToDefault> _resetSettingToDefaultClient;
    private readonly IRequestClient<SetUserMode> _setUserModeClient;
    private readonly IRequestClient<SetMaintenanceMode> _setMaintenanceModeClient;
    private readonly IRequestClient<SetNormalMode> _setNormalModeClient;
    private readonly IRequestClient<GetAgvInstantActionDefinitions> _getInstantActionsClient;
    private readonly IRequestClient<StartInstantAction> _startInstantActionsClient;
    private readonly IRequestClient<ResendAction> _resendActionClient;
    private readonly IRequestClient<RemoveAgvFromAllZones> _removeAgvFromZonesClient;
    private readonly IRequestClient<SolveResourceBlockingErrors> _resourceBlockingClient;
    private readonly IRequestClient<RemoveAgvFromZone> _removeAgvFromZoneClient;
    private readonly IRequestClient<RemoveAgvFromStation> _removeAgvFromStationClient;
    private readonly IRequestClient<TriggerAddingMaintenanceLog> _addMaintenanceLogClient;
    private readonly IRequestClient<GetMaintenanceLogsForAgv> _getMaintenanceLogsClient;
    private readonly IMapper _mapper;
    private readonly ILogger<AgvController> _logger;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Init controller
    /// </summary>
    public AgvController(
        ILogger<AgvController> logger,
        IMapper mapper,
        IRequestClient<GetAgv> getAgvClient,
        IRequestClient<GetAgvGraph> getAgvGraphClient,
        IRequestClient<GetAgvData> getAgvDataClient,
        IRequestClient<GetAgvBattery> getAgvBatteryClient,
        IRequestClient<GetCurrentAgvRoute> getCurrentAgvRouteClient,
        IRequestClient<GetRoute> getRouteClient,
        IRequestClient<GetAgvPosition> getAgvPositionClient,
        IRequestClient<AcceptAgvCommand> acceptAgvClient,
        IRequestClient<LockAgv> lockAgvClient,
        IRequestClient<RequestAgvData> emitRequestDataClient,
        IRequestClient<RequestAgvGraph> emitRequestGraphClient,
        IRequestClient<RequestAgvZones> emitRequestZonesClient,
        IRequestClient<GetActiveAgvErrors> getAgvErrorsRequestClient,
        IRequestClient<GetAgvErrors> getAgvErrorHistoryRequestClient,
        IRequestClient<MoveAgv> moveAgvClient,
        IRequestClient<MoveAgvToCoordinate> moveAgvToCoordinateClient,
        IRequestClient<ChargeAgv> chargeAgvClient,
        IRequestClient<SetAgvForceIdle> setForceIdleClient,
        IRequestClient<GetAgvHasCargo> getAgvHasCargoClient,
        IRequestClient<GetCargosOfAgv> getCargosOfAgvClient,
        IRequestClient<ResetAgvErrors> resetAgvErrorsClient,
        IRequestClient<GetAgvSettings> getAgvSettingsClient,
        IRequestClient<SendAgvToCharger> sendAgvToChargerClient,
        IRequestClient<UpdateAgvSettings> updateAgvSettingsClient,
        IRequestClient<ResetAgvSettingToDefault> resetSettingToDefaultClient,
        IRequestClient<SetUserMode> setUserModeClient,
        IRequestClient<SetMaintenanceMode> setMaintenanceModeClient,
        IRequestClient<SetNormalMode> setNormalModeClient,
        IRequestClient<GetAgvInstantActionDefinitions> getInstantActionsClient,
        IRequestClient<StartInstantAction> startInstantActionsClient,
        IRequestClient<ResendAction> resendActionClient,
        IRequestClient<RemoveAgvFromAllZones> removeAgvFromZonesClient,
        IRequestClient<SolveResourceBlockingErrors> resourceBlockingClient,
        IRequestClient<RemoveAgvFromZone> removeAgvFromZoneClient,
        IRequestClient<RemoveAgvFromStation> removeAgvFromStationClient,
        IRequestClient<TriggerAddingMaintenanceLog> addMaintenanceLogClient,
        IRequestClient<GetMaintenanceLogsForAgv> getMaintenanceLogsClient,
        ILoggingService loggingService
    )
    {
        _logger = logger;
        _getAgvClient = getAgvClient;
        _getAgvGraphClient = getAgvGraphClient;
        _getAgvDataClient = getAgvDataClient;
        _getAgvBatteryClient = getAgvBatteryClient;
        _getCurrentAgvRouteClient = getCurrentAgvRouteClient;
        _getRouteClient = getRouteClient;
        _getAgvPositionClient = getAgvPositionClient;
        _acceptAgvClient = acceptAgvClient;
        _lockAgvClient = lockAgvClient;
        _emitRequestDataClient = emitRequestDataClient;
        _emitRequestGraphClient = emitRequestGraphClient;
        _emitRequestZonesClient = emitRequestZonesClient;
        _getAgvErrorsRequestClient = getAgvErrorsRequestClient;
        _getAgvErrorHistoryRequestClient = getAgvErrorHistoryRequestClient;
        _moveAgvClient = moveAgvClient;
        _moveAgvToCoordinateClient = moveAgvToCoordinateClient;
        _chargeAgvClient = chargeAgvClient;
        _setForceIdleClient = setForceIdleClient;
        _getAgvHasCargoClient = getAgvHasCargoClient;
        _getCargosOfAgvClient = getCargosOfAgvClient;
        _mapper = mapper;
        _resetAgvErrorsClient = resetAgvErrorsClient;
        _getAgvSettingsClient = getAgvSettingsClient;
        _sendAgvToChargerClient = sendAgvToChargerClient;
        _updateAgvSettingsClient = updateAgvSettingsClient;
        _resetSettingToDefaultClient = resetSettingToDefaultClient;
        _setUserModeClient = setUserModeClient;
        _setMaintenanceModeClient = setMaintenanceModeClient;
        _setNormalModeClient = setNormalModeClient;
        _getInstantActionsClient = getInstantActionsClient;
        _startInstantActionsClient = startInstantActionsClient;
        _resendActionClient = resendActionClient;
        _removeAgvFromZonesClient = removeAgvFromZonesClient;
        _removeAgvFromZoneClient = removeAgvFromZoneClient;
        _resourceBlockingClient = resourceBlockingClient;
        _removeAgvFromStationClient = removeAgvFromStationClient;
        _addMaintenanceLogClient = addMaintenanceLogClient;
        _getMaintenanceLogsClient = getMaintenanceLogsClient;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Get agv.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlation id not found.</response>
    /// <returns>The full agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}")]
    public async Task<ActionResult<AgvDto>> GetAgv(Guid correlationId)
    {
        Response<GotAgv> response = await _getAgvClient.GetResponse<GotAgv>(new GetAgv(correlationId));
        var agv = _mapper.Map<AgvDto>(response.Message.Agv);
        return Ok(agv);
    }

    /// <summary>
    /// Get agv graph.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No content</response>
    /// <response code="404">Agv with given correlation id not found.</response>
    /// <returns>The agv graph of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/graph")]
    public async Task<ActionResult<GraphDto>> GetAgvGraph(Guid correlationId)
    {
        Response<GotAgvGraph> response = await _getAgvGraphClient.GetResponse<GotAgvGraph>(new GetAgvGraph(correlationId));
        var graph = _mapper.Map<GraphDto>(response.Message.AgvGraph);
        return Ok(graph);
    }

    /// <summary>
    /// Get agv data.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No Content</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The agv data of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/data")]
    public async Task<ActionResult<AgvDataDto>> GetAgvData(Guid correlationId)
    {
        Response<GotAgvData> response = await _getAgvDataClient.GetResponse<GotAgvData>(new GetAgvData(correlationId));
        var agvData = _mapper.Map<AgvDataDto>(response.Message.AgvData);
        return Ok(agvData);
    }

    /// <summary>
    /// Get agv battery.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The battery status of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/battery")]
    public async Task<ActionResult<AgvBatteryStatusDto>> GetAgvBattery(Guid correlationId)
    {
        Response<GotAgvBattery> response = await _getAgvBatteryClient.GetResponse<GotAgvBattery>(new GetAgvBattery(correlationId));
        var agvBattery = _mapper.Map<AgvBatteryStatusDto>((response.Message.AgvBatteryStatus, response.Message.AgvSettings));
        return Ok(agvBattery);
    }

    /// <summary>
    /// Get agv settings.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No Content</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The current settings of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/settings")]
    public async Task<ActionResult<AgvSettingsResponseDto>> GetAgvSettings(Guid correlationId)
    {
        Response<GotAgvSettings> response = await _getAgvSettingsClient.GetResponse<GotAgvSettings>(new GetAgvSettings(correlationId));
        var dto = _mapper.Map<AgvSettingsResponseDto>(response.Message.AgvSettings);
        return Ok(dto);
    }

    /// <summary>
    /// Update agv settings (only non-null properties are updated).
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="settings">The updated settings (Provide only setting properties to update).</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/settings")]
    public async Task<ActionResult> UpdateAgvSettings([FromRoute] Guid correlationId, [FromBody] AgvSettingsDto settings)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _updateAgvSettingsClient.GetResponse<Success>(new UpdateAgvSettings(correlationId, _mapper.Map<AgvSettingsReadModel>(settings), email));
        return Ok();
    }

    /// <summary>
    /// Reset agv settings property to default.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="propertyName">The settings property name to reset to default.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="400">Invalid (unknown) agv settings property name.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/settings/reset")]
    public async Task<ActionResult> ResetAgvSettingToDefault([FromRoute] Guid correlationId, [FromQuery] string propertyName)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _resetSettingToDefaultClient.GetResponse<Success>(new ResetAgvSettingToDefault(correlationId, propertyName, email));
        return Ok();
    }

    /// <summary>
    /// Remove an agv from all zones it occupies.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found or no online agv found to remove agv from zone.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/settings/removeFromZones")]
    public async Task<ActionResult> RemoveAgvFromZones([FromRoute] Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _removeAgvFromZonesClient.GetResponse<Success>(new RemoveAgvFromAllZones(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Get current route of agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation Id.</param>
    /// <returns></returns>
    /// <response code="200">AgvRoute</response>
    /// <response code="204">No running ActionGroup and therefore current route does not exist.</response>
    /// <response code="404">Agv with given correlation id not found.</response>
    /// <returns>The current route of that Agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/currentRoute")]
    public async Task<ActionResult<CurrentRouteDto?>> GetAgvRoute(Guid correlationId)
    {
        Response<GotCurrentAgvRoute> response = await _getCurrentAgvRouteClient.GetResponse<GotCurrentAgvRoute>(new GetCurrentAgvRoute(correlationId));
        var dto = _mapper.Map<CurrentRouteDto>(response.Message.AgvRoute);
        return Ok(dto);
    }

    /// <summary>
    /// Get route for an agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation Id.</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the Agv will be used.</param>
    /// <param name="destinationNodeId">The destination node Id to which the route should be calculated, starting from the Agv's current node.</param>
    /// <returns></returns>
    /// <response code="200">Route</response>
    /// <response code="204">No Route found.</response>
    /// <response code="400">Start/Destination node not found.</response>
    /// <response code="404">Agv with given correlation id not found.</response>
    /// <returns>The calculated route for the given parameters.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/route")]
    public async Task<ActionResult<RouteDto>> GetAgvRoute(Guid correlationId, [Required] string destinationNodeId, string? startNodeId = null)
    {
        Response<GotRoute> response = await _getRouteClient.GetResponse<GotRoute>(new GetRoute(correlationId, destinationNodeId.ToGuid(), startNodeId?.ToGuid()));
        var dto = _mapper.Map<RouteDto>(response.Message.Route);
        return Ok(dto);
    }

    /// <summary>
    /// Get agv position.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The current agv position.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/position")]
    public async Task<ActionResult<AgvPositionStatusDto>> GetAgvPosition(Guid correlationId)
    {
        Response<GotAgvPosition> response = await _getAgvPositionClient.GetResponse<GotAgvPosition>(new GetAgvPosition(correlationId));

        var positionStatus = _mapper.Map<AgvPositionStatusDto>(response.Message.AgvPositions);
        return Ok(positionStatus);
    }

    /// <summary>
    /// Get list of active errors of an agv.
    /// </summary>
    /// <returns>The list of active errors of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/errors")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrors(Guid correlationId)
    {
        Response<GotActiveAgvErrors> response = await _getAgvErrorsRequestClient.GetResponse<GotActiveAgvErrors>(new GetActiveAgvErrors(correlationId));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Sends a request to the agv to reset its errors.
    /// If one error reset fails, the whole request fails.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="errorIds">If specified, resets all specified errors. If no errors are explicitly specified, it resets all active errors.</param>
    /// <response code="200">All errors were reset successfully.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>The description of the result.</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/errors/reset")]
    public async Task<ActionResult<string>> ResetAgvErrors(Guid correlationId, Collection<Guid>? errorIds = null)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<ResetAgvErrorsResult> response = await _resetAgvErrorsClient.GetResponse<ResetAgvErrorsResult>(new ResetAgvErrors(correlationId, errorIds ?? new Collection<Guid>(), email));
        return Ok(response.Message.Description);
    }

    /// <summary>
    /// Get list of active and solved errors of an agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
    /// <returns>The list of historic errors of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/errorHistory")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrorHistory(Guid correlationId, DateTime? dateFrom, DateTime? dateTo)
    {
        Response<GotAgvErrors> response = await _getAgvErrorHistoryRequestClient.GetResponse<GotAgvErrors>(new GetAgvErrors(correlationId, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Accept discovered agv to allow it to be assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: agv status was successfully set to Idle</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="409">Conflict: Agv is not in state 'Discovered'</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/accept")]
    public async Task<ActionResult> AcceptAgv(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult> response =
            await _acceptAgvClient.GetResponse<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult>(new AcceptAgvCommand(correlationId));

        if (response.Message is AcceptAgvSucceeded)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, email }, { LogLabels.Operation, LogCodes.AgvAccept } });
            return Ok();
        }

        if (response.Message is AcceptAgvRejected)
        {
            return Conflict();
        }

        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }

        return Problem();
    }

    /// <summary>
    /// Lock agv to prevent it from being assigned to transport orders. Mode will be transitioned 
    /// after the assigned action group policy is finished.
    /// </summary>
    /// <response code="200">Ok: Transition to locked mode pending.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/lock")]
    public async Task<ActionResult> LockAgv(
        Guid correlationId,
        [FromQuery] LockReason lockReason,
        [FromQuery] string reasonDescription,
        [FromQuery] AssignedActionGroupPolicy assignedActionGroupsPolicy = AssignedActionGroupPolicy.Reset,
        [FromQuery] CurrentActionGroupPolicy currentActionGroupPolicy = CurrentActionGroupPolicy.FinishAction,
        [FromQuery] bool cancelManualActions = false)
    {
        var lockAgvCommand = new LockAgv(
            correlationId,
            lockReason,
            reasonDescription,
            User.GetEmail(),
            assignedActionGroupsPolicy,
            currentActionGroupPolicy,
            cancelManualActions);

        Response<LockAgvResult, AgvNotFoundResult> response = await _lockAgvClient.GetResponse<LockAgvResult, AgvNotFoundResult>(lockAgvCommand);

        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.AgvLocked } });

        return Ok();
    }

    /// <summary>
    /// Set agv to user mode. Mode will be transitioned
    /// after the assigned action group policy is finished.
    /// </summary>
    /// <response code="200">Ok: Transition to user mode pending.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/userMode")]
    public async Task<ActionResult> SetUserMode(
        Guid correlationId,
        [FromQuery] AssignedActionGroupPolicy assignedActionGroupsPolicy = AssignedActionGroupPolicy.LeaveAssigned,
        [FromQuery] CurrentActionGroupPolicy currentActionGroupPolicy = CurrentActionGroupPolicy.Pause,
        [FromQuery] bool cancelManualActions = false)
    {
        await _setUserModeClient.GetResponse<Success>(new SetUserMode(correlationId, User.GetEmail(), assignedActionGroupsPolicy, currentActionGroupPolicy, cancelManualActions));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.AgvModeChanged } });

        return Ok();
    }

    /// <summary>
    /// Set agv to maintenance mode.Mode will be transitioned
    /// after the assigned action group policy is finished.
    /// </summary>
    /// <response code="200">Ok: Transition to maintenance mode pending.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/maintenanceMode")]
    public async Task<ActionResult> SetMaintenanceMode(
        Guid correlationId,
        [FromQuery] MaintenanceReason maintenanceReason,
        [FromQuery] [Required] string reasonDescription,
        [FromQuery] AssignedActionGroupPolicy assignedActionGroupsPolicy = AssignedActionGroupPolicy.Reset,
        [FromQuery] CurrentActionGroupPolicy currentActionGroupPolicy = CurrentActionGroupPolicy.Pause,
        [FromQuery] bool cancelManualActions = false)
    {
        var setMaintenanceModeCommand = new SetMaintenanceMode(
            correlationId,
            User.GetEmail(),
            maintenanceReason,
            reasonDescription,
            assignedActionGroupsPolicy,
            currentActionGroupPolicy,
            cancelManualActions);

        await _setMaintenanceModeClient.GetResponse<Success>(setMaintenanceModeCommand);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.AgvModeChanged } });

        return Ok();
    }

    /// <summary>
    /// Set agv to normal mode.
    /// </summary>
    /// <response code="200">Ok: Agv was successfully set to normal mode.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/normalMode")]
    public async Task<ActionResult> SetNormalMode(Guid correlationId)
    {
        await _setNormalModeClient.GetResponse<Success>(new SetNormalMode(correlationId, null));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.AgvModeChanged } });

        return Ok();
    }

    /// <summary>
    /// Emit RequestAgvData message.
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/emitRequestData")]
    public async Task<ActionResult> EmitRequestData(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _emitRequestDataClient.GetResponse<RequestedAgvData>(new RequestAgvData(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Emit RequestAgvGraph message.
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/emitRequestGraph")]
    public async Task<ActionResult> EmitRequestGraph(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _emitRequestGraphClient.GetResponse<RequestAgvGraphResult>(new RequestAgvGraph(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Emit RequestAgvZones message.
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/emitRequestZones")]
    public async Task<ActionResult> EmitRequestZones(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _emitRequestZonesClient.GetResponse<Success>(new RequestAgvZones(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Move agv manually to a specific node. 
    /// </summary>
    /// <param name="correlationId">Id of the agv.</param>
    /// <param name="destinationNodeId">Id of the destination node.</param>
    /// <param name="theta">Angle in rad.</param>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="400">Target node does not exist or is reserved or occupied or agv is locked.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="409">Destination node needs reservation, but is currently not available.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/move")]
    public async Task<ActionResult<Guid>> MoveAgv(Guid correlationId, string destinationNodeId, [FromQuery] double theta = 0)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<AgvMoveSuccessful> response = await _moveAgvClient.GetResponse<AgvMoveSuccessful>(new MoveAgv(correlationId, destinationNodeId.ToGuid(), ActionGroupCreator.TransportOrderWebApi, email, theta));

        return Ok(response.Message.ActionGroupCorrelationId);
    }

    /// <summary>
    /// Move agv manually to a coordinate with orientation. 
    /// </summary>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="400">The position is inside a restricted area.
    /// Or the agv is not in user or maintenance mode.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/moveToCoordinate")]
    public async Task<ActionResult<Guid>> MoveAgvToCoordinate(
        Guid correlationId,
        [FromQuery] [Required] float x,
        [FromQuery] [Required] float y,
        [FromQuery] [Required] float theta)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<AgvMoveSuccessful> response = await _moveAgvToCoordinateClient.GetResponse<AgvMoveSuccessful>(new MoveAgvToCoordinate(correlationId, x, y, theta, ActionGroupCreator.TransportOrderWebApi, email));

        return Ok(response.Message.ActionGroupCorrelationId);
    }

    /// <summary>
    /// Move agv manually to a specific charger and charge it there. 
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV.</param>
    /// <param name="chargerId">The identifier of the charger.</param>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="400">ChargerId does not correspond to a charger or does not exist at all or agv is locked.</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <response code="409">Charger is currently not available.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/chargeAtCharger")]
    public async Task<ActionResult<Guid>> ChargeAgvAtCharger(
        Guid correlationId,
        string chargerId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<AgvMoveSuccessful> response = await _chargeAgvClient.GetResponse<AgvMoveSuccessful>(new ChargeAgv(correlationId, chargerId.ToGuid(), email));

        return Ok(response.Message.ActionGroupCorrelationId);
    }

    /// <summary>
    /// Set an agv to idle by force.
    /// </summary>
    /// <param name="correlationId">The correlation id of the Agv.</param>
    /// <response code="200">Ok: The agv was set to idle.</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/setForceIdle")]
    public async Task<ActionResult> SetForceIdle(Guid correlationId)
    {
        Response<SetAgvForceIdleResult, AgvNotFoundResult> response = await _setForceIdleClient.GetResponse<SetAgvForceIdleResult, AgvNotFoundResult>(new SetAgvForceIdle(correlationId));
        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.AgvIdle } });

        return Ok();
    }

    /// <summary>
    /// Get hasCargo state for given agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>True if the Agv has cargo.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/hasCargo")]
    public async Task<ActionResult<bool>> HasCargo(Guid correlationId)
    {
        Response<GotAgvHasCargo> response = await _getAgvHasCargoClient.GetResponse<GotAgvHasCargo>(new GetAgvHasCargo(correlationId));
        return Ok(response.Message.AgvHasCargo.HasCargo);
    }

    /// <summary>
    /// Retrieve currently loaded cargos for given agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>The list of cargos of that agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/cargos")]
    public async Task<ActionResult<ICollection<CargoDto>>> GetCargosOfAgv(Guid correlationId)
    {
        Response<GotCargosOfAgv> response = await _getCargosOfAgvClient.GetResponse<GotCargosOfAgv>(new GetCargosOfAgv(correlationId));
        var dto = _mapper.Map<ICollection<CargoDto>>(response.Message.Cargos);
        return Ok(dto);
    }

    /// <summary>
    /// Send agv to nearest charger.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV.</param>
    /// <response code="200">Ok: The ActionGroup for the charge was created and is going to be processed.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <response code="409">No charger is currently available.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/charge")]
    public async Task<ActionResult> SendAgvToCharger(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _sendAgvToChargerClient.GetResponse<Success>(new SendAgvToCharger(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Get instant action definitions for an agv.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No Content</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The instant actions of agv.</returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/instantActions")]
    public async Task<ActionResult<ICollection<AgvInstantActionDefinitionDto>>> GetInstantActionDefinitions(Guid correlationId)
    {
        Response<GotAgvInstantActionDefinitions> response = await _getInstantActionsClient.GetResponse<GotAgvInstantActionDefinitions>(new GetAgvInstantActionDefinitions(correlationId));
        var instantActions = _mapper.Map<ICollection<AgvInstantActionDefinitionDto>>(response.Message.AgvInstantActionDefinitions);
        return Ok(instantActions);
    }

    /// <summary>
    /// Start an instant action for an agv.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No Content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">AGV with given correlationId not found or instant action of this type not found or given parameter not found.</response>
    /// <returns>The id of the instant action.</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/instantActions")]
    public async Task<ActionResult<string>> StartInstantAction(Guid correlationId, [FromBody] AgvInstantActionDto action)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _startInstantActionsClient.GetResponse<Success>(new StartInstantAction(correlationId,
            action.ActionType,
            action.Parameters?.ToDictionary(k => k.Key, v => v.Value),
            action.BlockingType,
            email));
        return Ok();
    }

    /// <summary>
    /// Resend an action to the agv.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/resendAction")]
    public async Task<ActionResult> ResendAction(Guid correlationId)
    {
        Response<Success, ActionNotFound> result = await _resendActionClient.GetResponse<Success, ActionNotFound>(new ResendAction(correlationId));
        if (result.Message is ActionNotFound)
        {
            return Conflict();
        }

        return Ok();
    }

    /// <summary>
    /// Solve all resource blocking errors for an agv: remove all node locks.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="ignoreErrorState">If the error state should be ignored when removing resource locks.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/solveResourceErrors")]
    public async Task<ActionResult> SolveResourceErrors([FromRoute] Guid correlationId, bool ignoreErrorState = false)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _resourceBlockingClient.GetResponse<Success>(new SolveResourceBlockingErrors(correlationId, email, ignoreErrorState));
        return Ok();
    }

    /// <summary>
    /// Removes the agv from the provided zone
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="zoneId">The id of the zone.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlation id not found.</response>
    /// <response code="409">Agv is not in the provided zone or all agvs are offline.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/removeFromZone")]
    public async Task<ActionResult> RemoveFromZone([FromRoute] Guid correlationId, [Required] string zoneId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _removeAgvFromZoneClient.GetResponse<Success>(new RemoveAgvFromZone(correlationId, zoneId, email));
        return Ok();
    }

    /// <summary>
    /// Request to remove an agv from a station.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="stationName">The station name.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Station/Agv with given id not found.</response>
    /// <response code="409">All agvs are offline.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/removeFromStation")]
    public async Task<ActionResult> RemoveFromStation([FromRoute] Guid correlationId, [Required] string stationName)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _removeAgvFromStationClient.GetResponse<Success>(new RemoveAgvFromStation(stationName, correlationId, email));

        return Ok();
    }

    /// <summary>
    /// Add a maintenance log entry for an agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="maintenanceType">The type of maintenance done.</param>
    /// <param name="executionDate">The date and time the maintenance was done.</param>
    /// <param name="comment">Optional comment on the maintenance.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MaintenanceLogFullAccess)]
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/maintenanceLog")]
    public async Task<ActionResult> AddMaintenanceLog([FromRoute] Guid correlationId, [FromQuery] MaintenanceType maintenanceType, [FromQuery] DateTime executionDate, [FromQuery] string? comment)
    {
        await _addMaintenanceLogClient.GetResponse<Success>(new TriggerAddingMaintenanceLog(User.GetEmail() ?? string.Empty, correlationId, maintenanceType, executionDate, comment));
        return Ok();
    }

    /// <summary>
    /// Get all maintenance log entries for an agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="maintenanceType">Optional filter for the type of maintenance done.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/maintenanceLog")]
    public async Task<ActionResult<ICollection<AgvMaintenanceLogDto>>> GetMaintenanceLog([FromRoute] Guid correlationId, [FromQuery] MaintenanceType? maintenanceType)
    {
        Response<GotMaintenanceLogs> response = await _getMaintenanceLogsClient.GetResponse<GotMaintenanceLogs>(new GetMaintenanceLogsForAgv(correlationId, maintenanceType));
        return Ok(_mapper.Map<List<AgvMaintenanceLogDto>>(response.Message.MaintenanceLogs));
    }
}