using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using Gebhardt.Core.Observability.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.BackgroundEditor;

/// <summary>
/// Background editor requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class BackgroundController(
    IMapper mapper,
    IRequestClient<AddOrUpdateBackground> addOrUpdateBackgroundClient,
    IRequestClient<GetBackground> getBackgroundClient,
    IRequestClient<DeleteBackgroundScene> deleteBackgroundClient,
    IRequestClient<GetBackgrounds> getBackgroundsClient, ITracing tracing) : Controller
{
    /// <summary>
    /// Add or update background editor scene.
    /// </summary>
    /// <response code="200">Store the background successfully.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.BackGroundEdit)]
    [Route("/background")]
    public async Task<ActionResult> AddOrUpdateBackground([FromBody] [Required] BackgroundDto backgroundDto)
    {
        tracing.StartSpan(nameof(AddOrUpdateBackground), attributes: new[]
        {
            new KeyValuePair<string, object?>(TracingConstants.TagBackgroundId, backgroundDto.Id)
        });
        var background = mapper.Map<Background>(backgroundDto);
        await addOrUpdateBackgroundClient.GetResponse<BackgroundAddedOrUpdated>(new AddOrUpdateBackground(background));
        return Ok();
    }

    /// <summary>
    /// Get a background editor scene.
    /// </summary>
    /// <response code="200">Got background successfully.</response>
    /// <response code="404">Background not found.</response>
    [HttpGet]
    [Route("/background/{id:guid}")]
    public async Task<ActionResult<BackgroundDto>> GetBackground([FromRoute][Required] Guid id)
    {
        tracing.StartSpan(nameof(GetBackground), attributes: new[]
        {
            new KeyValuePair<string, object?>(TracingConstants.TagBackgroundId, id)
        });
        Response<GotBackground> response = await getBackgroundClient.GetResponse<GotBackground>(new GetBackground(id));
        var background = mapper.Map<BackgroundDto>(response.Message.Background);

        return Ok(background);
    }

    /// <summary>
    /// Delete a background editor scene.
    /// </summary>
    /// <response code="200">Background deleted successfully.</response>
    /// <response code="404">Background not found.</response>
    [HttpDelete]
    [Authorize(Policy = AuthorizationPolicies.BackGroundEdit)]
    [Route("/background/{id:guid}")]
    public async Task<ActionResult> DeleteBackground([FromRoute][Required] Guid id)
    {
        tracing.StartSpan(nameof(DeleteBackground), attributes: new[]
        {
            new KeyValuePair<string, object?>(TracingConstants.TagBackgroundId, id)
        });
        await deleteBackgroundClient.GetResponse<Success>(new DeleteBackgroundScene(id));

        return Ok();
    }

    /// <summary>
    /// Get background editor scenes (optionally, for given map ids).
    /// </summary>
    /// <response code="200">Got backgrounds successfully.</response>
    /// <param name="mapIds">Optional ids of maps the backgrounds are on.</param>
    /// <param name="maxCount">Maximum number of backgrounds returned (default 50).</param>
    [HttpPost]
    [Route("/backgrounds")]
    public async Task<ActionResult<ICollection<BackgroundDto>>> GetBackgrounds([FromQuery] ICollection<Guid>? mapIds, [FromQuery] int maxCount=50)
    {
        tracing.StartSpan(nameof(GetBackgrounds));
        Response<GotBackgrounds> response = await getBackgroundsClient.GetResponse<GotBackgrounds>(new GetBackgrounds(mapIds ?? [], maxCount));
        var backgrounds = mapper.Map<ICollection<BackgroundDto>>(response.Message.Backgrounds);

        return Ok(backgrounds);
    }
}
