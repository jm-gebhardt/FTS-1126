using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using System.Text;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogBatteryInformation(this ILogger logger, int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Battery) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Information, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogBatteryError(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Battery) },
            { "ErrorCode", errorCode},
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Error, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>.
    public static void LogBatteryWarning(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Battery) },
            { "ErrorCode", errorCode},
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Warning, message, parameters);
    }

    /// <summary>
    /// Log messages for agvs with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="agvId">The correlation id of the agv.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogAgvInformation(this ILogger logger, int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Agv) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Information, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogAgvError(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Agv) },
            { "ErrorCode", errorCode},
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Error, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogAgvWarning(this ILogger logger, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "LogCategory", nameof(Categories.Agv) },
            { "ErrorCode", errorCode},
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Warning, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogOrderInformation(this ILogger logger, int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "ActionGroupCorrelationId", actionGroupId },
            { "LogCategory", nameof(Categories.Order) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Information, message, parameters);
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
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogOrderWarning(this ILogger logger, int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "AgvCorrelationId", agvId },
            { "ActionGroupCorrelationId", actionGroupId },
            { "LogCategory", nameof(Categories.Order) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Warning, message, parameters);
    }

    /// <summary>
    /// Log messages for stations with information severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    public static void LogStationInformation(this ILogger logger, int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "StationId", stationId },
            { "LogCategory", nameof(Categories.Station) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Information, message, parameters);
    }

    /// <summary>
    /// Log messages for stations with warning severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    [PublicAPI]
    public static void LogStationWarning(this ILogger logger, int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "StationId", stationId },
            { "LogCategory", nameof(Categories.Station) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Warning, message, parameters);
    }

    /// <summary>
    /// Log messages for stations with error severity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="code">The log message code.</param>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message.</param>
    /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
    [PublicAPI]
    public static void LogStationError(this ILogger logger, int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
            { "LogCode", code },
            { "StationId", stationId },
            { "LogCategory", nameof(Categories.Station) },
            { "Title", title },
            { "Message", message }
        };
        AddLabels(parameters, additionalLabels);
        WriteLogLine(logger, LogLevel.Error, message, parameters);
    }

    private static void WriteLogLine(ILogger logger, LogLevel severity, string message, Dictionary<string, object?> parameters)
    {
        logger.Log(severity, new EventId(), parameters, null, (state, _) =>
        {
            var sb = new StringBuilder();
            sb.Append(message);
            foreach (KeyValuePair<string, object?> param in state)
            {
                sb.Append($" {{{param.Key}}}");
            }
            return sb.ToString();
        });
    }

    private static void AddLabels(Dictionary<string, object?> logParameters, Dictionary<string, object?>? additionalLabels)
    {
        if (additionalLabels != null)
        {
            foreach (KeyValuePair<string, object?> kvp in additionalLabels)
            {
                logParameters[kvp.Key] = kvp.Value;
            }
        }
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