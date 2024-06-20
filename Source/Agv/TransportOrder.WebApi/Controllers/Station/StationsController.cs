using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;

/// <summary>
/// Batch Stations requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class StationsController : Controller
{
    private readonly IRequestClient<GetStations> _getStationsClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="StationsController"/>
    public StationsController(IMapper mapper,
        IRequestClient<GetStations> getStationsClient)
    {
        _getStationsClient = getStationsClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get stations.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/stations")]
    public async Task<ActionResult<ICollection<StationDto>>> GetStations()
    {
        Response<GotStations> response = await _getStationsClient.GetResponse<GotStations>(new GetStations(Guid.NewGuid()));
        var stationsDto = _mapper.Map<List<StationDto>>(response.Message.Stations);
        return Ok(stationsDto);
    }
}