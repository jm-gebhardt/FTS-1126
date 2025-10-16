using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Describes how a <see cref="LogEntry"/> can be filtered.
/// </summary>
[PublicAPI]
public class LogEntryFilterable : LogEntry, IFilterable<LogEntry, LogEntryFilterable>
{
    /// <inheritdoc cref="LogEntryFilterable"/>>
    public LogEntryFilterable()
    {
    }

    /// <inheritdoc cref="LogEntry"/>>
    [KeyProperty]
    public new Guid Id { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new Categories Category { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new int LogCode { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new string LogLevel { get; init; } = string.Empty;

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new Guid? AgvCorrelationId { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new Guid? ActionGroupCorrelationId { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [FilterProperty]
    public new Guid? StationId { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    public new DateTime Timestamp { get; init; }

    /// <inheritdoc cref="LogEntry"/>>
    [HasConversion]
    public new Dictionary<string, string> Variables { get; set; } = new();
}