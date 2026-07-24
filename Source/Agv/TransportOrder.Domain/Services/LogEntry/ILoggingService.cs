using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;

/// <summary>
/// Wraps logging.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Log messages for orders with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="severity">The log severity level.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="actionGroupId">The action group correlation id.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task LogOrder(ILogger logger, LogLevel severity, int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log messages for agvs with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="severity">The log severity level.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task LogAgv(ILogger logger, LogLevel severity, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log messages for agv battery with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="severity">The log severity level.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task LogBattery(ILogger logger, LogLevel severity, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log messages for stations.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="severity">The log severity level.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task LogStation(ILogger logger, LogLevel severity, int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log messages for the system.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    /// <param name="logLevel">The log severity level.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task LogSystem(ILogger logger, int code, string title, string message, Dictionary<string, object?>? additionalLabels = null, LogLevel logLevel = LogLevel.Information, CancellationToken cancellationToken = default);
}