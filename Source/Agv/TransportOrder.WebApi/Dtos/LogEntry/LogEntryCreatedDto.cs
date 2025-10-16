using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;

/// <summary>
/// Represents a dto for a created log entry.
/// </summary>
[PublicAPI]
public record LogEntryCreatedDto : DatedMessage
{
    /// <inheritdoc cref="LogEntryCreatedDto"/>>
    public LogEntryCreatedDto(Guid? correlationId, LogEntryDto logEntry)
    {
        CorrelationId = correlationId;
        LogEntry = logEntry;
    }

    /// <inheritdoc cref="LogEntryCreatedDto"/>>
    public LogEntryCreatedDto()
    {
    }

    /// <summary>The correlation id (station, action group, agv, or null for system log entries).</summary>
    public Guid? CorrelationId { get; init; } = Guid.Empty;

    /// <summary>The new log entry.</summary>
    public LogEntryDto LogEntry { get; init; } = null!;

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    /// <param name="correlationId">The correlation id of the message</param>
    /// <param name="logEntry">The log entry.</param>
    public void Deconstruct(out Guid? correlationId, out LogEntryDto logEntry)
    {
        correlationId = CorrelationId;
        logEntry = LogEntry;
    }
}