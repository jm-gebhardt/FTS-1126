
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.LogEntry;

/// <summary>
/// Represents the event raised when a new log entry has been created.
/// </summary>
/// <param name="LogEntry">The new log entry.</param>
public record LogEntryCreated(LogEntryDto LogEntry) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => LogEntry.Id;
}