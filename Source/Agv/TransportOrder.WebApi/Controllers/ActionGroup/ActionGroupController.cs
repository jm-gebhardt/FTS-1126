using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
    private readonly IRequestClient<TriggerSetTarget> _setTargetClient;
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
        IRequestClient<TriggerActionGroupQueueing> actionGroupQueueingClient,
        IRequestClient<TriggerSetTarget> setTargetClient)
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
        _setTargetClient = setTargetClient;
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
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">Action group abortion failed because of the current group state (details in error message).</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/abort")]
    public async Task<ActionResult> AbortActionGroup(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _abortionRequestClient.GetResponse<ActionGroupAbortionTriggered>(new TriggerActionGroupAbortion(correlationId, AbortReason.WebApi, email));
        return Ok();
    }

    /// <summary>
    /// Cancel an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">Action group cancellation failed because of the current group state (details in error message).</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/cancel")]
    public async Task<ActionResult> CancelActionGroup(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<ActionGroupCanceled, ActionGroupCancelFailed> response = await _cancelActionGroupRequestClient.GetResponse<ActionGroupCanceled, ActionGroupCancelFailed>(new CancelActionGroup(correlationId, CancelReason.WebApi, User.GetEmail()));
        return response.Message switch
        {
            ActionGroupCanceled => Ok(),
            ActionGroupCancelFailed { ErrorCode: HostOrderCancelFailedErrorCode.OrderDoesNotExist } => NotFound(),
            _ => BadRequest(((ActionGroupCancelFailed) response.Message).ErrorCode)
        };
    }

    /// <summary>
    /// Reset an action group, so that it can be picked by an agv again.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the action group.</param>
    /// <param name="keepAgv">Optional parameter if the same agv should be kept assigned to the action group. Defaults to false.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/reset")]
    public async Task<ActionResult> ResetActionGroup(Guid correlationId, bool keepAgv = false)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _resetActionGroupRequestClient.GetResponse<ActionGroupResetTriggered>(new TriggerActionGroupReset(correlationId, keepAgv, email));
        return Ok();
    }

    /// <summary>
    /// Create a pick-drop action group.
    /// </summary>
    /// <param name="pickStation">The pick station.</param>
    /// <param name="dropStation">The drop station.</param>
    /// <param name="dockFrontPick">Whether to dock front for the pick.</param>
    /// <param name="dockFrontDrop">Whether to dock front for the drop.</param>
    /// <param name="handlingUnit">The handling unit of the action group.</param>
    /// <param name="additionalPickInfo">Additional info for the pick station (optional).</param>
    /// <param name="additionalDropInfo">Additional info for the drop station (optional).</param>
    /// <param name="agvCorrelationId">The assigned agv (optional).</param>
    /// <returns>The created action group.</returns>
    /// <response code="200">Ok</response>
    /// <response code="400">The given pick or drop station node is not of type pick, drop, or pick drop, respectively.
    /// Or the action group is invalid (e.g. there is no agv that can reach both the pick and drop station.).
    /// Or the given pick or drop station is not found. 
    /// Or the agv is not in maintenance or normal mode.
    /// Or no source or target is given.
    /// Or only a source or only a target is given, but no assigned agv.</response>
    /// <response code="401">Unauthorized .</response>
    /// <response code="404">AGV with given correlationId not found .</response>
    [HttpPost]
    [Route("/actionGroup")]
    public async Task<ActionResult<ActionGroupDto>> CreatePickDropActionGroup(
        string? pickStation,
        string? dropStation,
        bool dockFrontPick,
        bool dockFrontDrop,
        string? handlingUnit,
        string? additionalPickInfo,
        string? additionalDropInfo,
        Guid? agvCorrelationId
    )
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        Response<ActionGroupCreated> response = await _createActionGroupClient.GetResponse<ActionGroupCreated>(new CreatePickDropActionGroup(
            pickStation?.ToGuid(),
            dropStation?.ToGuid(),
            dockFrontPick,
            dockFrontDrop,
            handlingUnit,
            agvCorrelationId,
            additionalPickInfo,
            additionalDropInfo,
            false,
            ActionGroupCreator.TransportOrderWebApi, email));
        return Ok(_mapper.Map<ActionGroupDto>(response.Message.ActionGroup));
    }

    /// <summary>
    /// Re-assign action group to a different available agv.
    /// Optionally, provide the one to assign it to.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">The pick action of the action group is not in state started.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/reassign")]
    public async Task<ActionResult> ReassignActionGroup(Guid correlationId, [FromQuery] Guid? assignedAgv)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _reassignActionGroupClient.GetResponse<Success>(new TryReassignActionGroup(correlationId, assignedAgv, email));
        return Ok();
    }

    /// <summary>
    /// Complete an action of an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId or the action was not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/action/{actionCorrelationId}/complete")]
    public async Task<ActionResult> CompleteAction(Guid correlationId, Guid actionCorrelationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _actionCompletionClient.GetResponse<Success>(new TriggerActionCompletion(correlationId, actionCorrelationId, email));
        return Ok();
    }

    /// <summary>
    /// Queue action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/queue")]
    public async Task<ActionResult> QueueActionGroup(Guid correlationId)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _actionGroupQueueingClient.GetResponse<Success>(new TriggerActionGroupQueueing(correlationId, email));
        return Ok();
    }

    /// <summary>
    /// Set target for action of action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Action group or action with given correlationId not found or the given target node has not been found.</response>
    /// <response code="400">The action has already been completed or the target node is not of type Pick, Drop, or PickDrop.</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/action/{actionCorrelationId:guid}/setTarget")]
    public async Task<ActionResult> SetTarget(Guid correlationId, Guid actionCorrelationId, [FromQuery] [Required] string targetNode)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await _setTargetClient.GetResponse<Success>(new TriggerSetTarget(correlationId, actionCorrelationId, targetNode.ToGuid(), email));
        return Ok();
    }
}