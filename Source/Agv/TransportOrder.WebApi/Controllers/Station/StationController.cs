using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;

/// <summary>
/// Single station requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class StationController : Controller
{
    private readonly IRequestClient<GetStation> _getStationClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="StationController"/>
    public StationController(IMapper mapper,
        IRequestClient<GetStation> getStationClient)
    {
        _getStationClient = getStationClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get station.
    /// </summary>
    /// <returns>Station</returns>
    [HttpGet]
    [Route("/station/{correlationId}")]
    public async Task<ActionResult<StationDto>> GetStation(string correlationId)
    {
        Response<GotStation> response = await _getStationClient.GetResponse<GotStation>(new GetStation(correlationId));
        var stationDto = _mapper.Map<StationDto>(response.Message.Station);
        return Ok(stationDto);
    }
}