using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for log entry related queries.
/// </summary>
public interface ILogEntryRepository
{
    /// <summary>
    /// Adds a log entry.
    /// </summary>
    /// <param name="logCode">The log code.</param>
    /// <param name="logCategory">The log category.</param>
    /// <param name="logLevel">The log severity level.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="actionGroupCorrelationId">The action group correlation id.</param>
    /// <param name="stationId">The station id.</param>
    /// <param name="timestamp">The time the log was created.</param>
    /// <param name="variables">The additional variables provided.</param>
    /// <returns>True, of the entry has been added.</returns>
    Task<LogEntry> AddAsync(Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> variables);

    /// <summary>
    /// Get filtered log entries.
    /// </summary>
    /// <param name="logEntryFilter">The filter to apply.</param>
    /// <param name="pageNr">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of log entries matching the filter.</returns>
    Task<PagedResult<LogEntry>> FilterAsync(IFilter logEntryFilter, int pageNr, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Delete log entries older than given time.
    /// </summary>
    /// <param name="outDatedTime">The time a log entry gets outdated.</param>
    Task DeleteOutdatedLogs(DateTime outDatedTime);
}