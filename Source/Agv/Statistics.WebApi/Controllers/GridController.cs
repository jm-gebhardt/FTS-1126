using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.Statistics.WebApi.Controllers;

/// <summary>
/// Single action group requests.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class GridController : Controller
{
    private readonly IRequestClient<GetPositionFrequencies> _getPositionFrequencyClient;
    private readonly IRequestClient<GetGrids> _getGridsClient;
    private readonly IRequestClient<AddGrid> _addGridClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GridController"/>>
    public GridController(
        IMapper mapper,
        IRequestClient<GetGrids> getGridsRequestClient,
        IRequestClient<GetPositionFrequencies> positionFrequencyClient,
        IRequestClient<AddGrid> addGridRequestClient)
    {
        _mapper = mapper;
        _getGridsClient = getGridsRequestClient;
        _getPositionFrequencyClient = positionFrequencyClient;
        _addGridClient = addGridRequestClient;
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
    [HttpGet]
    [Route("/grid/{gridId:guid}/positionFrequency")]
    public async Task<ActionResult<PositionFrequencyDto>> GetPositionFrequency(Guid gridId, [Required] Guid mapId, DateTime? fromDate, DateTime? toDate)
    {
        Response<GotPositionFrequencies> response = await _getPositionFrequencyClient.GetResponse<GotPositionFrequencies>(
            new GetPositionFrequencies(gridId, mapId, fromDate, toDate));
        var positionFrequency = _mapper.Map<PositionFrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }
}