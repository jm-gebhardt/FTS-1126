using Gebhardt.Acc.Common.Messages.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;

/// <summary>
/// A log entry.
/// </summary>
[PublicAPI]
public class LogEntry
{
    /// <summary>
    /// The id of the log.
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
    public string LogLevel { get; set; }

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
    public virtual Dictionary<string, string> Variables { get; set; } = new();

    /// <inheritdoc cref="LogEntry"/>>
    public LogEntry(int logCode, Categories category, string logLevel, DateTime timestamp,
        Guid? agvCorrelationId, Guid? stationId,
        Guid? actionGroupCorrelationId)
    {
        Id = Guid.NewGuid();
        LogCode = logCode;
        Category = category;
        LogLevel = logLevel;
        Timestamp = timestamp;
        AgvCorrelationId = agvCorrelationId;
        ActionGroupCorrelationId = actionGroupCorrelationId;
        StationId = stationId;
    }

    /// <inheritdoc cref="LogEntry"/>>
    protected LogEntry()
    {
        LogLevel = string.Empty;
    }
}