using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterTemplate;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
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
public class FilterTemplateController(
    IMapper mapper,
    IRequestClient<GetFilterTemplates> getFilterTemplatesRequestClient,
    IRequestClient<AddFilterTemplate> addFilterTemplateRequestClient,
    IRequestClient<UpdateFilterTemplate> updateFilterTemplateRequestClient,
    IRequestClient<DeleteFilterTemplate> deleteFilterTemplateRequestClient) : Controller
{
    /// <summary>
    /// Gets the filter templates.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("/filterTemplates")]
    public async Task<ActionResult<ICollection<FilterTemplateDto>>> GetFilterTemplates()
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }
        Response<GotFilterTemplates> result = await getFilterTemplatesRequestClient.GetResponse<GotFilterTemplates>(new GetFilterTemplates(user));

        var filterTemplates = mapper.Map<ICollection<FilterTemplateDto>>(result.Message.FilterTemplates);

        return Ok(filterTemplates);
    }

    /// <summary>
    /// Adds a new filter template.
    /// </summary>
    /// <param name="filterTemplate">The filter template to add.</param>
    /// <param name="name">The name of the filter.</param>
    /// <response code="200">The guid of the filter</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("/filterTemplate/add")]
    public async Task<ActionResult<Guid>> AddFilter([Required] FilterTemplateDto filterTemplate, [Required] string name)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<AddedFilterTemplate> result = await addFilterTemplateRequestClient.GetResponse<AddedFilterTemplate>(new AddFilterTemplate(user, mapper.Map<FilterTemplate>(filterTemplate), name));

        return Ok(result.Message.FilterTemplateCorrelationId);
    }

    /// <summary>
    /// Updates a filter template.
    /// </summary>
    /// <param name="correlationId">The id of the filter.</param>
    /// <param name="filterTemplate">The new filter to save.</param>
    /// <param name="name">The name of the filter.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("/filterTemplate/{correlationId:guid}/update")]
    public async Task<ActionResult<bool>> UpdateFilter([FromRoute] Guid correlationId, [Required] FilterTemplateDto filterTemplate, [Required] string name)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }

        Response<OwnedFilterModificationResult> result = await updateFilterTemplateRequestClient.GetResponse<OwnedFilterModificationResult>(new UpdateFilterTemplate(user, mapper.Map<FilterTemplate>(filterTemplate), correlationId, name));

        return Ok(result.Message.Result);
    }

    /// <summary>
    /// Deletes a filter template.
    /// </summary>
    /// <param name="correlationId">The id of the filter.</param>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete]
    [Route("/filterTemplate/{correlationId:guid}/delete")]
    public async Task<ActionResult<bool>> DeleteFilter([FromRoute] Guid correlationId)
    {
        string? user = User.GetEmail();
        if (user == null)
        {
            return Unauthorized();
        }
        Response<OwnedFilterModificationResult> result = await deleteFilterTemplateRequestClient.GetResponse<OwnedFilterModificationResult>(new DeleteFilterTemplate(correlationId, user));

        return Ok(result.Message.Result);
    }
}