using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Statistics.Messages.Requests;
using Statistics.WebApi.Dtos;

namespace Statistics.WebApi.Controllers;

/// <summary>
/// Single action group requests.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class GridController : Controller
{
    private readonly IRequestClient<GetPositionFrequenciesForGrid> _getPositionFrequencyClient;
    private readonly IRequestClient<GetGrids> _getGridsClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GridController"/>>
    public GridController(
        IMapper mapper,
        IRequestClient<GetGrids> getGridsRequestClient, 
        IRequestClient<GetPositionFrequenciesForGrid> positionFrequencyClient)
        {
            _mapper = mapper;
            _getGridsClient = getGridsRequestClient;
            _getPositionFrequencyClient = positionFrequencyClient;
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
    /// Get the position frequencies heatmap for the given grid.
    /// </summary>
    /// <param name="gridId">The grid id.</param>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/grid/{gridId:guid}/positionFrequency")]
    public async Task<ActionResult<PositionFrequencyDto>> GetPositionFrequency(Guid gridId)
    {
        Response<GotPositionFrequenciesForGrid> response = await _getPositionFrequencyClient.GetResponse<GotPositionFrequenciesForGrid>(new GetPositionFrequenciesForGrid(gridId));
        var positionFrequency = _mapper.Map<PositionFrequencyDto>(response.Message.PositionFrequency);
        return Ok(positionFrequency);
    }
}