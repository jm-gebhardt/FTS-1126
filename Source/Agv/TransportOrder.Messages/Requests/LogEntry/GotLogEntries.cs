using Gebhardt.Acc.Common.Filter.Api.Dtos;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.LogEntry;

/// <summary>
/// Represents the response to the <see cref="GetFilteredLogEntries"/> request.
/// </summary>
/// <param name="LogEntries">The received log entries.</param>
public record GotLogEntries(PagedResult<Domain.Models.LogEntry.LogEntry> LogEntries);