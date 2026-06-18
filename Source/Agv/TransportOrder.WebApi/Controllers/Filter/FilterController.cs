using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Filter;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Filter;

/// <summary>
/// Provides filter related endpoints.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class FilterController(
    IMapper mapper,
    IRequestClient<GetOwnedFilters> getOwnedFiltersRequestClient,
    IRequestClient<AddOwnedFilter> addOwnedFilterRequestClient,
    IRequestClient<UpdateOwnedFilter> updateOwnedFilterRequestClient,
    IRequestClient<DeleteOwnedFilter> deleteOwnedFilterRequestClient) : Controller
{
    /// <summary>
    /// Gets the filters for the current user.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("/filters")]
    public async Task<ActionResult<ICollection<OwnedFilterDto>>> GetFilters()
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<GotOwnedFilters> result = await getOwnedFiltersRequestClient.GetResponse<GotOwnedFilters>(new GetOwnedFilters(user));

        var ownedFilterDtos = mapper.Map<ICollection<OwnedFilterDto>>(result.Message.Filters);

        return Ok(ownedFilterDtos);
    }

    /// <summary>
    /// Adds a new filter for the user.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <param name="name">The name of the filter (max 400 characters).</param>
    /// <response code="200">The guid of the filter</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("/filter/add")]
    public async Task<ActionResult<Guid>> AddFilter([Required] FilterDto filter, [Required] string name)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<AddedOwnedFilter> result = await addOwnedFilterRequestClient.GetResponse<AddedOwnedFilter>(new AddOwnedFilter(user, filter, name));

        return Ok(result.Message.OwnedFilterCorrelationId);
    }

    /// <summary>
    /// Updates a filter for the user.
    /// </summary>
    /// <param name="correlationId">The id of the filter.</param>
    /// <param name="filter">The new filter to save.</param>
    /// <param name="name">The name of the filter (max 400 characters).</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("/filter/{correlationId:guid}/update")]
    public async Task<ActionResult<bool>> UpdateFilter([FromRoute] Guid correlationId, [Required] FilterDto filter, [Required] string name)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<OwnedFilterModificationResult> result = await updateOwnedFilterRequestClient.GetResponse<OwnedFilterModificationResult>(new UpdateOwnedFilter(user, filter, correlationId, name));

        return Ok(result.Message.Result);
    }

    /// <summary>
    /// Deletes a filter for the user.
    /// </summary>
    /// <param name="correlationId">The id of the filter.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete]
    [Route("/filter/{correlationId:guid}/delete")]
    public async Task<ActionResult<bool>> DeleteFilter([FromRoute] Guid correlationId)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<OwnedFilterModificationResult> result = await deleteOwnedFilterRequestClient.GetResponse<OwnedFilterModificationResult>(new DeleteOwnedFilter(user, correlationId));

        return Ok(result.Message.Result);
    }
}