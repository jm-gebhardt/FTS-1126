using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Single action group requests.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupController: Controller
{
    private readonly IRequestClient<GetActionGroup> _getActionGroupRequestClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    public ActionGroupController(IMapper mapper,  IRequestClient<GetActionGroup> getActionGroupRequestClient)
    {
        _mapper = mapper;
        _getActionGroupRequestClient = getActionGroupRequestClient;
    }

    /// <summary>
    /// Get action group.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId doesn't exist</response>
    [HttpGet]
    [Route("/actionGroup/{correlationId}")]
    public async Task<ActionResult<ActionGroupDto>> GetActionGroup(Guid correlationId)
    {
        Response<GotAgvActionGroup> response = await _getActionGroupRequestClient.GetResponse<GotAgvActionGroup>(new GetActionGroup(correlationId));
        var actionGroup = _mapper.Map<ActionGroupDto>(response.Message.AgvActionGroup);
        return Ok(actionGroup!);
    }
}