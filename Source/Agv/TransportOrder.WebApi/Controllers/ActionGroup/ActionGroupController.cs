using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
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
    private readonly IRequestClient<CancelActionGroup> _cancelActionGroupRequestClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    public ActionGroupController(IMapper mapper, IRequestClient<GetActionGroup> getActionGroupRequestClient, IRequestClient<CancelActionGroup> cancelActionGroupRequestClient)
    {
        _mapper = mapper;
        _getActionGroupRequestClient = getActionGroupRequestClient;
        _cancelActionGroupRequestClient = cancelActionGroupRequestClient;
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
    /// Cancel an action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId not found.</response>
    /// <response code="400">Action group cancellation failed because of the current group state (details in error message).</response>
    [HttpPatch]
    [Route("/actionGroup/{correlationId:guid}/cancel")]
    public async Task<ActionResult> CancelActionGroup(Guid correlationId)
    {
        Response<ActionGroupCancelled, ActionGroupCancelFailed> response = await _cancelActionGroupRequestClient.GetResponse<ActionGroupCancelled, ActionGroupCancelFailed>(new CancelActionGroup(correlationId));
        return response.Message switch
        {
            ActionGroupCancelled => Ok(),
            ActionGroupCancelFailed { ErrorCode: HostOrderCancelFailedErrorCode.OrderDoesNotExist } => NotFound(),
            _ => BadRequest(((ActionGroupCancelFailed)response.Message).ErrorCode)
        };
    }
}