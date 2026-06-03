using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.System.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Provides system related endpoints.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SystemController(
    IMapper mapper,
    IRequestClient<GetSystemTime> getSystemTimeClient,
    IRequestClient<GetFactoryTimeZone> getFactoryTimeClient,
    IRequestClient<UpdateFactoryTimeZone> updateFactoryTimeClient,
    IRequestClient<UpdateHostOrderGeneration> updateHostOrderGenerationClient,
    IRequestClient<RequestHostOrderGeneration> requestHostOrderGenerationClient,
    IRequestClient<GetCertificateExpirations> getCertificateExpirationClient,
    IRequestClient<UpdateHousekeepingConfiguration> updateHousekeepingConfigurationClient) : Controller
{
    /// <summary>
    /// Get system time in ISO 8601 format.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/system/time")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<DateTime>> GetSystemTime()
    {
        Response<GotSystemTime> response = await getSystemTimeClient.GetResponse<GotSystemTime>(new GetSystemTime(Guid.NewGuid()));
        string dto = mapper.Map<string>(response.Message);
        return Ok(dto);
    }

    /// <summary>
    /// Get the time zones available to the system.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/system/timezones")]
    [Produces(MediaTypeNames.Application.Json)]
    public Task<ActionResult<ReadOnlyCollection<TimeZoneInfo>>> GetSystemTimeZones()
    {
        ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
        return Task.FromResult<ActionResult<ReadOnlyCollection<TimeZoneInfo>>>(Ok(timeZones));
    }

    /// <summary>
    /// Sets the factory time zone.
    /// </summary>
    /// <param name="timeZone">The time zone as retrievable from <see cref="GetSystemTimeZones"/></param>
    /// <returns>True if the time zone exists, false otherwise.</returns>
    /// <response code="200">Ok</response>
    [HttpPatch]
    [Route("/system/timezone/factory")]
    public async Task<ActionResult<bool>> SetFactoryTimeZone([FromQuery] string timeZone)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo))
        {
            return BadRequest($"No TimeZone '{timeZone}' found");
        }

        Response<UpdatedFactoryTimeZone> response = await updateFactoryTimeClient.GetResponse<UpdatedFactoryTimeZone>(new UpdateFactoryTimeZone(timeZoneInfo));

        return Ok(response.Message.Result);
    }

    /// <summary>
    /// Get the factory time zone.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="203">Saved time zone has become unavailable, delivering default.</response>
    [HttpGet]
    [Route("/system/timezone/factory")]
    public async Task<ActionResult<TimeZoneInfo>> GetFactoryTimeZone()
    {
        Response<GotFactoryTimeZone> response = await getFactoryTimeClient.GetResponse<GotFactoryTimeZone>(new GetFactoryTimeZone());
        return response.Message.Dirty ? StatusCode(203, response.Message.FactoryTime) : Ok(response.Message.FactoryTime);
    }

    /// <summary>
    /// Updates the HostOrder generation setting.
    /// </summary>
    /// <param name="isEnabled">Indicates whether HostOrder generation should be enabled or disabled.</param>
    /// <response code="204">No Content</response>
    [HttpPut]
    [Route("/system/hostOrderGeneration")]
    public async Task<IActionResult> UpdateHostOrderGeneration([FromQuery] bool isEnabled)
    {
        var command = new UpdateHostOrderGeneration(isEnabled);
        await updateHostOrderGenerationClient.GetResponse<Success>(command);
        return NoContent();
    }

    /// <summary>
    /// Creates host orders based on the specified parameters.
    /// </summary>
    /// <param name="source">Origin of the orders (host definition).</param>
    /// <param name="destination">Target of the orders (host definition).</param>
    /// <param name="count">Number of orders to create.</param>
    /// <returns>A list of created order Ids.</returns>
    [HttpPost]
    [Route("/system/hostOrders")]
    public async Task<ActionResult<List<string>>> CreateHostOrders([FromQuery] string source, [FromQuery] string destination, [FromQuery] int count)
    {
        var command = new RequestHostOrderGeneration(Guid.NewGuid(), source, destination, count);
        Response<HostOrderGenerationResponse> response = await requestHostOrderGenerationClient.GetResponse<HostOrderGenerationResponse>(command);
        return Ok(response.Message.OrderIds);
    }

    /// <summary>
    /// Get certificate expiration information.
    /// </summary>
    /// <response code="200">Returns a list of known certificates with its expiration dates</response>
    [HttpGet]
    [Route("/system/monitoring/certificates")]
    public async Task<ActionResult<IEnumerable<CertificateExpirationDto>>> GetCertificateExpirations()
    {
        Response<GotCertificateExpirations> response = await getCertificateExpirationClient.GetResponse<GotCertificateExpirations>(new GetCertificateExpirations());

        var dto = mapper.Map<IEnumerable<CertificateExpirationDto>>(response.Message.Certificates);
        return Ok(dto);
    }

    /// <summary>
    /// Updates the agv error housekeeping configuration.
    /// </summary>
    [HttpPost]
    [Route("/system/housekeepingConfiguration")]
    public async Task<ActionResult> UpdateHousekeepingInterval([FromQuery] int outdatedValue, [FromQuery] HousekeepingInterval outdatedInterval, [FromQuery] HousekeepingEntityType entityType, [FromQuery] int scheduleValue, [FromQuery] HousekeepingInterval scheduleInterval, [FromQuery] int scheduleHour, [FromQuery] int scheduleMinute)
    {
        await updateHousekeepingConfigurationClient.GetResponse<Success>(new UpdateHousekeepingConfiguration(entityType, outdatedValue, outdatedInterval, scheduleValue, scheduleInterval, scheduleHour, scheduleMinute));
        return Ok();
    }
}