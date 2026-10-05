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
    private const string TimestampKey = "Timestamp:yyyy-MM-dd HH:mm:ss.fffff";
    private const string LogCodeKey = "LogCode";
    private const string AgvCorrelationIdKey = "AgvCorrelationId";
    private const string ActionGroupCorrelationIdKey = "ActionGroupCorrelationId";
    private const string LogCategoryKey = "LogCategory";
    private const string TitleKey = "Title";
    private const string MessageKey = "Message";
    private const string ErrorCodeKey = "ErrorCode";
    private const string StationIdKey = "StationId";

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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogBatteryInformation(int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParameters(timeProvider ?? TimeProvider.System, code, nameof(Categories.Battery), agvId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogBatteryError(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParametersWithError(timeProvider ?? TimeProvider.System, code, nameof(Categories.Battery), agvId, errorCode, title, message);
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
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogBatteryWarning(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParametersWithError(timeProvider ?? TimeProvider.System, code, nameof(Categories.Battery), agvId, errorCode, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogAgvInformation(int code, Guid agvId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParameters(timeProvider ?? TimeProvider.System, code, nameof(Categories.Agv), agvId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogAgvError(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParametersWithError(timeProvider ?? TimeProvider.System, code, nameof(Categories.Agv), agvId, errorCode, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogAgvWarning(int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateAgvParametersWithError(timeProvider ?? TimeProvider.System, code, nameof(Categories.Agv), agvId, errorCode, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogOrderInformation(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateOrderParameters(timeProvider ?? TimeProvider.System, code, agvId, actionGroupId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogOrderWarning(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateOrderParameters(timeProvider ?? TimeProvider.System, code, agvId, actionGroupId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogOrderError(int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateOrderParameters(timeProvider ?? TimeProvider.System, code, agvId, actionGroupId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogStationInformation(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateStationParameters(timeProvider ?? TimeProvider.System, code, stationId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        [PublicAPI]
        public void LogStationWarning(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateStationParameters(timeProvider ?? TimeProvider.System, code, stationId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        [PublicAPI]
        public void LogStationError(int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateStationParameters(timeProvider ?? TimeProvider.System, code, stationId, title, message);
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
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        public void LogSystemInformation(int code, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateSystemParameters(timeProvider ?? TimeProvider.System, code, title, message);
            AddLabels(parameters, additionalLabels);
            WriteLogLine(logger, LogLevel.Information, message, parameters);
        }

        /// <summary>
        /// Log messages for the system with warning severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        [PublicAPI]
        public void LogSystemWarning(int code, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateSystemParameters(timeProvider ?? TimeProvider.System, code, title, message);
            AddLabels(parameters, additionalLabels);
            WriteLogLine(logger, LogLevel.Warning, message, parameters);
        }

        /// <summary>
        /// Log messages for the system with error severity.
        /// </summary>
        /// <param name="code">The log message code.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="additionalLabels">Additional labels for values used in the log message.</param>
        /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
        [PublicAPI]
        public void LogSystemError(int code, string title, string message, Dictionary<string, object?>? additionalLabels = null, TimeProvider? timeProvider = null)
        {
            var parameters = CreateSystemParameters(timeProvider ?? TimeProvider.System, code, title, message);
            AddLabels(parameters, additionalLabels);
            WriteLogLine(logger, LogLevel.Error, message, parameters);
        }
    }

    private static Dictionary<string, object?> CreateAgvParameters(TimeProvider timeProvider, int code, string category, Guid agvId, string title, string message) =>
        new()
        {
            { TimestampKey, timeProvider.GetUtcNow().UtcDateTime },
            { LogCodeKey, code },
            { AgvCorrelationIdKey, agvId },
            { LogCategoryKey, category },
            { TitleKey, title },
            { MessageKey, message }
        };

    private static Dictionary<string, object?> CreateAgvParametersWithError(TimeProvider timeProvider, int code, string category, Guid agvId, string? errorCode, string title, string message)
    {
        var parameters = CreateAgvParameters(timeProvider, code, category, agvId, title, message);
        parameters[ErrorCodeKey] = errorCode;
        return parameters;
    }

    private static Dictionary<string, object?> CreateOrderParameters(TimeProvider timeProvider, int code, Guid agvId, Guid actionGroupId, string title, string message) =>
        new()
        {
            { TimestampKey, timeProvider.GetUtcNow().UtcDateTime },
            { LogCodeKey, code },
            { AgvCorrelationIdKey, agvId },
            { ActionGroupCorrelationIdKey, actionGroupId },
            { LogCategoryKey, nameof(Categories.Order) },
            { TitleKey, title },
            { MessageKey, message }
        };

    private static Dictionary<string, object?> CreateStationParameters(TimeProvider timeProvider, int code, Guid stationId, string title, string message) =>
        new()
        {
            { TimestampKey, timeProvider.GetUtcNow().UtcDateTime },
            { LogCodeKey, code },
            { StationIdKey, stationId },
            { LogCategoryKey, nameof(Categories.Station) },
            { TitleKey, title },
            { MessageKey, message }
        };

    private static Dictionary<string, object?> CreateSystemParameters(TimeProvider timeProvider, int code, string title, string message) =>
        new()
        {
            { TimestampKey, timeProvider.GetUtcNow().UtcDateTime },
            { LogCodeKey, code },
            { LogCategoryKey, nameof(Categories.System) },
            { TitleKey, title },
            { MessageKey, message }
        };

    private static void WriteLogLine(ILogger logger, LogLevel severity, string message, Dictionary<string, object?> parameters)
    {
        // Convert Dictionary to IReadOnlyList<KeyValuePair<string, object?>> for proper structured logging support.
        // This ensures logging providers (e.g., Loki/Serilog) can extract individual key-value pairs as structured properties.
        IReadOnlyList<KeyValuePair<string, object?>> state = parameters.ToList();

        logger.Log(severity, new EventId(), state, null, (s, _) =>
        {
            var sb = new StringBuilder();
            sb.Append(message);
            foreach (var param in s)
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
            foreach (var kvp in additionalLabels)
            {
                logParameters[kvp.Key] = kvp.Value;
            }
        }
    }
}