using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.View;

/// <summary>
/// Batch view requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ViewsController(IRequestClient<GetViews> getViewsClient, IMapper mapper) : Controller
{
    /// <summary>
    /// Get all views. While no view is stored, one generated view per level.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/views")]
    public async Task<ActionResult<IReadOnlyList<ViewDto>>> GetViews()
    {
        var response = await getViewsClient.GetResponse<GotViews>(new GetViews(Guid.NewGuid()));
        return Ok(mapper.Map<IReadOnlyList<ViewDto>>(response.Message.Views));
    }
}