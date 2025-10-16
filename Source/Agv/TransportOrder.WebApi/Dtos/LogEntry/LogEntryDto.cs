using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;

/// <summary>
/// Provides a dto for log entries.
/// </summary>
[PublicAPI]
public class LogEntryDto
{
    /// <summary>
    /// The log category.
    /// </summary>
    [SwaggerFilterableProperty]
    public Categories Category { get; set; }

    /// <summary>
    /// The time and date the log was created.
    /// </summary>
    [SwaggerFilterableProperty]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The log code.
    /// </summary>
    [SwaggerFilterableProperty]
    public int LogCode { get; set; }

    /// <summary>
    /// The log severity level.
    /// </summary>
    [SwaggerFilterableProperty]
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// The agv correlation id.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? AgvCorrelationId { get; set; }

    /// <summary>
    /// The action group correlation id.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? ActionGroupCorrelationId { get; set; }

    /// <summary>
    /// The action group correlation id.
    /// </summary>
    [SwaggerFilterableProperty]
    public Guid? StationId { get; set; }

    /// <summary>
    /// Additional variables provided with the log.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}