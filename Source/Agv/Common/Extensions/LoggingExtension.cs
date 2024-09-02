using Microsoft.Extensions.Logging;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for logging.
/// </summary>
public static class LoggingExtension
{
    /// <summary>
    /// Log messages for batteries with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogBatteryInformation(this ILogger logger, int code, Guid agvId, string title, string message)
    {
        logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Battery), title, message);
    }

    /// <summary>
    /// Log messages for batteries with error severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogBatteryError(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message)
    {
        logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {ErrorCode} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Battery), errorCode, title, message);
    }

    /// <summary>
    /// Log messages for batteries with warning severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogBatteryWarning(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message)
    {
        logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {ErrorCode} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Battery), errorCode, title, message);
    }

    /// <summary>
    /// Log messages for agvs with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogAgvInformation(this ILogger logger, int code, Guid agvId, string title, string message)
    {
        logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Agv), title, message);
    }

    /// <summary>
    /// Log messages for agvs with error severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogAgvError(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message)
    {
        logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {ErrorCode} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Agv), errorCode, title, message);
    }

    /// <summary>
    /// Log messages for agvs with warning severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="errorCode">The error code of the agv error.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogAgvWarning(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message)
    {
        logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {LogCategory} {ErrorCode} {Title} {Message}", DateTime.UtcNow, code, agvId, nameof(Categories.Agv), errorCode, title, message);
    }

    /// <summary>
    /// Log messages for orders with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="actionGroupId">The action group correlation id.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogOrderInformation(this ILogger logger, int code, Guid agvId, Guid actionGroupId, string title, string message)
    {
        logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {ActionGroupCorrelationId} {LogCategory} {Title} {Message}", DateTime.UtcNow, code, agvId, actionGroupId, nameof(Categories.Order), title, message);
    }

    /// <summary>
    /// Log messages for orders with warning severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="actionGroupId">The action group correlation id.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    public static void LogOrderWarning(this ILogger logger, int code, Guid agvId, Guid actionGroupId, string title, string message)
    {
        logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {AgvCorrelationId} {ActionGroupCorrelationId} {LogCategory} {Title} {Message}", DateTime.UtcNow, code, agvId, actionGroupId, nameof(Categories.Order), title, message);
    }

    /// <summary>
    /// Log messages for stations with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    public static void LogStationInformation(this ILogger logger, int code, Guid stationId, string title, string message)
    {
        logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {StationId}  {LogCategory} {Title} {Message}", DateTime.UtcNow, code, stationId, nameof(Categories.Station), title, message);
    }

    /// <summary>
    /// Log messages for stations with warning severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    [PublicAPI]
    public static void LogStationWarning(this ILogger logger, int code, Guid stationId, string title, string message)
    {
        logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {StationId}  {LogCategory} {Title} {Message}", DateTime.UtcNow, code, stationId, nameof(Categories.Station), title, message);
    }

    /// <summary>
    /// Log messages for stations with error severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    [PublicAPI]
    public static void LogStationError(this ILogger logger, int code, Guid stationId, string title, string message)
    {
        logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} {LogCode} {StationId}  {LogCategory} {Title} {Message}", DateTime.UtcNow, code, stationId, nameof(Categories.Station), title, message);
    }
}

/// <summary>
/// Provides log categories.
/// </summary>
public enum Categories
{
    /// <summary>
    /// Battery related logs.
    /// </summary>
    Battery,

    /// <summary>
    /// Agv related logs.
    /// </summary>
    Agv,

    /// <summary>
    /// Order (action group) related logs.
    /// </summary>
    Order,

    /// <summary>
    /// Station related logs.
    /// </summary>
    Station
}