using Gebhardt.Acc.Common.Messages.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model of filterable log entries.
/// </summary>
[PublicAPI]
public record FilteredLogEntry
{
    /// <summary>
    /// The unique identifier of the log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The category of the log entry.
    /// </summary>
    public Categories Category { get; init; }

    /// <summary>
    /// The log code.
    /// </summary>
    public int LogCode { get; init; }

    /// <summary>
    /// The log level.
    /// </summary>
    public string LogLevel { get; init; } = string.Empty;

    /// <summary>
    /// The correlation ID of the AGV associated with this log entry.
    /// </summary>
    public Guid? AgvCorrelationId { get; init; }

    /// <summary>
    /// The correlation ID of the action group associated with this log entry.
    /// </summary>
    public Guid? ActionGroupCorrelationId { get; init; }

    /// <summary>
    /// The station ID associated with this log entry.
    /// </summary>
    public Guid? StationId { get; init; }

    /// <summary>
    /// The timestamp of the log entry.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Additional variables associated with the log entry.
    /// </summary>
    public Dictionary<string, string> Variables { get; init; } = new();
}
