using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

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
    private readonly IRequestClient<UnlockAgv> _unlockAgvClient;
    private readonly IRequestClient<LockAgv> _lockAgvClient;
    private readonly IRequestClient<RequestAgvData> _emitRequestDataClient;
    private readonly IRequestClient<RequestAgvGraph> _emitRequestGraphClient;
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
    private readonly IMapper _mapper;

    /// <summary>
    /// Init controller
    /// </summary>
    public AgvController(
        IMapper mapper,
        IRequestClient<GetAgv> getAgvClient,
        IRequestClient<GetAgvGraph> getAgvGraphClient,
        IRequestClient<GetAgvData> getAgvDataClient,
        IRequestClient<GetAgvBattery> getAgvBatteryClient,
        IRequestClient<GetCurrentAgvRoute> getCurrentAgvRouteClient,
        IRequestClient<GetRoute> getRouteClient,
        IRequestClient<GetAgvPosition> getAgvPositionClient,
        IRequestClient<AcceptAgvCommand> acceptAgvClient,
        IRequestClient<UnlockAgv> unlockAgvClient,
        IRequestClient<LockAgv> lockAgvClient,
        IRequestClient<RequestAgvData> emitRequestDataClient,
        IRequestClient<RequestAgvGraph> emitRequestGraphClient,
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
        IRequestClient<SetNormalMode> setNormalModeClient
    )
    {
        _getAgvClient = getAgvClient;
        _getAgvGraphClient = getAgvGraphClient;
        _getAgvDataClient = getAgvDataClient;
        _getAgvBatteryClient = getAgvBatteryClient;
        _getCurrentAgvRouteClient = getCurrentAgvRouteClient;
        _getRouteClient = getRouteClient;
        _getAgvPositionClient = getAgvPositionClient;
        _acceptAgvClient = acceptAgvClient;
        _unlockAgvClient = unlockAgvClient;
        _lockAgvClient = lockAgvClient;
        _emitRequestDataClient = emitRequestDataClient;
        _emitRequestGraphClient = emitRequestGraphClient;
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
        var agvBattery = _mapper.Map<AgvBatteryStatusDto>(response.Message.AgvBatteryStatus);
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
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/settings")]
    public async Task<ActionResult> UpdateAgvSettings([FromRoute] Guid correlationId, [FromBody] AgvSettingsDto settings)
    {
        await _updateAgvSettingsClient.GetResponse<Success>(new UpdateAgvSettings(correlationId, _mapper.Map<AgvSettingsReadModel>(settings)));
        return Ok();
    }

    /// <summary>
    /// Reset agv settings property to default.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="propertyName">The settings property name to reset to default.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="400">Invalid (unknown) agv settings property name.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/settings/reset")]
    public async Task<ActionResult> ResetAgvSettingToDefault([FromRoute] Guid correlationId, [FromQuery] string propertyName)
    {
        await _resetSettingToDefaultClient.GetResponse<Success>(new ResetAgvSettingToDefault(correlationId, propertyName));
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
        Response<GotRoute> response = await _getRouteClient.GetResponse<GotRoute>(new GetRoute(correlationId, destinationNodeId, startNodeId));
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
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>The description of the result.</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/errors/reset")]
    public async Task<ActionResult<string>> ResetAgvErrors(Guid correlationId, Collection<Guid>? errorIds = null)
    {
        Response<ResetAgvErrorsResult> response = await _resetAgvErrorsClient.GetResponse<ResetAgvErrorsResult>(new ResetAgvErrors(correlationId, errorIds ?? new Collection<Guid>()));
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
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="409">Conflict: Agv is not in state 'Discovered'</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/accept")]
    public async Task<ActionResult> AcceptAgv(Guid correlationId)
    {
        Response<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult> response =
            await _acceptAgvClient.GetResponse<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult>(new AcceptAgvCommand(correlationId));

        if (response.Message is AcceptAgvSucceeded)
        {
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
    /// Unlock agv to allow it to be assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: Agv was successfully unlocked</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/unlock")]
    public async Task<ActionResult> UnlockAgv(Guid correlationId)
    {
        Response<UnlockAgvResult, AgvNotFoundResult> response = await _unlockAgvClient.GetResponse<UnlockAgvResult, AgvNotFoundResult>(new UnlockAgv(correlationId));
        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Lock agv to prevent it from being assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: Agv was successfully locked</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/lock")]
    public async Task<ActionResult> LockAgv(Guid correlationId, [FromQuery] string reason)
    {
        Response<LockAgvResult, AgvNotFoundResult> response = await _lockAgvClient.GetResponse<LockAgvResult, AgvNotFoundResult>(new LockAgv(correlationId, reason, User.GetEmail()));
        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Set agv to user mode.
    /// </summary>
    /// <response code="200">Ok: Agv was successfully set to user mode.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/userMode")]
    public async Task<ActionResult> SetUserMode(Guid correlationId)
    {
        await _setUserModeClient.GetResponse<Success>(new SetUserMode(correlationId, User.GetEmail()));
        return Ok();
    }

    /// <summary>
    /// Set agv to maintenance mode.
    /// </summary>
    /// <response code="200">Ok: Agv was successfully set to maintenance mode.</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/maintenanceMode")]
    public async Task<ActionResult> SetMaintenanceMode(Guid correlationId, [FromQuery] string reason, [FromQuery] bool cancelManualActions)
    {
        await _setMaintenanceModeClient.GetResponse<Success>(new SetMaintenanceMode(correlationId, User.GetEmail(), reason, cancelManualActions));
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
        await _emitRequestDataClient.GetResponse<RequestedAgvData>(new RequestAgvData(correlationId));
        return Ok();
    }

    /// <summary>
    /// Emit RequestAgvGraph message.
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/emitRequestGraph")]
    public async Task<ActionResult> EmitRequestGraph(Guid correlationId)
    {
        await _emitRequestGraphClient.GetResponse<RequestAgvGraphResult>(new RequestAgvGraph(correlationId));
        return Ok();
    }

    /// <summary>
    /// Move agv manually to a specific node. 
    /// </summary>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="400">Target node does not exist or is reserved or occupied or agv is locked.</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="409">Destination node needs reservation, but is currently not available.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/move")]
    public async Task<ActionResult<Guid>> MoveAgv(Guid correlationId, string destinationNodeId)
    {
        Response<AgvMoveSuccessful> response = await _moveAgvClient.GetResponse<AgvMoveSuccessful>(new MoveAgv(correlationId, destinationNodeId, ActionGroupCreator.TransportOrderWebApi));

        return Ok(response.Message.ActionGroupCorrelationId);
    }

    /// <summary>
    /// Move agv manually to a coordinate with orientation. 
    /// </summary>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="400">The position is inside a restricted area.</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/moveToCoordinate")]
    public async Task<ActionResult<Guid>> MoveAgvToCoordinate(Guid correlationId, [FromQuery] [Required] float x, [FromQuery][Required] float y, [FromQuery][Required] float theta )
    {
        Response<AgvMoveSuccessful> response = await _moveAgvToCoordinateClient.GetResponse<AgvMoveSuccessful>(new MoveAgvToCoordinate(correlationId, x, y, theta, ActionGroupCreator.TransportOrderWebApi));

        return Ok(response.Message.ActionGroupCorrelationId);
    }

    /// <summary>
    /// Move agv manually to a specific charger and charge it there. 
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV.</param>
    /// <param name="chargerId">The identifier of the charger.</param>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed.</response>
    /// <response code="400">ChargerId does not correspond to a charger or does not exist at all or agv is locked.</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <response code="409">Charger is currently not available.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/chargeAtCharger")]
    public async Task<ActionResult<Guid>> ChargeAgvAtCharger(Guid correlationId, string chargerId)
    {
        Response<AgvMoveSuccessful> response = await _chargeAgvClient.GetResponse<AgvMoveSuccessful>(new ChargeAgv(correlationId, chargerId));

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
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <response code="409">No charger is currently available.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/charge")]
    public async Task<ActionResult> SendAgvToCharger(Guid correlationId)
    {
        await _sendAgvToChargerClient.GetResponse<Success>(new SendAgvToCharger(correlationId));
        return Ok();
    }
}