using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered log entry.
/// </summary>
[PublicAPI]
public record FilteredLogEntryDto
{
    /// <summary>
    /// The unique identifier of the log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The category of the log entry.
    /// </summary>
    [SwaggerFilterableProperty]
    public Categories Category { get; init; }

    /// <summary>
    /// The log code.
    /// </summary>
    [SwaggerFilterableProperty]
    public int LogCode { get; init; }

    /// <summary>
    /// The log level.
    /// </summary>
    [SwaggerFilterableProperty]
    public string LogLevel { get; init; } = string.Empty;

    /// <summary>
    /// The correlation ID of the AGV associated with this log entry.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? AgvCorrelationId { get; init; }

    /// <summary>
    /// The correlation ID of the action group associated with this log entry.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? ActionGroupCorrelationId { get; init; }

    /// <summary>
    /// The station ID associated with this log entry.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? StationId { get; init; }

    /// <summary>
    /// The timestamp of the log entry.
    /// </summary>
    [SwaggerFilterableProperty]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Additional variables associated with the log entry.
    /// </summary>
    public Dictionary<string, string> Variables { get; init; } = new();
}
