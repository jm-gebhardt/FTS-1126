using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Single action group requests.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupController : Controller
{
    private readonly IRequestClient<GetActionGroup> _getActionGroupRequestClient;
    private readonly IRequestClient<TriggerActionGroupAbortion> _abortionRequestClient;
    private readonly IRequestClient<CancelActionGroup> _cancelActionGroupRequestClient;
    private readonly IRequestClient<TriggerActionGroupReset> _resetActionGroupRequestClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    public ActionGroupController(IMapper mapper, 
        IRequestClient<GetActionGroup> getActionGroupRequestClient, 
        IRequestClient<CancelActionGroup> cancelActionGroupRequestClient, 
        IRequestClient<TriggerActionGroupAbortion> abortionRequestClient,
        IRequestClient<TriggerActionGroupReset> resetActionGroupRequestClient)
    {
        _mapper = mapper;
        _getActionGroupRequestClient = getActionGroupRequestClient;
        _abortionRequestClient = abortionRequestClient;
        _cancelActionGroupRequestClient = cancelActionGroupRequestClient;
        _resetActionGroupRequestClient = resetActionGroupRequestClient;
    }

    /// <summary>
    /// Get action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    [HttpGet]
    [Route("/actionGroup/{correlationId:guid}")]
    public async Task<ActionResult<ActionGroupDto>> GetActionGroup(Guid correlationId)
    {
        Response<GotAgvActionGroup> response = await _getActionGroupRequestClient.GetResponse<GotAgvActionGroup>(new GetActionGroup(correlationId));
        var actionGroup = _mapper.Map<ActionGroupDto>(response.Message.AgvActionGroup);
        return Ok(actionGroup!);
    }

    /// <summary>
    /// Abort an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">Action group abortion failed because of the current group state (details in error message).</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/abort")]
    public async Task<ActionResult> AbortActionGroup(Guid correlationId)
    {
        await _abortionRequestClient.GetResponse<ActionGroupAbortionTriggered>(new TriggerActionGroupAbortion(correlationId, AbortReason.WebApi));
        return Ok();
    }

    /// <summary>
    /// Cancel an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">Action group cancellation failed because of the current group state (details in error message).</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/cancel")]
    public async Task<ActionResult> CancelActionGroup(Guid correlationId)
    {
        Response<ActionGroupCanceled, ActionGroupCancelFailed> response = await _cancelActionGroupRequestClient.GetResponse<ActionGroupCanceled, ActionGroupCancelFailed>(new CancelActionGroup(correlationId, CancelReason.WebApi));
        return response.Message switch
        {
            ActionGroupCanceled => Ok(),
            ActionGroupCancelFailed { ErrorCode: HostOrderCancelFailedErrorCode.OrderDoesNotExist } => NotFound(),
            _ => BadRequest(((ActionGroupCancelFailed)response.Message).ErrorCode)
        };
    }

    /// <summary>
    /// Reset an action group, so that it can be picked by an agv again.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/reset")]
    public async Task<ActionResult> ResetActionGroup(Guid correlationId)
    {
        await _resetActionGroupRequestClient.GetResponse<ActionGroupResetTriggered>(new TriggerActionGroupReset(correlationId));
        return Ok();
    }
}