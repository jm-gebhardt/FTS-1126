using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActionGroupCreated = Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup.ActionGroupCreated;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Single action group requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupController : Controller
{
    private readonly IRequestClient<GetActionGroup> _getActionGroupRequestClient;
    private readonly IRequestClient<TriggerActionGroupAbortion> _abortionRequestClient;
    private readonly IRequestClient<CancelActionGroup> _cancelActionGroupRequestClient;
    private readonly IRequestClient<TriggerActionGroupReset> _resetActionGroupRequestClient;
    private readonly IRequestClient<CreatePickDropActionGroup> _createActionGroupClient;
    private readonly IRequestClient<TryReassignActionGroup> _reassignActionGroupClient;
    private readonly IRequestClient<TriggerActionCompletion> _actionCompletionClient;
    private readonly IRequestClient<TriggerActionGroupQueueing> _actionGroupQueueingClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    public ActionGroupController(IMapper mapper,
        IRequestClient<GetActionGroup> getActionGroupRequestClient,
        IRequestClient<CancelActionGroup> cancelActionGroupRequestClient,
        IRequestClient<TriggerActionGroupAbortion> abortionRequestClient,
        IRequestClient<TriggerActionGroupReset> resetActionGroupRequestClient,
        IRequestClient<CreatePickDropActionGroup> createActionGroupClient,
        IRequestClient<TryReassignActionGroup> reassignActionGroupClient,
        IRequestClient<TriggerActionCompletion> actionCompletionClient,
            IRequestClient<TriggerActionGroupQueueing> actionGroupQueueingClient)
    {
        _mapper = mapper;
        _getActionGroupRequestClient = getActionGroupRequestClient;
        _abortionRequestClient = abortionRequestClient;
        _cancelActionGroupRequestClient = cancelActionGroupRequestClient;
        _resetActionGroupRequestClient = resetActionGroupRequestClient;
        _createActionGroupClient = createActionGroupClient;
        _reassignActionGroupClient = reassignActionGroupClient;
        _actionCompletionClient = actionCompletionClient;
        _actionGroupQueueingClient = actionGroupQueueingClient;
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

    /// <summary>
    /// Create a pick-drop action group.
    /// </summary>
    /// <param name="pickStation">The pick station.</param>
    /// <param name="dropStation">The drop station.</param>
    /// <param name="agvCorrelationId">The assigned agv (optional).</param>
    /// <returns>The created action group.</returns>
    /// <response code="200">Ok</response>
    /// <response code="400">The given pick or drop station node is not of type pick, drop, or pick drop, respectively.
    /// Or the action group is invalid (e.g. there is no agv that can reach both the pick and drop station.).
    /// Or the given pick or drop station is not found. 
    /// Or the agv is not in maintenance or normal mode.</response>
    /// <response code="404">AGV with given correlationId not found .</response>
    [HttpPost]
    [Route("/actionGroup")]
    public async Task<ActionResult<ActionGroupDto>> CreatePickDropActionGroup(string pickStation, string dropStation, Guid? agvCorrelationId)
    {
        Response<ActionGroupCreated> response = await _createActionGroupClient.GetResponse<ActionGroupCreated>(new CreatePickDropActionGroup(pickStation, dropStation, agvCorrelationId, false, ActionGroupCreator.TransportOrderWebApi));
        return Ok(_mapper.Map<ActionGroupDto>(response.Message.ActionGroup));
    }

    /// <summary>
    /// Re-assign action group to a different available agv.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">The pick action of the action group is not in state started.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/reassign")]
    public async Task<ActionResult> ReassignActionGroup(Guid correlationId)
    {
        await _reassignActionGroupClient.GetResponse<Success>(new TryReassignActionGroup(correlationId));
        return Ok();
    }

    /// <summary>
    /// Complete an action of an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId or the action was not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/action/{actionCorrelationId}/complete")]
    public async Task<ActionResult> CompleteAction(Guid correlationId, Guid actionCorrelationId)
    {
        await _actionCompletionClient.GetResponse<Success>(new TriggerActionCompletion(correlationId, actionCorrelationId));
        return Ok();
    }

    /// <summary>
    /// Queue action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/queue")]
    public async Task<ActionResult> QueueActionGroup(Guid correlationId)
    {
        await _actionGroupQueueingClient.GetResponse<Success>(new TriggerActionGroupQueueing(correlationId));
        return Ok();
    }
}