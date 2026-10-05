using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Map;

/// <summary>
/// Batch map requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class MapsController : Controller
{
    private readonly IRequestClient<GetMapList> _getMapListClient;

    /// <summary>
    /// Initializes a new <see cref="MapsController"/> instance.
    /// </summary>
    public MapsController(IRequestClient<GetMapList> getMapListClient)
    {
        _getMapListClient = getMapListClient;
    }

    /// <summary>
    /// Get maps.
    /// </summary>
    [HttpGet]
    [Route("/maps")]
    public async Task<ActionResult<IEnumerable<MapMetaDataDto>>> GetMaps()
    {
        var response = await _getMapListClient.GetResponse<GotMapList>(new GetMapList());
        var maps = response.Message.Maps.Select(m => new MapMetaDataDto(m.CorrelationId, m.MapName));
        return Ok(maps);
    }
}