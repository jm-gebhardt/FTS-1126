using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for agv error related commands.
/// </summary>
[UsedImplicitly]
public class AgvErrorCommandHandler : IConsumer<UpdateErrorStatus>
{
    private readonly IAgvDb _agvDb;
    private readonly ILogger<AgvErrorCommandHandler> _logger;
    private readonly IMetricCollectionService _metricCollector;

    /// <inheritdoc cref="AgvErrorCommandHandler"/>>
    public AgvErrorCommandHandler(IAgvDb agvDb, ILogger<AgvErrorCommandHandler> logger, IMetricCollectionService metricCollector )
    {
        _agvDb = agvDb;
        _logger = logger;
        _metricCollector = metricCollector;
    }

    /// <summary>
    /// Handles the <see cref="UpdateErrorStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError>? errors, var position) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        
        IList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(agv.CorrelationId);
        List<AgvError> newErrors = errors.Where(e => !activeErrors.Any(e.IsSameError)).ToList();
        LogErrors(newErrors, agv);

        (bool errorChanged, List<AgvError> newActiveErrors, List<AgvError> resolvedErrors) = await _agvDb.Errors.AddOrUpdateAgvErrorStatus(errors, correlationId);
        _logger.LogDebug("Received {Message} with {Errors} errors. Error changed: {ErrorChanged}", nameof(UpdateErrorStatus), errors, errorChanged);
        if (errorChanged)
        {
            _metricCollector.CollectSafetyFieldViolationMetrics(agv.CorrelationId, newActiveErrors, resolvedErrors);
            await context.Publish(new AgvErrorChanged(context.Message.CorrelationId, errors));
        }
        foreach (var error in newActiveErrors)
        {
            await context.Publish(new AgvErrorReceived(error, position, agv.SerialNumber));
        }
    }

    private void LogErrors(List<AgvError> errors, Agv agv)
    {
        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Fatal))
        {
            if (agvError.ErrorFlag == ErrorFlag.Battery)
            {
                _logger.LogBatteryError(LogCodes.BatteryError, agv.CorrelationId, agvError.ErrorCode, "Battery Error",
                    $"Battery of AGV {agv.SerialNumber} threw error {agvError.ErrorDescription}",
                    new Dictionary<string, object?>
                    {
                        { LogLabels.AgvSerialNumber, agv.SerialNumber },
                        { LogLabels.ErrorDescription, agvError.ErrorDescription }
                    });
                continue;
            }
            _logger.LogAgvError(LogCodes.AgvError, agv.CorrelationId, agvError.ErrorCode, "AGV Error",
                $"AGV {agv.SerialNumber} threw error {agvError.ErrorDescription}",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.ErrorDescription, agvError.ErrorDescription }
                });
        }
        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Warning))
        {
            if (agvError.ErrorFlag == ErrorFlag.Battery)
            {
                _logger.LogBatteryWarning(LogCodes.BatteryWarning, agv.CorrelationId, agvError.ErrorCode,
                    "Battery Warning", $"Battery of AGV {agv.SerialNumber} threw warning {agvError.ErrorDescription}",
                    new Dictionary<string, object?>
                    {
                        { LogLabels.AgvSerialNumber, agv.SerialNumber },
                        { LogLabels.ErrorDescription, agvError.ErrorDescription }
                    });
                continue;
            }
            _logger.LogAgvWarning(LogCodes.AgvWarning, agv.CorrelationId, agvError.ErrorCode, "AGV Warning",
                $"AGV {agv.SerialNumber} threw warning {agvError.ErrorDescription}",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.ErrorDescription, agvError.ErrorDescription }
                });
        }
    }
}