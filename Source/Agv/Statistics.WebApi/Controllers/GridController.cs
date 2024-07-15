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
    private readonly IRequestClient<GetPositionFrequencies> _getPositionFrequencyClient;
    private readonly IRequestClient<GetErrorFrequencies> _getErrorFrequencyClient;
    private readonly IRequestClient<GetFieldViolationFrequencies> _getFieldViolationFrequencyClient;
    private readonly IRequestClient<GetGrids> _getGridsClient;
    private readonly IRequestClient<AddGrid> _addGridClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GridController"/>>
    public GridController(
        IMapper mapper,
        IRequestClient<GetGrids> getGridsRequestClient,
        IRequestClient<GetPositionFrequencies> positionFrequencyClient,
        IRequestClient<GetErrorFrequencies> getErrorFrequencyClient,
        IRequestClient<AddGrid> addGridRequestClient,
        IRequestClient<GetFieldViolationFrequencies> getFieldViolationFrequencyClient)
    {
        _mapper = mapper;
        _getGridsClient = getGridsRequestClient;
        _getPositionFrequencyClient = positionFrequencyClient;
        _getErrorFrequencyClient = getErrorFrequencyClient;
        _addGridClient = addGridRequestClient;
        _getFieldViolationFrequencyClient = getFieldViolationFrequencyClient;
    }

    /// <summary>
    /// Get all grids.
    /// </summary>
    [HttpGet]
    [Route("/grids/")]
    public async Task<ActionResult<IEnumerable<GridDto>>> GetGrids()
    {
        Response<GotGrids> response = await _getGridsClient.GetResponse<GotGrids>(new GetGrids());
        var grids = _mapper.Map<List<GridDto>>(response.Message.Grids);
        return Ok(grids);
    }

    /// <summary>
    /// Add a new grid.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpPatch]
    [Route("/grid/add")]
    public async Task<ActionResult> AddGrid(string name, float xSteps, float ySteps)
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
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId,gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/positionFrequency")]
    public async Task<ActionResult<FrequencyDto>> GetPositionFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate)
    {
        Response<GotPositionFrequencies> response = await _getPositionFrequencyClient.GetResponse<GotPositionFrequencies>(
            new GetPositionFrequencies(gridId, mapId, fromDate, toDate));
        var positionFrequency = _mapper.Map<FrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the error frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvSerialNumbers">Serial numbers of agvs (optional) for which the errors occured.</param>
    /// <param name="errorCode">The error code.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId,gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/errorFrequency")]
    public async Task<ActionResult<FrequencyDto>> GetErrorFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, string[]? agvSerialNumbers, string? errorCode)
    {
        Response<GotPositionFrequencies> response = await _getErrorFrequencyClient.GetResponse<GotPositionFrequencies>(
            new GetErrorFrequencies(gridId,
                mapId,
                fromDate,
                toDate,
                agvSerialNumbers ?? [],
                ErrorLevel.Fatal,
                errorCode));
        var positionFrequency = _mapper.Map<FrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the warning frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvSerialNumbers">Serial numbers of agvs (optional) for which the errors occured.</param>
    /// <param name="errorCode">The error code.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId,gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/warningFrequency")]
    public async Task<ActionResult<FrequencyDto>> GetWarningFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, string[]? agvSerialNumbers, string? errorCode)
    {
        Response<GotPositionFrequencies> response = await _getErrorFrequencyClient.GetResponse<GotPositionFrequencies>(
            new GetErrorFrequencies(gridId,
                mapId,
                fromDate,
                toDate,
                agvSerialNumbers ?? [],
                ErrorLevel.Warning,
                errorCode));
        var positionFrequency = _mapper.Map<FrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }

    /// <summary>
    /// Get the safety field violation frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="agvSerialNumbers">Serial numbers of agvs (optional) for which the errors occured.</param>
    /// <response code="200">Ok</response>
    /// <returns>Object containing mapId,gridId, and frequencies [[x-index, y-index, count], ...]</returns>
    [HttpGet]
    [Route("/grid/{gridId:guid}/safetyFieldViolationFrequency")]
    public async Task<ActionResult<FrequencyDto>> GetFieldViolationFrequency([Required] Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate, string[]? agvSerialNumbers)
    {
        Response<GotPositionFrequencies> response = await _getFieldViolationFrequencyClient.GetResponse<GotPositionFrequencies>(
            new GetFieldViolationFrequencies(gridId,
                mapId,
                fromDate,
                toDate,
                agvSerialNumbers ?? []));
        var positionFrequency = _mapper.Map<FrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }
}