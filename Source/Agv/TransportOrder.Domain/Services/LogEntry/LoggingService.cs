using AutoMapper;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;

/// <summary>
/// Wraps logging.
/// </summary>
public class LoggingService(ILogEntryRepository logEntryRepository, IBus bus, IMapper mapper): ILoggingService
{
    /// <inheritdoc cref="ILoggingService"/>>
    public async Task LogOrder(ILogger logger, LogLevel severity, int code, Guid agvId, Guid actionGroupId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var logEntry = await logEntryRepository.AddAsync(Categories.Order, code, severity.ToString(), agvId, actionGroupId, null, DateTime.UtcNow, additionalLabels ?? new Dictionary<string, object?>());
        switch (severity)
        {
            case LogLevel.Information:
            {
                logger.LogOrderInformation(code, agvId, actionGroupId, title, message, additionalLabels);
                break;
            }

            case LogLevel.Warning:
            {
                logger.LogOrderWarning(code, agvId, actionGroupId, title, message, additionalLabels);
                break;
            }
        }
        await bus.Publish(new LogEntryCreated(mapper.Map<LogEntryDto>(logEntry)));
    }

    /// <inheritdoc cref="ILoggingService"/>>
    public async Task LogAgv(ILogger logger, LogLevel severity, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var logEntry = await logEntryRepository.AddAsync(Categories.Agv, code, severity.ToString(), agvId, null, null, DateTime.UtcNow, additionalLabels ?? new Dictionary<string, object?>());
        switch (severity)
        {
            case LogLevel.Information:
            {
                logger.LogAgvInformation(code, agvId, title, message, additionalLabels);
                break;
            }

            case LogLevel.Warning:
            {
                logger.LogAgvWarning(code, agvId, errorCode, title, message, additionalLabels);
                break;
            }

            case LogLevel.Error:
            {
                logger.LogAgvError(code, agvId, errorCode, title, message, additionalLabels);
                break;
            }
        }
        await bus.Publish(new LogEntryCreated(mapper.Map<LogEntryDto>(logEntry)));
    }

    /// <inheritdoc cref="ILoggingService"/>>
    public async Task LogBattery(ILogger logger, LogLevel severity, int code, Guid agvId, string? errorCode, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var logEntry = await logEntryRepository.AddAsync(Categories.Battery, code, severity.ToString(), agvId, null, null, DateTime.UtcNow, additionalLabels ?? new Dictionary<string, object?>());
        switch (severity)
        {
            case LogLevel.Information:
            {
                logger.LogBatteryInformation(code, agvId, title, message, additionalLabels);
                break;
            }

            case LogLevel.Warning:
            {
                logger.LogBatteryWarning(code, agvId, errorCode, title, message, additionalLabels);
                break;
            }

            case LogLevel.Error:
            {
                logger.LogBatteryError(code, agvId, errorCode, title, message, additionalLabels);
                break;
            }
        }
        await bus.Publish(new LogEntryCreated(mapper.Map<LogEntryDto>(logEntry)));
    }

    /// <inheritdoc cref="ILoggingService"/>>
    public async Task LogStation(ILogger logger, LogLevel severity, int code, Guid stationId, string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        var logEntry = await logEntryRepository.AddAsync(Categories.Station, code, severity.ToString(), null, null, stationId, DateTime.UtcNow, additionalLabels ?? new Dictionary<string, object?>());
        switch (severity)
        {
            case LogLevel.Information:
            {
                logger.LogStationInformation(code, stationId, title, message, additionalLabels);
                break;
            }
                
            case LogLevel.Warning:
            {
                logger.LogStationWarning(code, stationId, title, message, additionalLabels);
                break;
            }

            case LogLevel.Error:
            {
                logger.LogStationError(code, stationId, title, message, additionalLabels);
                break;
            }
        }
        await bus.Publish(new LogEntryCreated(mapper.Map<LogEntryDto>(logEntry)));
    }

    /// <inheritdoc cref="ILoggingService"/>>
    public async Task LogSystem(ILogger logger,int code,  string title, string message, Dictionary<string, object?>? additionalLabels = null)
    {
        const LogLevel severity = LogLevel.Information;
        var logEntry = await logEntryRepository.AddAsync(Categories.Station, code, severity.ToString(), null, null, null, DateTime.UtcNow, additionalLabels ?? new Dictionary<string, object?>());
        logger.LogSystemInformation(code, title, message, additionalLabels);
        await bus.Publish(new LogEntryCreated(mapper.Map<LogEntryDto>(logEntry)));
    }
}