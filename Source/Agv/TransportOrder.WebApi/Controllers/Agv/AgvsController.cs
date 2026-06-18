using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Agv;

/// <summary>
/// Agv batch requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvsController : Controller
{
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IRequestClient<GetCurrentAgvRoutes> _getCurrentAgvRoutesClient;
    private readonly IRequestClient<GetAgvPositions> _getAgvPositionsClient;
    private readonly IRequestClient<GetAgvGraphs> _getAgvGraphsClient;
    private readonly IRequestClient<GetAgvsData> _getAgvDataClient;
    private readonly IRequestClient<GetAgvsBattery> _getAgvsBatteryClient;
    private readonly IRequestClient<GetActiveErrorsForAgvs> _getActiveErrorsClient;
    private readonly IRequestClient<GetErrorHistoryForAgvs> _getErrorHistoryClient;
    private readonly IRequestClient<GetCargosOfAgvs> _getCargosOfAgvsClient;
    private readonly IRequestClient<GetAgvsSettings> _getAgvsSettingsClient;
    private readonly IRequestClient<GetAgvsHasCargo> _getAgvsHasCargoClient;
    private readonly IRequestClient<UpdateDefaultAgvSettings> _updateDefaultSettingsClient;
    private readonly IRequestClient<UpdateAgvsSettings> _updateAgvsSettingsClient;
    private readonly IRequestClient<ResetAgvsSettingToDefault> _resetSettingsToDefaultClient;
    private readonly IRequestClient<GetAgvsInstantActionsDefinitions> _instantActionsClient;
    private readonly IRequestClient<GetMaintenanceLogsForAgvs> _getMaintenanceLogsClient;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AgvsController"/> instance.
    /// </summary>
    public AgvsController(IMapper mapper,
        IRequestClient<GetAgvs> getAgvsClient,
        IRequestClient<GetCurrentAgvRoutes> getCurrentAgvRoutesClient,
        IRequestClient<GetAgvPositions> getAgvPositionsClient,
        IRequestClient<GetAgvGraphs> getAgvGraphsClient,
        IRequestClient<GetAgvsData> getAgvDataClient,
        IRequestClient<GetAgvsBattery> getAgvsBatteryClient,
        IRequestClient<GetActiveErrorsForAgvs> getActiveErrorsClient,
        IRequestClient<GetErrorHistoryForAgvs> getErrorHistoryClient,
        IRequestClient<GetAgvsSettings> getAgvsSettingsClient,
        IRequestClient<GetCargosOfAgvs> getCargosOfAgvsClient,
        IRequestClient<GetAgvsHasCargo> getAgvsHasCargoClient,
        IRequestClient<UpdateDefaultAgvSettings> updateDefaultSettingsClient,
        IRequestClient<UpdateAgvsSettings> updateAgvsSettingsClient,
        IRequestClient<ResetAgvsSettingToDefault> resetSettingsToDefaultClient,
        IRequestClient<GetAgvsInstantActionsDefinitions> instantActionsClient,
        IRequestClient<GetMaintenanceLogsForAgvs> getMaintenanceLogsClient
    )
    {
        _getAgvsClient = getAgvsClient;
        _getCurrentAgvRoutesClient = getCurrentAgvRoutesClient;
        _getAgvPositionsClient = getAgvPositionsClient;
        _mapper = mapper;
        _getAgvGraphsClient = getAgvGraphsClient;
        _getAgvDataClient = getAgvDataClient;
        _getAgvsBatteryClient = getAgvsBatteryClient;
        _getActiveErrorsClient = getActiveErrorsClient;
        _getErrorHistoryClient = getErrorHistoryClient;
        _getCargosOfAgvsClient = getCargosOfAgvsClient;
        _getAgvsSettingsClient = getAgvsSettingsClient;
        _getAgvsHasCargoClient = getAgvsHasCargoClient;
        _updateDefaultSettingsClient = updateDefaultSettingsClient;
        _updateAgvsSettingsClient = updateAgvsSettingsClient;
        _resetSettingsToDefaultClient = resetSettingsToDefaultClient;
        _instantActionsClient = instantActionsClient;
        _getMaintenanceLogsClient = getMaintenanceLogsClient;
    }

    /// <summary>
    /// Get list of agvs.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/agvs")]
    public async Task<ActionResult<IEnumerable<AgvDto>>> GetAgvs()
    {
        Response<GotAgvs> response = await _getAgvsClient.GetResponse<GotAgvs>(new GetAgvs(new List<Guid>()));
        var agvs = _mapper.Map<List<AgvDto>>(response.Message.Agvs);
        return Ok(agvs);
        
        // check if map/NODE-1 is right
    }

    /// <summary>
    /// Get list of selected agvs.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpPost]
    [Route("/agvs")]
    public async Task<ActionResult<IEnumerable<AgvDto>>> GetSelectedAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        if (correlationIds.Count == 0)
        {
            return Ok(new List<AgvDto>());
        }
        Response<GotAgvs> response = await _getAgvsClient.GetResponse<GotAgvs>(new GetAgvs(correlationIds));
        var agvs = _mapper.Map<List<AgvDto>>(response.Message.Agvs);
        return Ok(agvs);
    }

    /// <summary>
    /// Get list of selected agvs positions.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Returns a list of AGV positions containing only entries which have a position</response>
    [HttpPost]
    [Route("/agvs/position")]
    public async Task<ActionResult<IEnumerable<AgvPositionStatusDto>>> GetAgvPositions([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvPositions> response = await _getAgvPositionsClient.GetResponse<GotAgvPositions>(new GetAgvPositions(correlationIds));

        var positionStatus = _mapper.Map<List<AgvPositionStatusDto>>(response.Message.AgvPositions);
        return Ok(positionStatus);
    }

    /// <summary>
    /// Get list of selected agvs graphs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids does not belong to a known agv.</response>
    [HttpPost]
    [Route("/agvs/graph")]
    public async Task<ActionResult<IEnumerable<GraphDto>>> GetAgvGraphs([FromQuery] ICollection<Guid> correlationIds)
    {
        if (correlationIds.Count == 0)
        {
            return Ok(new List<GraphDto>());
        }
        Response<GotAgvGraphs> response = await _getAgvGraphsClient.GetResponse<GotAgvGraphs>(new GetAgvGraphs(correlationIds));
        var graphs = _mapper.Map<List<GraphDto>>(response.Message.Graphs);
        return Ok(graphs);
    }

    /// <summary>
    /// Get list of selected agvs data.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    [HttpPost]
    [Route("/agvs/data")]
    public async Task<ActionResult<IEnumerable<AgvDataDto>>> GetAgvsData([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsData> response = await _getAgvDataClient.GetResponse<GotAgvsData>(new GetAgvsData(correlationIds));
        var agvDataDtos = _mapper.Map<List<AgvDataDto>>(response.Message.AgvData);
        return Ok(agvDataDtos);
    }

    /// <summary>
    /// Get list of selected agvs battery.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/battery")]
    public async Task<ActionResult<IEnumerable<AgvBatteryStatusDto>>> GetAgvsBattery([FromQuery] ICollection<Guid> correlationIds)
    {
        if (correlationIds.Count == 0)
        {
            return Ok(new List<AgvBatteryStatusDto>());
        }
        Response<GotAgvsBattery> response = await _getAgvsBatteryClient.GetResponse<GotAgvsBattery>(new GetAgvsBattery(correlationIds));
        var agvBattery = _mapper.Map<List<AgvBatteryStatusDto>>(response.Message.AgvBatteryStatus);
        return Ok(agvBattery);
    }

    /// <summary>
    /// Get list of selected agvs` current routes.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/currentRoute")]
    public async Task<ActionResult<IEnumerable<CurrentRouteDto>>> GetAgvRoute([FromQuery] ICollection<Guid> correlationIds)
    {
        if (correlationIds.Count == 0)
        {
            return Ok(new List<CurrentRouteDto>());
        }
        Response<GotCurrentAgvRoutes> response = await _getCurrentAgvRoutesClient.GetResponse<GotCurrentAgvRoutes>(new GetCurrentAgvRoutes(correlationIds));
        var dto = _mapper.Map<List<CurrentRouteDto>>(response.Message.AgvRoutes);
        List<CurrentRouteDto> result = correlationIds.Except(dto.Select(d => d.CorrelationId)).Select(c => new CurrentRouteDto(c, [], [])).ToList();
        result.AddRange(dto ?? []);
        return Ok(result);
    }

    /// <summary>
    /// Get mapping of selected agvs` active error lists.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/errors")]
    public async Task<ActionResult<IEnumerable<AgvErrorsDto>>> GetErrorsForSelectedAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotActiveErrorsForAgvs> response = await _getActiveErrorsClient.GetResponse<GotActiveErrorsForAgvs>(new GetActiveErrorsForAgvs(correlationIds));
        var dto = _mapper.Map<List<AgvErrorsDto>>(response.Message.AgvsErrors);
        List<AgvErrorsDto> result = correlationIds.Except(dto.Select(d => d.CorrelationId)).Select(c => new AgvErrorsDto(c, [])).ToList();
        result.AddRange(dto);
        return Ok(result);
    }

    /// <summary>
    ///Get mapping of selected agvs active and solved error lists.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/errorHistory")]
    public async Task<ActionResult<IEnumerable<AgvErrorsDto>>> GetErrorHistoryForSelectedAgvs([FromQuery] ICollection<Guid> correlationIds, DateTime? dateFrom, DateTime? dateTo)
    {
        Response<GotErrorHistoryForAgvs> response = await _getErrorHistoryClient.GetResponse<GotErrorHistoryForAgvs>(new GetErrorHistoryForAgvs(correlationIds, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var dto = _mapper.Map<List<AgvErrorsDto>>(response.Message.AgvsErrors);
        List<AgvErrorsDto> result = correlationIds.Except(dto.Select(d => d.CorrelationId)).Select(c => new AgvErrorsDto(c, [])).ToList();
        result.AddRange(dto);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve currently loaded Cargos for given Agvs
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/cargos")]
    public async Task<ActionResult<IEnumerable<CargosDto>>> GetCargosOfAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotCargosOfAgvs> response = await _getCargosOfAgvsClient.GetResponse<GotCargosOfAgvs>(new GetCargosOfAgvs(correlationIds));
        var dto = _mapper.Map<IEnumerable<CargosDto>>(response.Message.AgvCargosList);
        return Ok(dto);
    }

    /// <summary>
    /// Get has cargo states for given agvs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/hasCargo")]
    public async Task<ActionResult<IEnumerable<AgvHasCargoDto>>> HasCargo([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsHasCargo> response = await _getAgvsHasCargoClient.GetResponse<GotAgvsHasCargo>(new GetAgvsHasCargo(correlationIds));
        var dto = _mapper.Map<List<AgvHasCargoDto>>(response.Message.AgvsHasCargo);
        return Ok(dto);
    }

    /// <summary>
    /// Get list of selected agvs settings
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/settings")]
    public async Task<ActionResult<IEnumerable<AgvSettingsResponseDto>>> GetAgvSettings([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsSettings> response = await _getAgvsSettingsClient.GetResponse<GotAgvsSettings>(new GetAgvsSettings(correlationIds));
        var dto = _mapper.Map<List<AgvSettingsResponseDto>>(response.Message.AgvSettings);
        return Ok(dto);
    }

    /// <summary>
    /// Set default value to new value for settings listed in request body.
    /// </summary>
    /// <param name="updatedSettings">The updated settings. </param>
    /// <response code="200">Settings updated.</response>
    /// <response code="401">Unauthorized</response>
    [HttpPatch]
    [Route("/agvs/defaultSettings")]
    public async Task<ActionResult> UpdateDefaultSettings([FromBody] AgvSettingsDto updatedSettings)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _updateDefaultSettingsClient.GetResponse<Success>(new UpdateDefaultAgvSettings(_mapper.Map<AgvSettingsReadModel>(updatedSettings), email));
        return Ok();
    }

    /// <summary>
    /// Update the settings for given agvs. All non-null properties get updated.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="updatedSettings">The updated settings.</param>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="200">Settings updated.</response>
    [HttpPatch]
    [Route("/agvs/settings")]
    public async Task<ActionResult> UpdateSettings([FromQuery] ICollection<Guid> correlationIds, [FromBody] AgvSettingsDto updatedSettings)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _updateAgvsSettingsClient.GetResponse<Success>(new UpdateAgvsSettings(correlationIds, _mapper.Map<AgvSettingsReadModel>(updatedSettings), email));
        return Ok();
    }

    /// <summary>
    /// Reset agv settings property to default for all given agvs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="propertyName">The settings property name to reset to default.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="400">Invalid (unknown) agv settings property name.</response>
    [HttpPatch]
    [Route("/agvs/settings/reset")]
    public async Task<ActionResult> ResetAgvsSettingToDefault([FromQuery] ICollection<Guid> correlationIds, [FromQuery] string propertyName)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _resetSettingsToDefaultClient.GetResponse<Success>(new ResetAgvsSettingToDefault(correlationIds, propertyName, email));
        return Ok();
    }

    /// <summary>
    /// Get instant action definitions for given agvs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    [HttpPost]
    [Route("/agvs/instantActions")]
    public async Task<ActionResult<Dictionary<Guid, List<AgvInstantActionDefinitionDto>>>> GetInstantActionDefinitions([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsInstantActionDefinitions> response = await _instantActionsClient.GetResponse<GotAgvsInstantActionDefinitions>(new GetAgvsInstantActionsDefinitions(correlationIds));
        var instantActions = new Dictionary<Guid, List<AgvInstantActionDefinitionDto>>();
        foreach ((var agv, List<InstantActionDefinition> instantActionDefinitions) in response.Message.InstantActionDefinitions)
        {
            instantActions.Add(agv, _mapper.Map<List<AgvInstantActionDefinitionDto>>(instantActionDefinitions));
        }
        return Ok(instantActions);
    }

    /// <summary>
    /// Get all maintenance log entries for given agvs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="maintenanceType">Optional filter for the type of maintenance done.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    [HttpPost]
    [Route("/agvs/maintenanceLog")]
    public async Task<ActionResult<Dictionary<Guid, ICollection<AgvMaintenanceLogDto>>>> GetMaintenanceLog([FromQuery] ICollection<Guid> correlationIds, [FromQuery] MaintenanceType? maintenanceType)
    {
        Response<GotMaintenanceLogs> response = await _getMaintenanceLogsClient.GetResponse<GotMaintenanceLogs>(new GetMaintenanceLogsForAgvs(correlationIds, maintenanceType));
        Dictionary<Guid, ICollection<AgvMaintenanceLogDto>> logs = response.Message.MaintenanceLogs
            .GroupBy(log => log.AgvCorrelationId)
            .ToDictionary(
                group => group.Key, 
                group => (ICollection<AgvMaintenanceLogDto>)group
                    .Select(log => _mapper.Map<AgvMaintenanceLogDto>(log))
                    .ToList()
                    .OrderByDescending(l => l.ExecutionDate)
                    .ToList()
            );
        return Ok(logs);
    }
}