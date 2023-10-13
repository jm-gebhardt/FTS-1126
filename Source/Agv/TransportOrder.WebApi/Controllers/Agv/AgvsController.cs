using AutoMapper;
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
    private readonly IRequestClient<GetAgvRoutes> _getAgvRoutesClient;
    private readonly IRequestClient<GetAgvPositions> _getAgvPositionsClient;
    private readonly IRequestClient<GetAgvGraphs> _getAgvGraphsClient;
    private readonly IRequestClient<GetAgvsData> _getAgvDataClient;
    private readonly IRequestClient<GetActiveErrorsForAgvs> _getActiveErrorsClient;
    private readonly IRequestClient<GetErrorHistoryForAgvs> _getErrorHistoryClient;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AgvsController"/> instance.
    /// </summary>
    public AgvsController(IMapper mapper,
        IRequestClient<GetAgvs> getAgvsClient,
        IRequestClient<GetAgvRoutes> getAgvRoutesClient,
        IRequestClient<GetAgvPositions> getAgvPositionsClient,
        IRequestClient<GetAgvGraphs> getAgvGraphsClient,
        IRequestClient<GetAgvsData> getAgvDataClient,
        IRequestClient<GetActiveErrorsForAgvs> getActiveErrorsClient,
        IRequestClient<GetErrorHistoryForAgvs> getErrorHistoryClient)
    {
        _getAgvsClient = getAgvsClient;
        _getAgvRoutesClient = getAgvRoutesClient;
        _getAgvPositionsClient = getAgvPositionsClient;
        _mapper = mapper;
        _getAgvGraphsClient = getAgvGraphsClient;
        _getAgvDataClient = getAgvDataClient;
        _getActiveErrorsClient = getActiveErrorsClient;
        _getErrorHistoryClient = getErrorHistoryClient;
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
    /// Get list of selected agvs' remaining routes.
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <returns></returns>
    [HttpPost]
    [Route("/agvs/route")]
    public async Task<ActionResult<Dictionary<Guid, AgvRouteDto?>>> GetAgvRoute([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvRoutes> response = await _getAgvRoutesClient.GetResponse<GotAgvRoutes>(new GetAgvRoutes(correlationIds));
        var dto = _mapper.Map<Dictionary<Guid, AgvRouteDto>>(response.Message.AgvRoutesByCorrelationId);
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
        Response<GotErrorHistoryForAgvs> response = await _getErrorHistoryClient.GetResponse<GotErrorHistoryForAgvs>(new GetErrorHistoryForAgvs(correlationIds, dateFrom?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var dto = _mapper.Map<Dictionary<Guid, IEnumerable<AgvErrorDto>>>(response.Message.ErrorsByAgv);
        return Ok(dto);
    }
}