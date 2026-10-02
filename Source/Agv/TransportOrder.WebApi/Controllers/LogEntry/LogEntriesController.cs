using AutoMapper;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.TransportOrder.Messages.Requests.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using MassTransit;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.LogEntry;

/// <summary>
/// Provides log entries related endpoints.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class LogEntriesController(IMapper mapper, IRequestClient<GetFilteredLogEntries> filterLogEntriesClient) : Controller
{
    /// <summary>
    /// Get filtered log entries.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpPost]
    [Route("/logEntries/filter")]
    public async Task<ActionResult<PagedResult<LogEntryDto>>> FilterLogEntries(
        [FromBody] FilterDto filter,
        [FromQuery, Range(0, int.MaxValue)] int pageNumber,
        [FromQuery, Range(1, int.MaxValue)] int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        Response<GotLogEntries> response = await filterLogEntriesClient.GetResponse<GotLogEntries>(new GetFilteredLogEntries(filter, pageNumber, pageSize), cancellationToken);
        var dto = mapper.Map<PagedResult<LogEntryDto>>(response.Message.LogEntries);
        return Ok(dto);
    }
}