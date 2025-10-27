using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles log entry related commands.
/// </summary>
[UsedImplicitly]
public class LogEntryCommandHandler(ILogger<LogEntryCommandHandler> logger, ILoggingService loggingService) : IConsumer<PersistLogEntry>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PersistLogEntry> context)
    {
        var entry = context.Message.Entry;

        if (!Enum.TryParse(entry.LogLevel, out LogLevel level))
        {
            logger.LogWarning("Could not parse log level {level}", entry.LogLevel);
            return;
        }

        Dictionary<string, object?> variables = entry.Variables.ToDictionary(kv => kv.Key, object? (kv) => kv.Value);

        switch (entry.Category)
        {
            case Categories.Battery:
                await loggingService.LogBattery(logger, level, entry.LogCode, entry.AgvCorrelationId!.Value, null,
                    "Log entry received",
                    $"Received {level} {entry.Category.ToString()} log entry", variables);
                break;

            case Categories.Agv:
                await loggingService.LogAgv(logger, level, entry.LogCode, entry.AgvCorrelationId!.Value, null,
                    "Log entry received",
                    $"Received {level} {entry.Category.ToString()} log entry", variables);
                break;

            case Categories.Order:
                await loggingService.LogOrder(logger, level, entry.LogCode, entry.AgvCorrelationId!.Value, entry.ActionGroupCorrelationId!.Value,
                    "Log entry received",
                    $"Received {level} {entry.Category.ToString()} log entry", variables);
                break;

            case Categories.Station:
                await loggingService.LogStation(logger, level, entry.LogCode, entry.StationId!.Value,
                    "Log entry received",
                    $"Received {level} {entry.Category.ToString()} log entry", variables);
                break;

            case Categories.System:
                await loggingService.LogSystem(logger, entry.LogCode,
                    "Log entry received",
                    $"Received {level} {entry.Category.ToString()} log entry", variables);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}