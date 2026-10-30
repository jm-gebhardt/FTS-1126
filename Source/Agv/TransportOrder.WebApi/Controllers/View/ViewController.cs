using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.View;

/// <summary>
/// Single view requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ViewController(
    IRequestClient<GetView> getViewClient,
    IRequestClient<AddView> addViewClient,
    IRequestClient<UpdateView> updateViewClient,
    IRequestClient<DeleteView> deleteViewClient,
    IRequestClient<GetViewBackgroundImage> getBackgroundImageClient,
    IRequestClient<SetViewBackground> setBackgroundClient,
    IRequestClient<DeleteViewBackground> deleteBackgroundClient,
    ILoggingService loggingService,
    ILogger<ViewController> logger,
    IMapper mapper) : Controller
{
    private const string PngContentType = "image/png";

    private Task LogOperatorInteractionAsync(int operation)
    {
        return loggingService.LogSystem(logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system",
            new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, operation } });
    }

    /// <summary>
    /// Get a view.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">View with the given id not found.</response>
    [HttpGet]
    [Route("/view/{viewId:guid}")]
    public async Task<ActionResult<ViewDto>> GetView(Guid viewId)
    {
        var response = await getViewClient.GetResponse<GotView>(new GetView(viewId));
        return Ok(mapper.Map<ViewDto>(response.Message.View));
    }

    /// <summary>
    /// Create a view.
    /// </summary>
    /// <param name="dto">The data of the view.</param>
    /// <response code="200">The created view.</response>
    /// <response code="400">Invalid view data.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpPost]
    [Route("/view")]
    public async Task<ActionResult<ViewDto>> AddView(AddViewDto dto)
    {
        var command = new AddView(Guid.NewGuid(), dto.Name, dto.LevelId, dto.IsDefault, mapper.Map<ViewArea>(dto.Area));
        var response = await addViewClient.GetResponse<GotView>(command);
        await LogOperatorInteractionAsync(LogCodes.SystemViewAdded);

        return Ok(mapper.Map<ViewDto>(response.Message.View));
    }

    /// <summary>
    /// Update a view. Updating a generated view stores it.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <param name="dto">The changes of the view.</param>
    /// <response code="200">The updated view.</response>
    /// <response code="400">View with the given id not found or invalid view data.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpPatch]
    [Route("/view/{viewId:guid}")]
    public async Task<ActionResult<ViewDto>> UpdateView(Guid viewId, UpdateViewDto dto)
    {
        var area = dto.Area is null ? null : mapper.Map<ViewArea>(dto.Area);
        var command = new UpdateView(viewId, dto.Name, dto.LevelId, dto.IsDefault, area);
        var response = await updateViewClient.GetResponse<GotView>(command);
        await LogOperatorInteractionAsync(LogCodes.SystemViewUpdated);

        return Ok(mapper.Map<ViewDto>(response.Message.View));
    }

    /// <summary>
    /// Delete a view. Deleting a generated view does nothing.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <response code="204">View deleted.</response>
    /// <response code="400">View with the given id not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpDelete]
    [Route("/view/{viewId:guid}")]
    public async Task<ActionResult> DeleteView(Guid viewId)
    {
        await deleteViewClient.GetResponse<Success>(new DeleteView(viewId));
        await LogOperatorInteractionAsync(LogCodes.SystemViewDeleted);

        return NoContent();
    }

    /// <summary>
    /// Get the PNG background image of a view.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <param name="cancellationToken">Token cancelled if the request is aborted.</param>
    /// <response code="200">PNG bytes returned.</response>
    /// <response code="204">The view has no background.</response>
    /// <response code="400">View with the given id not found.</response>
    [HttpGet]
    [Route("/view/{viewId:guid}/background.png")]
    [Produces(PngContentType)]
    public async Task<ActionResult> GetBackgroundImage(Guid viewId, CancellationToken cancellationToken)
    {
        var response = await getBackgroundImageClient.GetResponse<GotViewBackgroundImage>(new GetViewBackgroundImage(viewId), cancellationToken);
        if (response.Message.Image is null)
        {
            return NoContent();
        }

        return File(response.Message.Image, PngContentType);
    }

    /// <summary>
    /// Upload or replace the background image of a view. Setting the background of a generated view stores it.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <param name="dto">The placement and PNG image of the background.</param>
    /// <response code="200">Background stored.</response>
    /// <response code="400">View with the given id not found, or the file is not a valid PNG.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpPut]
    [Route("/view/{viewId:guid}/background")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> SetBackground(Guid viewId, [FromForm] SetViewBackgroundDto dto)
    {
        await using var stream = new MemoryStream();
        await dto.File.CopyToAsync(stream);

        var command = new SetViewBackground(viewId, dto.Resolution, dto.XOffset, dto.YOffset, stream.ToArray());
        await setBackgroundClient.GetResponse<Success>(command);
        await LogOperatorInteractionAsync(LogCodes.SystemViewBackgroundImageUpdated);

        return Ok();
    }

    /// <summary>
    /// Delete the background image of a view.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <response code="204">Background deleted or none existed.</response>
    /// <response code="400">View with the given id not found.</response>
    [Authorize(Policy = AuthorizationPolicies.MapFullAccess)]
    [HttpDelete]
    [Route("/view/{viewId:guid}/background")]
    public async Task<ActionResult> DeleteBackground(Guid viewId)
    {
        await deleteBackgroundClient.GetResponse<Success>(new DeleteViewBackground(viewId));
        await LogOperatorInteractionAsync(LogCodes.SystemViewBackgroundImageDeleted);

        return NoContent();
    }
}