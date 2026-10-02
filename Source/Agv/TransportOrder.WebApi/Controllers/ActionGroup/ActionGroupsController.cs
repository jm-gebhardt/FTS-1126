using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SortDirection = Gebhardt.Acc.Common.Filter.Enums.SortDirection;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Action groups batch requests.
/// </summary>
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ActionGroupsController : Controller
{
    private readonly IRequestClient<GetActionGroups> _getAgvActionGroupsRequest;
    private readonly IRequestClient<GetActionGroupCountsByState> _getAgvActionGroupCountsByStateRequest;
    private readonly IRequestClient<GetActionGroupStatistics> _durationStatisticsRequest;
    private readonly IRequestClient<GetFilteredActionGroups> _filteredActionGroupsRequest;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupsController" /> instance.
    /// </summary>
    public ActionGroupsController(IMapper mapper,
        IRequestClient<GetActionGroups> getAgvActionGroupsRequest,
        IRequestClient<GetActionGroupCountsByState> getAgvActionGroupCountsByStateRequest,
        IRequestClient<GetActionGroupStatistics> durationStatisticsRequest,
        IRequestClient<GetFilteredActionGroups> filteredActionGroupsRequest)
    {
        _mapper = mapper;
        _getAgvActionGroupsRequest = getAgvActionGroupsRequest;
        _getAgvActionGroupCountsByStateRequest = getAgvActionGroupCountsByStateRequest;
        _durationStatisticsRequest = durationStatisticsRequest;
        _filteredActionGroupsRequest = filteredActionGroupsRequest;
    }

    /// <summary>
    ///  Get list of action groups.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/actionGroups")]
    public async Task<ActionResult<IEnumerable<ActionGroupDto>>> GetActionGroups(ActionGroupState[] states, DateTime? fromDate, DateTime? toDate, Guid? assignedAgv, int limit = 1000, SortBy sortBy = SortBy.CreatedDate, SortDirection sortDirection = SortDirection.Desc, CancellationToken cancellationToken = default)
    {
        Response<GotActionGroups> response = await _getAgvActionGroupsRequest.GetResponse<GotActionGroups>(new GetActionGroups(
            states,
            fromDate ?? DateTime.MinValue,
            toDate ?? DateTime.MaxValue,
            assignedAgv != null ? new List<Guid> { (Guid) assignedAgv } : null,
            limit,
            sortBy,
            sortDirection == SortDirection.Desc),
            cancellationToken);
        List<ActionGroupDto> actionGroups = response.Message.ActionGroups.Select(g => _mapper.Map<ActionGroupDto>(g)).ToList();
        return Ok(actionGroups);
    }

    /// <summary>
    /// Get number of action groups by state.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/actionGroupsAggregate/byState")]
    public async Task<ActionResult<IEnumerable<AgvGroupCountByStateDto>>> GetActionGroupCountsByState(CancellationToken cancellationToken = default)
    {
        Response<GotActionGroupCountsByState> response = await _getAgvActionGroupCountsByStateRequest.GetResponse<GotActionGroupCountsByState>(new GetActionGroupCountsByState(), cancellationToken);
        var actionGroupCountByState = _mapper.Map<List<AgvGroupCountByStateDto>>(response.Message.AgvGroupCountByState);
        return Ok(actionGroupCountByState);
    }

    /// <summary>
    /// Get duration statistics by action type in group heading from source to target. Source and target are optional.
    /// </summary>
    /// <param name="source">The source node id (optional).</param>
    /// <param name="target">The target node id (optional).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request.</param>
    /// <response code="200">Ok</response>
    /// <returns>Duration statistics of actions in groups matching query parameters. Empty list if no matching groups where found.</returns>
    [HttpGet]
    [Route("/actionGroups/durationStatistics")]
    public async Task<ActionResult<IEnumerable<ActionDurationStatisticsDto>>> GetDurationStatistics([FromQuery] string? source, [FromQuery] string? target, CancellationToken cancellationToken = default)
    {
        Response<GotActionGroupStatistics> response = await _durationStatisticsRequest.GetResponse<GotActionGroupStatistics>(new GetActionGroupStatistics(source?.ToGuid(), target?.ToGuid()), cancellationToken);
        return Ok(_mapper.Map<List<ActionDurationStatisticsDto>>(response.Message.DurationStatistics));
    }

    /// <summary>
    /// Gets filtered action groups.
    /// </summary>
    [HttpPost]
    [Route("/actionGroups/filtered")]
    public async Task<ActionResult<PagedResult<FilteredActionGroupDto>>> Filtered(
        [FromBody] FilterDto filter,
        [FromQuery, Range(0, int.MaxValue)] int pageNumber,
        [FromQuery, Range(1, int.MaxValue)] int pageSize,
        SortDirection direction,
        ICollection<string>? orderingProperties,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        Response<GotFilteredActionGroups> result = await _filteredActionGroupsRequest.GetResponse<GotFilteredActionGroups>(new GetFilteredActionGroups(filter, pageNumber, pageSize, direction, orderingProperties ?? []), cancellationToken);
        var mapped = _mapper.Map<PagedResult<FilteredActionGroupDto>>(result.Message.ActionGroups);

        return Ok(mapped);
    }
}