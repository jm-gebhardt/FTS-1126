using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using System.Text;
using Gebhardt.Acc.Common.Messages.LogEntry;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for logging.
/// </summary>
public static class LoggingExtension
{
    /// <summary>
    /// Extension block for <see cref="ILogger"/>.
    /// </summary>
    extension(ILogger logger)
    {
        /// <summary>
        /// Log messages for batteries with information severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogBatteryInformation(int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="errorCode">The error code of the agv error.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogBatteryError(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="errorCode">The error code of the agv error.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>.
        public void LogBatteryWarning(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogAgvInformation(int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="errorCode">The error code of the agv error.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogAgvError(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="errorCode">The error code of the agv error.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogAgvWarning(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="actionGroupId">The action group correlation id.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogOrderInformation(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="actionGroupId">The action group correlation id.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogOrderWarning(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// Log messages for orders with error severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="agvId">The correlation id of the agv.</param>
        /// <param name="actionGroupId">The action group correlation id.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogOrderError(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
            WriteLogLine(logger, LogLevel.Error, message, parameters);
        }

        /// <summary>
        /// Log messages for stations with information severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="stationId">The id of the station.</param>
        /// <param name="title">The title of the message.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogStationInformation(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="stationId">The id of the station.</param>
        /// <param name="title">The title of the message.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        [PublicAPI]
        public void LogStationWarning(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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
        /// <param name="code">The log message code.</param>
        /// <param name="stationId">The id of the station.</param>
        /// <param name="title">The title of the message.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        [PublicAPI]
        public void LogStationError(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
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

        /// <summary>
        /// Log messages for the system, with information severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        public void LogSystemInformation(int code, string title, string message, Dictionary<string, object?>? additionalLabels = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                { "Timestamp:yyyy-MM-dd HH:mm:ss.fffff", DateTime.UtcNow },
                { "LogCode", code },
                { "LogCategory", nameof(Categories.System) },
                { "Title", title },
                { "Message", message }
            };
            AddLabels(parameters, additionalLabels);
            WriteLogLine(logger, LogLevel.Information, message, parameters);
        }
    }

    private static void WriteLogLine(ILogger logger, LogLevel severity, string message, Dictionary<string, object?> parameters)
    {
        // Convert Dictionary to IReadOnlyList<KeyValuePair<string, object?>> for proper structured logging support.
        // This ensures logging providers (e.g., Loki/Serilog) can extract individual key-value pairs as structured properties.
        IReadOnlyList<KeyValuePair<string, object?>> state = parameters.ToList();

        logger.Log(severity, new EventId(), state, null, (s, _) =>
        {
            var sb = new StringBuilder();
            sb.Append(message);
            foreach (KeyValuePair<string, object?> param in s)
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

