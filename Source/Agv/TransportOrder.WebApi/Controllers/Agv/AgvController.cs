using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Agv;

/// <summary>
/// Single agv requests.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvController : Controller
{
    private readonly IRequestClient<GetAgv> _getAgvClient;
    private readonly IRequestClient<GetAgvGraph> _getAgvGraphClient;
    private readonly IRequestClient<GetAgvData> _getAgvDataClient;
    private readonly IRequestClient<GetCurrentAgvRoute> _getCurrentAgvRouteClient;
    private readonly IRequestClient<GetAgvPosition> _getAgvPositionClient;
    private readonly IRequestClient<AcceptAgvCommand> _acceptAgvClient;
    private readonly IRequestClient<UnlockAgv> _unlockAgvClient;
    private readonly IRequestClient<LockAgv> _lockAgvClient;
    private readonly IRequestClient<RequestAgvData> _emitRequestDataClient;
    private readonly IRequestClient<RequestAgvGraph> _emitRequestGraphClient;
    private readonly IRequestClient<GetActiveAgvErrors> _getAgvErrorsRequestClient;
    private readonly IRequestClient<GetAgvErrors> _getAgvErrorHistoryRequestClient;
    private readonly IRequestClient<MoveAgv> _moveAgvClient;
    private readonly IRequestClient<SetAgvForceIdle> _setForceIdleClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Init controller
    /// </summary>
    public AgvController(IMapper mapper, IRequestClient<GetAgv> getAgvClient,
        IRequestClient<GetAgvGraph> getAgvGraphClient,
        IRequestClient<GetAgvData> getAgvDataClient,
        IRequestClient<GetCurrentAgvRoute> getCurrentAgvRouteClient,
        IRequestClient<GetAgvPosition> getAgvPositionClient,
        IRequestClient<AcceptAgvCommand> acceptAgvClient,
        IRequestClient<UnlockAgv> unlockAgvClient,
        IRequestClient<LockAgv> lockAgvClient,
        IRequestClient<RequestAgvData> emitRequestDataClient,
        IRequestClient<RequestAgvGraph> emitRequestGraphClient,
        IRequestClient<GetActiveAgvErrors> getAgvErrorsRequestClient,
        IRequestClient<GetAgvErrors> getAgvErrorHistoryRequestClient,
        IRequestClient<MoveAgv> moveAgvClient,
        IRequestClient<SetAgvForceIdle> setForceIdleClient)
    {
        _getAgvClient = getAgvClient;
        _getAgvGraphClient = getAgvGraphClient;
        _getAgvDataClient = getAgvDataClient;
        _getCurrentAgvRouteClient = getCurrentAgvRouteClient;
        _getAgvPositionClient = getAgvPositionClient;
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
    /// Get agv.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">AGV with given correlationId not found.</response>
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
    /// <response code="404">AGV with given correlationId not found</response>
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
    [HttpGet]
    [Route("/agv/{correlationId:guid}/data")]
    public async Task<ActionResult<AgvDataDto>> GetAgvData(Guid correlationId)
    {
        Response<GotAgvData> response = await _getAgvDataClient.GetResponse<GotAgvData>(new GetAgvData(correlationId));
        var agvData = _mapper.Map<AgvDataDto>(response.Message.AgvData);
        return Ok(agvData);
    }

    /// <summary>
    /// Get remaining agv route.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <returns></returns>
    /// <response code="200">AgvRoute</response>
    /// <response code="204">No Agv or running ActionGroup existing</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/route")]
    public async Task<ActionResult<CurrentRouteDto?>> GetAgvRoute(Guid correlationId)
    {
        Response<GotCurrentAgvRoute> response = await _getCurrentAgvRouteClient.GetResponse<GotCurrentAgvRoute>(new GetCurrentAgvRoute(correlationId));
        var dto = _mapper.Map<CurrentRouteDto>(response.Message.AgvRoute);
        return Ok(dto);
    }

    /// <summary>
    /// Get agv position.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">AGV with given correlationId not found.</response>
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
    /// <returns></returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/errors")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrors(Guid correlationId)
    {
        Response<GotActiveAgvErrors> response = await _getAgvErrorsRequestClient.GetResponse<GotActiveAgvErrors>(new GetActiveAgvErrors(correlationId));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Get list of active and solved errors of an agv.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
    /// <returns></returns>
    [HttpGet]
    [Route("/agv/{correlationId:guid}/errorHistory")]
    public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrorHistory(Guid correlationId, DateTime? dateFrom, DateTime? dateTo)
    {
        Response<GotAgvErrors> response = await _getAgvErrorHistoryRequestClient.GetResponse<GotAgvErrors>(new GetAgvErrors(correlationId, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
        var errors = _mapper.Map<List<AgvErrorDto>>(response.Message.AgvErrors);
        return Ok(errors);
    }

    /// <summary>
    /// Accept discovered AGV to allow it to be assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: AGV status was successfully set to Idle</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <response code="409">Conflict: AGV is not in state 'Discovered'</response>
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
    /// Unlock AGV to allow it to be assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: AGV was successfully unlocked</response>
    /// <response code="404">AGV with given correlationId not found.</response>
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
    /// Lock AGV to prevent it from being assigned to transport orders.
    /// </summary>
    /// <response code="200">Ok: AGV was successfully locked</response>
    /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/lock")]
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
    /// Emit RequestAgvData message.
    /// </summary>
    /// <response code="200">Ok: Telegram was successfully emitted</response>
    /// <response code="404">AGV with given correlationId not found.</response>
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
    /// <response code="404">AGV with given correlationId not found.</response>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/emitRequestGraph")]
    public async Task<ActionResult> EmitRequestGraph(Guid correlationId)
    {
        await _emitRequestGraphClient.GetResponse<RequestAgvGraphResult>(new RequestAgvGraph(correlationId));
        return Ok();
    }

    /// <summary>
    /// Move Agv manually to a specific node. 
    /// </summary>
    /// <response code="200">Ok: The ActionGroup for the move was created and is going to be processed</response>
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The CorrelationId of the created ActionGroup</returns>
    [HttpPatch]
    [Route("/agv/{correlationId:guid}/move")]
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
    /// <response code="404">AGV with given correlationId not found.</response>
    /// <returns>The correlation id of the agv.</returns>
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
}