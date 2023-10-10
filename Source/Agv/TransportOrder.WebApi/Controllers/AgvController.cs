using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;

/// <summary>
/// Agv controller
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvController : Controller
{
    private readonly IRequestClient<GetAgv> _getAgvClient;
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IRequestClient<GetAgvGraph> _getAgvGraphClient;
    private readonly IRequestClient<GetAgvDataRequest> _getAgvDataClient;
    private readonly IRequestClient<GetAgvRoute> _getAgvRouteClient;
    private readonly IRequestClient<GetAgvRoutes> _getAgvRoutesClient;
    private readonly IRequestClient<GetAgvPositions> _getAgvPositionsClient;
    private readonly IRequestClient<AcceptAgvCommand> _acceptAgvClient;
    private readonly IRequestClient<UnlockAgv> _unlockAgvClient;
    private readonly IRequestClient<LockAgv> _lockAgvClient;
    private readonly IRequestClient<RequestAgvData> _emitRequestDataClient;
    private readonly IRequestClient<RequestAgvGraph> _emitRequestGraphClient;
    private readonly IRequestClient<GetActiveAgvErrorsRequest> _getAgvErrorsRequestClient;
    private readonly IRequestClient<GetAgvErrorsRequest> _getAgvErrorHistoryRequestClient;
    private readonly IRequestClient<MoveAgv> _moveAgvClient;
    private readonly IRequestClient<SetAgvForceIdle> _setForceIdleClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Init controller
    /// </summary>
    public AgvController(IMapper mapper, IRequestClient<GetAgv> getAgvClient,
        IRequestClient<GetAgvs> getAgvsClient,
        IRequestClient<GetAgvGraph> getAgvGraphClient,
        IRequestClient<GetAgvDataRequest> getAgvDataClient,
        IRequestClient<GetAgvRoute> getAgvRouteClient,
        IRequestClient<GetAgvRoutes> getAgvRoutesClient,
        IRequestClient<GetAgvPositions> getAgvPositionsClient,
        IRequestClient<AcceptAgvCommand> acceptAgvClient,
        IRequestClient<UnlockAgv> unlockAgvClient,
        IRequestClient<LockAgv> lockAgvClient,
        IRequestClient<RequestAgvData> emitRequestDataClient,
        IRequestClient<RequestAgvGraph> emitRequestGraphClient,
        IRequestClient<GetActiveAgvErrorsRequest> getAgvErrorsRequestClient,
        IRequestClient<GetAgvErrorsRequest> getAgvErrorHistoryRequestClient,
        IRequestClient<MoveAgv> moveAgvClient,
        IRequestClient<SetAgvForceIdle> setForceIdleClient)
    {
        _getAgvClient = getAgvClient;
        _getAgvsClient = getAgvsClient;
        _getAgvGraphClient = getAgvGraphClient;
        _getAgvDataClient = getAgvDataClient;
        _getAgvRouteClient = getAgvRouteClient;
        _getAgvRoutesClient = getAgvRoutesClient;
        _getAgvPositionsClient = getAgvPositionsClient;
        _acceptAgvClient = acceptAgvClient;
        _unlockAgvClient = unlockAgvClient;
        _lockAgvClient = lockAgvClient;
        _emitRequestDataClient = emitRequestDataClient;
        _emitRequestGraphClient = emitRequestGraphClient;
        _getAgvErrorsRequestClient = getAgvErrorsRequestClient;
        _getAgvErrorHistoryRequestClient = getAgvErrorHistoryRequestClient;
        _moveAgvClient = moveAgvClient;
        _setForceIdleClient = setForceIdleClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieve paged list of agvs
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
    /// Retrieve agv by correlationId
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">AGV with given correlationId doesn't exist</response>
    [HttpGet]
    [Route("/agvs/{correlationId}")]
    public async Task<ActionResult<AgvDto>> GetAgv(Guid correlationId)
    {
        Response<GotAgv> response = await _getAgvClient.GetResponse<GotAgv>(new GetAgv(correlationId));
        var agv = _mapper.Map<AgvDto>(response.Message.Agv);
        return Ok(agv);
    }

    /// <summary>
    /// Retrieve graph for agv
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No content</response>
    /// <response code="404">Not found</response>
    [HttpGet]
    [Route("/agvs/{correlationId}/graph")]
    public async Task<ActionResult<GraphDto>> GetAgvGraph(Guid correlationId)
    {
        Response<GotAgvGraph> response = await _getAgvGraphClient.GetResponse<GotAgvGraph>(new GetAgvGraph(correlationId));
        var graph = _mapper.Map<GraphDto>(response.Message.AgvGraph);
        return Ok(graph);
    }

    /// <summary>
    /// Retrieve data for agv
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="204">No Content</response>
    /// <response code="404">Not found</response>
    [HttpGet]
    [Route("/agvs/{correlationId}/data")]
    public async Task<ActionResult<AgvDataDto>> GetAgvData(Guid correlationId)
    {
        Response<GetAgvDataResult> response = await _getAgvDataClient.GetResponse<GetAgvDataResult>(new GetAgvDataRequest(correlationId));
        var agvData = _mapper.Map<AgvDataDto>(response.Message.AgvData);
        return Ok(agvData);
    }

    /// <summary>
    /// Retrieve current routes of the Agvs. It is null when the agv has completed the last action.
    /// It will deliver the route of the Agv is existing
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    /// <response code="200">AgvRoute</response>
    /// <response code="204">No Agv or running ActionGroup existing</response>
    [HttpGet]
    [Route("/agv/{correlationId}/route")]
    public async Task<ActionResult<AgvRouteDto?>> GetAgvRoute(Guid correlationId)
    {
        Response<GotAgvRoute> response = await _getAgvRouteClient.GetResponse<GotAgvRoute>(new GetAgvRoute(correlationId));
        var dto = _mapper.Map<AgvRouteDto>(response.Message.AgvRoute);
        return Ok(dto);
    }

    /// <summary>
    /// Retrieve list of agv positions
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/agvsPositions")]
    public async Task<ActionResult<IEnumerable<AgvPositionStatusDto>>> GetAgvPositions()
    {
        Response<GotAgvPositions> response = await _getAgvPositionsClient.GetResponse<GotAgvPositions>(new GetAgvPositions());

        var positionStatus = _mapper.Map<List<AgvPositionStatusDto>>(response.Message.AgvPositions);
        return Ok(positionStatus);
    }

    /// <summary>
    /// Retrieve current route of Agv. It is null when the agv has completed the last action.
    /// It will deliver the route of the Agv is existing
    /// </summary>
    /// <param name="correlationIds">Only Agvs contained in this list will be considered</param>
    /// <returns></returns>
    [HttpGet]
    [Route("/agvs/route")]
    public async Task<ActionResult<Dictionary<Guid, AgvRouteDto?>>> GetAgvRoute([FromQuery] ICollection<Guid> correlationIds)
    {
        Response<GotAgvRoutes> response = await _getAgvRoutesClient.GetResponse<GotAgvRoutes>(new GetAgvRoutes(correlationIds));
        var dto = _mapper.Map<Dictionary<Guid, AgvRouteDto>>(response.Message.AgvRoutesByCorrelationId);
        return Ok(dto);
    }

    /// <summary>
    /// Accept discovered AGV to allow it to be assigned to transport orders
    /// </summary>
    /// <response code="200">Ok: AGV status was successfully set to Idle</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    /// <response code="409">Conflict: AGV is not in state 'Discovered'</response>
    [HttpPatch]
    [Route("/agvs/{correlationId}/accept")]
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
    /// Unlock AGV to allow it to be assigned to transport orders
    /// </summary>
    /// <response code="200">Ok: AGV was successfully unlocked</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agvs/{correlationId}/unlock")]
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
    /// Lock AGV to prevent it from being assigned to transport orders
    /// </summary>
    /// <response code="200">Ok: AGV was successfully locked</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agvs/{correlationId}/lock")]
    public async Task<ActionResult> LockAgv(Guid correlationId)
    {
        Response<LockAgvResult, AgvNotFoundResult> response = await _lockAgvClient.GetResponse<LockAgvResult, AgvNotFoundResult>(new LockAgv(correlationId));
        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Emit RequestData telegram when receiving PATCH request
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agvs/{correlationId}/emitRequestData")]
    public async Task<ActionResult> EmitRequestData(Guid correlationId)
    {
        await _emitRequestDataClient.GetResponse<GotAgvData>(new RequestAgvData(correlationId));
        return Ok();
    }

    /// <summary>
    /// Emit RequestGraph telegram when receiving PATCH request
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agvs/{correlationId}/emitRequestGraph")]
    public async Task<ActionResult> EmitRequestGraph(Guid correlationId)
    {
        await _emitRequestGraphClient.GetResponse<RequestAgvGraphResult>(new RequestAgvGraph(correlationId));
        return Ok();
    }

    /// <summary>
    /// Get active errors for given Agv
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/agvs/{agvCorrelationId}/errors")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrors(Guid agvCorrelationId)
    {
        Response<GetActiveAgvErrorsResult> response = await _getAgvErrorsRequestClient.GetResponse<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Get all historic (solved and unsolved) errors for given Agv including resolved errors
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
    /// <returns></returns>
    [HttpGet]
    [Route("/agvs/{agvCorrelationId}/errorHistory")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrorHistory(Guid agvCorrelationId, DateTime? dateFrom, DateTime? dateTo)
    {
        Response<GetAgvErrorsResult> response = await _getAgvErrorHistoryRequestClient.GetResponse<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Move Agv manually to a specific node. This will be executed as the next ActionGroup
    /// </summary>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="404">Not Found: Agv with the given correlationId does not exist</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPost]
    [Route("/agvs/{correlationId}/move")]
    public async Task<ActionResult> MoveAgv(Guid correlationId, string destinationNodeId)
    {
        Response<MoveAgvResult, AgvNotFoundResult> response = await _moveAgvClient.GetResponse<MoveAgvResult, AgvNotFoundResult>(new MoveAgv(correlationId, destinationNodeId, ActionGroupCreator.TransportOrderWebApi));
   
        if (response.Message is MoveAgvResult moveAgvResult)
        {
            return Ok(moveAgvResult.ActionGroupCorrelationId);
        }
        return NotFound();
    }

    /// <summary>
    /// Set an agv to idle by force.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="200">Ok: The agv was set to idle.</response>
    /// <response code="404">Not Found: An agv with the given correlationId does not exist</response>
    /// <returns>The correlation id of the agv.</returns>
    [HttpPost]
    [Route("/agvs/{correlationId}/setForceIdle")]
    public async Task<ActionResult> SetForceIdle(Guid correlationId)
    {
        Response<SetAgvForceIdleResult, AgvNotFoundResult> response= await _setForceIdleClient.GetResponse<SetAgvForceIdleResult, AgvNotFoundResult>(new SetAgvForceIdle(correlationId));
        if (response.Message is AgvNotFoundResult)
        {
            return NotFound();
        }
        return Ok();
    }
}