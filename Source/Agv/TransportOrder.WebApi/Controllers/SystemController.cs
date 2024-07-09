using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Provides system related endpoints.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SystemController : Controller
{
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetSystemTime> _getSystemTimeClient;

    /// <inheritdoc cref="SystemController"/>
    public SystemController(
        IMapper mapper,
        IRequestClient<GetSystemTime> getSystemTimeClient)
    {
        _mapper = mapper;
        _getSystemTimeClient = getSystemTimeClient;
    }

    /// <summary>
    /// Get system time in ISO 8601 format.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/system/time")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<DateTime>> GetSystemTime()
    {
        Response<GotSystemTime> response = await _getSystemTimeClient.GetResponse<GotSystemTime>(new GetSystemTime(Guid.NewGuid()));
        string dto = _mapper.Map<string>(response.Message);
        return Ok(dto);
    }
}