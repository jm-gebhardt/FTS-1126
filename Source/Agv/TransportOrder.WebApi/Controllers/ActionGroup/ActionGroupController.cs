using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
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
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    public ActionGroupController(IMapper mapper,
        IRequestClient<GetActionGroup> getActionGroupRequestClient,
        IRequestClient<CancelActionGroup> cancelActionGroupRequestClient,
        IRequestClient<TriggerActionGroupAbortion> abortionRequestClient,
        IRequestClient<TriggerActionGroupReset> resetActionGroupRequestClient,
        IRequestClient<CreatePickDropActionGroup> createActionGroupClient)
    {
        _mapper = mapper;
        _getActionGroupRequestClient = getActionGroupRequestClient;
        _abortionRequestClient = abortionRequestClient;
        _cancelActionGroupRequestClient = cancelActionGroupRequestClient;
        _resetActionGroupRequestClient = resetActionGroupRequestClient;
        _createActionGroupClient = createActionGroupClient;
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
    /// <response code="400">The given pick or drop station node is not of type pick, drop, or pickdrop, respectively.
    /// Or the action group is invalid (e.g. there is no agv that can reach both the pick and drop station.).
    /// Or the given pick or drop station not found</response>
    /// <response code="404">AGV with given correlationId not found .</response>
    [HttpPost]
    [Route("/actionGroup")]
    public async Task<ActionResult<ActionGroupDto>> CreatePickDropActionGroup(string pickStation, string dropStation, Guid? agvCorrelationId)
    {
        Response<ActionGroupCreated> response = await _createActionGroupClient.GetResponse<ActionGroupCreated>(new CreatePickDropActionGroup(pickStation, dropStation, agvCorrelationId, false, ActionGroupCreator.TransportOrderWebApi));
        return Ok(_mapper.Map<ActionGroupDto>(response.Message.ActionGroup));
    }
}