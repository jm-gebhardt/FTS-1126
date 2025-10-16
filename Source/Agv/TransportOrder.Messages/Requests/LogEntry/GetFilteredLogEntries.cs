using Gebhardt.Acc.Common.Filter.Api.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.LogEntry;

/// <summary>
/// Provides a command to request filtered log entries.
/// </summary>
/// <param name="Filter">The filter to apply.</param>
/// <param name="PageNr">The current page number.</param>
/// <param name="PageSize">The page size.</param>
[PublicAPI]
public record GetFilteredLogEntries(FilterDto Filter, int PageNr, int PageSize);