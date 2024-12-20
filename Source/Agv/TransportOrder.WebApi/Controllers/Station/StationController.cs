using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
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
    private readonly IRequestClient<TryUnlockStation> _unlockStationClient;
    private readonly IRequestClient<TryLockStation> _lockStationClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="StationController"/>
    public StationController(IMapper mapper,
        IRequestClient<GetStation> getStationClient, IRequestClient<TryLockStation> lockStationClient, IRequestClient<TryUnlockStation> unlockStationClient)
    {
        _getStationClient = getStationClient;
        _lockStationClient = lockStationClient;
        _unlockStationClient = unlockStationClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get station.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Station with given id not found.</response>
    [HttpGet]
    [Route("/station/{id:Guid}")]
    public async Task<ActionResult<StationDto>> GetStation(Guid id)
    {
        Response<GotStation> response = await _getStationClient.GetResponse<GotStation>(new GetStation(id));
        var stationDto = _mapper.Map<StationDto>(response.Message.Station);
        return Ok(stationDto);
    }

    /// <summary>
    /// Lock station.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Station with given id not found.</response>
    [HttpPost]
    [Route("/station/{id:Guid}/lock")]
    public async Task<ActionResult<bool>> LockStation(
        [Required]Guid id,
        [FromQuery][Required] string reason,
        [FromQuery][Required] string lockDescription,
        [FromQuery][Required] LockedOrderHandlingType lockedOrderHandling
    )
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }

        Response<LockedUnlockedStation> response = await _lockStationClient.GetResponse<LockedUnlockedStation>(new TryLockStation(id, email, reason, lockDescription, lockedOrderHandling));
        bool result = response.Message.Result;
        return Ok(result);
    }

    /// <summary>
    /// Unlock station.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Station with given id not found.</response>
    [HttpPatch]
    [Route("/station/{id:Guid}/unlock")]
    public async Task<ActionResult<bool>> UnlockStation([Required]Guid id)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }

        Response<LockedUnlockedStation> response = await _unlockStationClient.GetResponse<LockedUnlockedStation>(new TryUnlockStation(id, email));
        bool result = response.Message.Result;
        return Ok(result);
    }
}