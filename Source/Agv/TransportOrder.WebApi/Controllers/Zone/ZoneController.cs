using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Zone;

/// <summary>
/// Zone requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class ZoneController(IMapper mapper, IRequestClient<GetZones> getZonesRequestClient) : Controller
{
    /// <summary>
    /// Get all the zones.
    /// </summary>
    [HttpGet]
    [Route("/zones")]
    public async Task<ActionResult<ICollection<ZoneDto>>> GetZones()
    {
        Response<GotZones> response = await getZonesRequestClient.GetResponse<GotZones>(new GetZones());
        var dto = mapper.Map<List<ZoneDto>>(response.Message.Zones);
        return Ok(dto);
    }
}