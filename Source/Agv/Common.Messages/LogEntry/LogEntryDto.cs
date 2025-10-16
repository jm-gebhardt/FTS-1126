using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.LogEntry;

/// <summary>
/// Provides a dto for log entries.
/// </summary>
[PublicAPI]
public class LogEntryDto
{
    /// <summary>
    /// The id of the log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The log category.
    /// </summary>
    public Categories Category { get; set; }

    /// <summary>
    /// The time and date the log was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The log code.
    /// </summary>
    public int LogCode { get; set; }

    /// <summary>
    /// The log severity level.
    /// </summary>
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// The agv correlation id.
    /// </summary>
    public Guid? AgvCorrelationId { get; set; }

    /// <summary>
    /// The action group correlation id.
    /// </summary>
    public Guid? ActionGroupCorrelationId { get; set; }

    /// <summary>
    /// The action group correlation id.
    /// </summary>
    public Guid? StationId { get; set; }

    /// <summary>
    /// Additional variables provided with the log.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}