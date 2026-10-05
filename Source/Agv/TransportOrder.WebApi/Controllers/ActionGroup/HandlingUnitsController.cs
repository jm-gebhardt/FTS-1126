using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;

/// <summary>
/// Handling units batch requests.
/// </summary>
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class HandlingUnitsController : Controller
{
    private readonly IRequestClient<GetHandlingUnits> _getHandlingUnitsRequest;

    /// <inheritdoc cref="HandlingUnitsController"/>
    public HandlingUnitsController(
        IRequestClient<GetHandlingUnits> getHandlingUnitsRequest
    )
    {
        _getHandlingUnitsRequest = getHandlingUnitsRequest;
    }

    /// <summary>
    /// Search distinct Handling Units (HU) by substring (case-insensitive).
    /// Returns HU strings only (for autocomplete).
    /// </summary>
    /// <param name="query">The substring to search for (case-insensitive).</param>
    /// <param name="limit">The maximum number of handling unit results to return. Default is 100.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Invalid query</response>
    [HttpGet]
    [Route("/handlingUnits")]
    public async Task<ActionResult<IEnumerable<string>>> SearchHandlingUnits([FromQuery] [Required] string query, int limit = 100)
    {
        var response = await _getHandlingUnitsRequest.GetResponse<GotHandlingUnits>(new GetHandlingUnits(query, limit));

        return Ok(response.Message.HandlingUnits);
    }
}