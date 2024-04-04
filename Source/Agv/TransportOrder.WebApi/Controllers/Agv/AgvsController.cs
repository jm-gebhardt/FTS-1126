using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Agv;

/// <summary>
/// Agv batch requests.
/// </summary>
[ApiController]
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
        IRequestClient<ResetAgvsSettingToDefault> resetSettingsToDefaultClient
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
    }

    /// <summary>
    /// Get list of selected agvs.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpPost]
    [Route("/agvs")]
    public async Task<ActionResult<IEnumerable<AgvDto>>> GetSelectedAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvs> response = await _getAgvsClient.GetResponse<GotAgvs>(new GetAgvs(correlationIds));
        var agvs = _mapper.Map<List<AgvDto>>(response.Message.Agvs);
        return Ok(agvs);
    }

    /// <summary>
    /// Get list of selected agvs' positions.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/position")]
    public async Task<ActionResult<IEnumerable<AgvPositionStatusDto>>> GetAgvPositions([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvPositions> response = await _getAgvPositionsClient.GetResponse<GotAgvPositions>(new GetAgvPositions(correlationIds));

        var positionStatus = _mapper.Map<List<AgvPositionStatusDto>>(response.Message.AgvPositions);
        return Ok(positionStatus);
    }

    /// <summary>
    /// Get list of selected agvs' graphs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/graph")]
    public async Task<ActionResult<IEnumerable<GraphDto>>> GetAgvGraphs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvGraphs> response = await _getAgvGraphsClient.GetResponse<GotAgvGraphs>(new GetAgvGraphs(correlationIds));
        var graphs = _mapper.Map<List<GraphDto>>(response.Message.Graphs);
        return Ok(graphs);
    }

    /// <summary>
    /// Get list of selected agvs' data.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/data")]
    public async Task<ActionResult<IEnumerable<AgvDataDto>>> GetAgvsData([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsData> response = await _getAgvDataClient.GetResponse<GotAgvsData>(new GetAgvsData(correlationIds));
        var graphs = _mapper.Map<List<AgvDataDto>>(response.Message.AgvData);
        return Ok(graphs);
    }

    /// <summary>
    /// Get list of selected agvs' battery.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/battery")]
    public async Task<ActionResult<IEnumerable<AgvBatteryStatusDto>>> GetAgvsBattery([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvsBattery> response = await _getAgvsBatteryClient.GetResponse<GotAgvsBattery>(new GetAgvsBattery(correlationIds));
        var agvBattery = _mapper.Map<List<AgvBatteryStatusDto>>(response.Message.AgvBatteryStatus);
        return Ok(agvBattery);
    }

    /// <summary>
    /// Get list of selected agvs' current routes.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/currentRoute")]
    public async Task<ActionResult<Dictionary<Guid, CurrentRouteDto?>>> GetAgvRoute([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotCurrentAgvRoutes> response = await _getCurrentAgvRoutesClient.GetResponse<GotCurrentAgvRoutes>(new GetCurrentAgvRoutes(correlationIds));
        var dto = _mapper.Map<Dictionary<Guid, CurrentRouteDto>>(response.Message.AgvRoutesByCorrelationId);
        return Ok(dto);
    }

    /// <summary>
    /// Get mapping of selected agvs' active error lists.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/errors")]
    public async Task<ActionResult<Dictionary<Guid, IEnumerable<AgvErrorDto>>>> GetErrorsForSelectedAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotActiveErrorsForAgvs> response = await _getActiveErrorsClient.GetResponse<GotActiveErrorsForAgvs>(new GetActiveErrorsForAgvs(correlationIds));
        var dto = _mapper.Map<Dictionary<Guid, IEnumerable<AgvErrorDto>>>(response.Message.ErrorsByAgv);
        return Ok(dto);
    }

    /// <summary>
    ///Get mapping of selected agvs' active and solved error lists.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
    /// <response code="200">Ok</response>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    [HttpPost]
    [Route("/agvs/errorHistory")]
    public async Task<ActionResult<Dictionary<Guid, IEnumerable<AgvErrorDto>>>> GetErrorHistoryForSelectedAgvs([FromQuery] ICollection<Guid> correlationIds, DateTime? dateFrom, DateTime? dateTo)
    {
        Response<GotErrorHistoryForAgvs> response = await _getErrorHistoryClient.GetResponse<GotErrorHistoryForAgvs>(new GetErrorHistoryForAgvs(correlationIds, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var dto = _mapper.Map<Dictionary<Guid, IEnumerable<AgvErrorDto>>>(response.Message.ErrorsByAgv);
        return Ok(dto);
    }

    /// <summary>
    /// Retrieve currently loaded Cargos for given Agvs
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/cargos")]
    public async Task<ActionResult<IReadOnlyDictionary<Guid, ICollection<CargoDto>>>> GetCargosOfAgvs([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotCargosOfAgvs> response = await _getCargosOfAgvsClient.GetResponse<GotCargosOfAgvs>(new GetCargosOfAgvs(correlationIds));
        var dto = _mapper.Map<IReadOnlyDictionary<Guid, ICollection<CargoDto>>>(response.Message.CargosByAgvCorrelationId);
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
    /// Get list of selected agvs' settings
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
    [HttpPatch]
    [Route("/agvs/defaultSettings")]
    public async Task<ActionResult> UpdateDefaultSettings([FromBody] AgvSettingsDto updatedSettings)
    {
        await _updateDefaultSettingsClient.GetResponse<Success>(new UpdateDefaultAgvSettings(_mapper.Map<AgvSettingsReadModel>(updatedSettings)));
        return Ok();
    }

    /// <summary>
    /// Update the settings for given agvs. All non-null properties get updated.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="updatedSettings">The updated settings.</param>
    /// <response code="400">One or more of given correlation ids is unknown.</response>
    /// <response code="200">Settings updated.</response>
    [HttpPatch]
    [Route("/agvs/settings")]
    public async Task<ActionResult> UpdateSettings([FromQuery] ICollection<Guid> correlationIds, [FromBody] AgvSettingsDto updatedSettings)
    {
        await _updateAgvsSettingsClient.GetResponse<Success>(new UpdateAgvsSettings(correlationIds, _mapper.Map<AgvSettingsReadModel>(updatedSettings)));
        return Ok();
    }

    /// <summary>
    /// Reset agv settings property to default for all given agvs.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <param name="propertyName">The settings property name to reset to default.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv with given correlationId not found.</response>
    /// <response code="400">Invalid (unknown) agv settings property name.</response>
    [HttpPatch]
    [Route("/agvs/settings/reset")]
    public async Task<ActionResult> ResetAgvsSettingToDefault([FromQuery] ICollection<Guid> correlationIds, [FromQuery] string propertyName)
    {
        await _resetSettingsToDefaultClient.GetResponse<Success>(new ResetAgvsSettingToDefault(correlationIds, propertyName));
        return Ok();
    }
}