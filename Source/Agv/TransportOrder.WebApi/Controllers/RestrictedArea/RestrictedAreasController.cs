using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.RestrictedArea;

/// <summary>
/// Restricted area requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class RestrictedAreasController : Controller
{
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetRestrictedAreas> _getRestrictedAreasClient;

    /// <inheritdoc cref="RestrictedAreaController"/>>
    public RestrictedAreasController(IMapper mapper,
        IRequestClient<GetRestrictedAreas> getRestrictedAreasClient)
    {
        _mapper = mapper;
        _getRestrictedAreasClient = getRestrictedAreasClient;
    }

    /// <summary>
    /// Get restricted areas.
    /// </summary>
    /// <param name="ids">The ids of the restricted areas (all are returned if empty array is given).</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Any of the restricted area with given ids not found.</response>
    [HttpGet]
    [Route("/restrictedAreas")]
    public async Task<ActionResult<ICollection<RestrictedAreaDto>>> GetRestrictedAreas([FromQuery] Guid[] ids)
    {
        var response = await _getRestrictedAreasClient.GetResponse<GotRestrictedAreas>(new GetRestrictedAreas(ids));
        var restrictedAreas = _mapper.Map<List<RestrictedAreaDto>>(response.Message.RestrictedAreas);
        return Ok(restrictedAreas);
    }
}