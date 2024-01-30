using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Action groups batch requests.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupsController : Controller
{
    private readonly IRequestClient<GetActionGroups> _getAgvActionGroupsRequest;
    private readonly IRequestClient<GetActionGroupCountsByState> _getAgvActionGroupCountsByStateRequest;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupsController" /> instance.
    /// </summary>
    public ActionGroupsController(IMapper mapper, IRequestClient<GetActionGroups> getAgvActionGroupsRequest,
        IRequestClient<GetActionGroupCountsByState> getAgvActionGroupCountsByStateRequest)
    {
        _mapper = mapper;
        _getAgvActionGroupsRequest = getAgvActionGroupsRequest;
        _getAgvActionGroupCountsByStateRequest = getAgvActionGroupCountsByStateRequest;
    }

    /// <summary>
    ///  Get list of action groups.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/actionGroups")]
    public async Task<ActionResult<IEnumerable<ActionGroupDto>>> GetActionGroups(ActionGroupState[] states, DateTime? fromDate, DateTime? toDate, Guid? assignedAgv, int limit = 1000, SortBy sortBy = SortBy.CreatedDate, SortDirection sortDirection = SortDirection.Desc)
    {
        Response<GotActionGroups> response = await _getAgvActionGroupsRequest.GetResponse<GotActionGroups>(new GetActionGroups(
            states,
            fromDate ?? DateTime.MinValue,
            toDate ?? DateTime.MaxValue,
            assignedAgv != null ? new List<Guid> { (Guid)assignedAgv } : null,
            limit,
            sortBy,
            sortDirection == SortDirection.Desc));
        List<ActionGroupDto> actionGroups = response.Message.ActionGroups.Select(g => _mapper.Map<ActionGroupDto>(g)).ToList();
        return Ok(actionGroups);
    }

    /// <summary>
    /// Get number of action groups by state.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/actionGroupsAggregate/byState")]
    public async Task<ActionResult<IEnumerable<AgvGroupCountByStateDto>>> GetActionGroupCountsByState()
    {
        Response<GotActionGroupCountsByState> response = await _getAgvActionGroupCountsByStateRequest.GetResponse<GotActionGroupCountsByState>(new GetActionGroupCountsByState());
        var actionGroupCountByState = _mapper.Map<List<AgvGroupCountByStateDto>>(response.Message.AgvGroupCountByState);
        return Ok(actionGroupCountByState);
    }
}