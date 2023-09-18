using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;

/// <summary>
/// Controller for <see cref="AgvAction"/>
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupController: Controller
{
    private readonly IRequestClient<GetActionGroups> _getAgvActionGroupsRequest;
    private readonly IRequestClient<GetActionGroupCountsByState> _getAgvActionGroupCountsByStateRequest;
    private readonly IRequestClient<GetActionGroup> _getActionGroupRequestClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupController" /> instance.
    /// </summary>
    /// <param name="mapper"></param>
    /// <param name="getAgvActionGroupsRequest"></param>
    /// <param name="getAgvActionGroupCountsByStateRequest"></param>
    /// <param name="getActionGroupRequestClient"></param>
    public ActionGroupController(IMapper mapper, IRequestClient<GetActionGroups> getAgvActionGroupsRequest, 
        IRequestClient<GetActionGroupCountsByState> getAgvActionGroupCountsByStateRequest, IRequestClient<GetActionGroup> getActionGroupRequestClient)
    {
        _mapper = mapper;
        _getAgvActionGroupsRequest = getAgvActionGroupsRequest;
        _getAgvActionGroupCountsByStateRequest = getAgvActionGroupCountsByStateRequest;
        _getActionGroupRequestClient = getActionGroupRequestClient;
    }

    /// <summary>
    ///  Retrieve actions
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/actionGroups")]
    public async Task<ActionResult<IEnumerable<ActionGroupDto>>> GetActionGroups(ActionGroupState[] states, DateTime? fromDate, DateTime? toDate, int limit=1000)
    {
        Response<GotActionGroups> response = await _getAgvActionGroupsRequest.GetResponse<GotActionGroups>( new GetActionGroups(
            states, 
            fromDate ?? DateTime.MinValue, 
            toDate ?? DateTime.MaxValue, 
            limit));
        List<ActionGroupDto> actionGroups = response.Message.ActionGroups.Select(g => _mapper.Map<ActionGroupDto>(g)).ToList();
        return Ok(actionGroups);
    }

    /// <summary>
    /// Retrieve action group by correlationId.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Action group with given correlationId doesn't exist</response>
    [HttpGet]
    [Route("/actionGroups/{correlationId}")]
    public async Task<ActionResult<ActionGroupDto>> GetActionGroup(Guid correlationId)
    {
        Response<GotAgvActionGroup> response = await _getActionGroupRequestClient.GetResponse<GotAgvActionGroup>(new GetActionGroup(correlationId));
        var actionGroup = _mapper.Map<ActionGroupDto>(response.Message.AgvActionGroup);
        return Ok(actionGroup!);
    }

    /// <summary>
    /// Get Action Group Meta
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/actionGroups/CountsByState")]
    public async Task<ActionResult<IEnumerable<AgvGroupCountByStateDto>>> GetActionGroupCountsByState()
    {
        Response<GotActionGroupCountsByState> response = await _getAgvActionGroupCountsByStateRequest.GetResponse<GotActionGroupCountsByState>(new GetActionGroupCountsByState());
        var actionGroupCountByState = _mapper.Map<List<AgvGroupCountByStateDto>>(response.Message.AgvGroupCountByState);
        return Ok(actionGroupCountByState);
    }
}