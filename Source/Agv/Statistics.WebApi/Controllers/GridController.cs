using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.Statistics.WebApi.Controllers;

/// <summary>
/// Single action group requests.
/// </summary>
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class GridController : Controller
{
    private readonly IRequestClient<GetStayDurations> _getStayDurationsClient;
    private readonly IRequestClient<GetErrorFrequencies> _getErrorFrequencyClient;
    private readonly IRequestClient<GetFieldViolationFrequencies> _getFieldViolationFrequencyClient;
    private readonly IRequestClient<GetGrids> _getGridsClient;
    private readonly IRequestClient<AddGrid> _addGridClient;
    private readonly IRequestClient<GetWifiSignalStrength> _getWifiSignalStrengthClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GridController"/>>
    public GridController(
        IMapper mapper,
        IRequestClient<GetGrids> getGridsRequestClient,
        IRequestClient<GetStayDurations> stayDurationsClient,
        IRequestClient<GetErrorFrequencies> getErrorFrequencyClient,
        IRequestClient<AddGrid> addGridRequestClient,
        IRequestClient<GetFieldViolationFrequencies> getFieldViolationFrequencyClient,
        IRequestClient<GetWifiSignalStrength> getWifiSignalStrengthClient)
    {
        _mapper = mapper;
        _getGridsClient = getGridsRequestClient;
        _getStayDurationsClient = stayDurationsClient;
        _getErrorFrequencyClient = getErrorFrequencyClient;
        _addGridClient = addGridRequestClient;
        _getFieldViolationFrequencyClient = getFieldViolationFrequencyClient;
        _getWifiSignalStrengthClient = getWifiSignalStrengthClient;
    }

    /// <summary>
    /// Get all grids.
    /// </summary>
    [HttpGet]
    [Route("/grids/")]
    public async Task<ActionResult<IEnumerable<GridDto>>> GetGrids()
    {
        Response<GotGrids> response = await _getGridsClient.GetResponse<GotGrids>(new GetGrids(Guid.NewGuid()));
        var grids = _mapper.Map<List<GridDto>>(response.Message.Grids);
        return Ok(grids);
    }

    /// <summary>
    /// Add a new grid.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpPatch]
    [Route("/grid/add")]
    public async Task<ActionResult> AddGrid(string name, decimal xSteps, decimal ySteps)
    {
        await _addGridClient.GetResponse<Success>(new AddGrid(name, xSteps, ySteps));
        return Ok();
    }

    /// <summary>
    /// Get the position frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvCorrelationIds">Optional. An array of AGV CorrelationIds to filter the results. If null or empty, results will include all AGVs without filtering.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId, gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/positionFrequency")]
    public async Task<ActionResult<ScalarHeatmapDto>> GetPositionFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds)
    {
        Response<GotStayDurations> response = await _getStayDurationsClient.GetResponse<GotStayDurations>(
            new GetStayDurations
            {
                GridId = gridId,
                MapId = mapId,
                FromDate = fromDate,
                ToDate = toDate,
                AgvCorrelationIds = agvCorrelationIds ?? []
            });
        var positionFrequency = _mapper.Map<ScalarHeatmapDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the error frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvCorrelationIds">Optional. An array of AGV CorrelationIds to filter the results. If null or empty, results will include all AGVs without filtering.</param>
    /// <param name="errorCode">The error code.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId, gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/errorFrequency")]
    public async Task<ActionResult<ScalarHeatmapDto>> GetErrorFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds, string? errorCode)
    {
        Response<GotStayDurations> response = await _getErrorFrequencyClient.GetResponse<GotStayDurations>(
            new GetErrorFrequencies
            {
                GridId = gridId,
                MapId = mapId,
                FromDate = fromDate,
                ToDate = toDate,
                AgvCorrelationIds = agvCorrelationIds ?? [],
                ErrorLevel = ErrorLevel.Fatal,
                ErrorCode = errorCode
            }
        );
        var positionFrequency = _mapper.Map<ScalarHeatmapDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the warning frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvCorrelationIds">Optional. An array of AGV CorrelationIds to filter the results. If null or empty, results will include all AGVs without filtering.</param>
    /// <param name="errorCode">The error code.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId, gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/warningFrequency")]
    public async Task<ActionResult<ScalarHeatmapDto>> GetWarningFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds, string? errorCode)
    {
        Response<GotStayDurations> response = await _getErrorFrequencyClient.GetResponse<GotStayDurations>(
            new GetErrorFrequencies
            {
                GridId = gridId,
                MapId = mapId,
                FromDate = fromDate,
                ToDate = toDate,
                AgvCorrelationIds = agvCorrelationIds ?? [],
                ErrorLevel = ErrorLevel.Warning,
                ErrorCode = errorCode
            });
        var positionFrequency = _mapper.Map<ScalarHeatmapDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the safety field violation frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvCorrelationIds">Optional. An array of AGV CorrelationIds to filter the results. If null or empty, results will include all AGVs without filtering.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId, gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/safetyFieldViolationFrequency")]
    public async Task<ActionResult<ScalarHeatmapDto>> GetFieldViolationFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds)
    {
        Response<GotStayDurations> response = await _getFieldViolationFrequencyClient.GetResponse<GotStayDurations>(
            new GetFieldViolationFrequencies
            {
                GridId = gridId,
                MapId = mapId,
                FromDate = fromDate,
                ToDate = toDate,
                AgvCorrelationIds = agvCorrelationIds ?? []
            });
        var positionFrequency = _mapper.Map<ScalarHeatmapDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the Wi-Fi signal strength [dBm] heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvCorrelationIds">Optional. An array of AGV CorrelationIds to filter the results. If null or empty, results will include all AGVs without filtering.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId, gridId, and signal strength values [[x-index, y-index, dBm], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/wifiSignalStrength")]
    public async Task<ActionResult<ScalarHeatmapDto>> GetWifiSignalStrength([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds)
    {
        Response<GotWifiSignalStrength> response = await _getWifiSignalStrengthClient.GetResponse<GotWifiSignalStrength>(
            new GetWifiSignalStrength
            {
                GridId = gridId,
                MapId = mapId,
                FromDate = fromDate,
                ToDate = toDate,
                AgvCorrelationIds = agvCorrelationIds ?? []
            });
        var signalStrength = _mapper.Map<ScalarHeatmapDto>(response.Message.SignalStrength);
        return Ok(signalStrength);
    }
}