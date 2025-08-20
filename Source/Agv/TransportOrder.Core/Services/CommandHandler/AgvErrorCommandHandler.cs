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
public class AgvErrorCommandHandler :
    IConsumer<UpdateErrorStatus>,
    IConsumer<AddSystemAgvErrors>,
    IConsumer<UpdateSystemAgvErrors>,
    IConsumer<ResolveSystemAgvError>
{
    private readonly IAgvDb _agvDb;
    private readonly ILogger<AgvErrorCommandHandler> _logger;
    private readonly IMetricCollectionService _metricCollector;

    /// <inheritdoc cref="AgvErrorCommandHandler"/>>
    public AgvErrorCommandHandler(IAgvDb agvDb, ILogger<AgvErrorCommandHandler> logger, IMetricCollectionService metricCollector)
    {
        _agvDb = agvDb;
        _logger = logger;
        _metricCollector = metricCollector;
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

        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Information))
        {
            _logger.LogAgvInformation(LogCodes.AgvInformation, agv.CorrelationId, "AGV Information",
                $"AGV {agv.SerialNumber} sent information {agvError.ErrorDescription}",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.ErrorDescription, agvError.ErrorDescription }
                });
        }
    }

    private async Task HandleErrorChangesAsync(IPublishEndpoint publishEndpoint, List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid correlationId, AgvPosition? position)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        bool changed = newErrors.Count > 0 || resolvedErrors.Count > 0;
        if (changed)
        {
            await _agvDb.Errors.AddAndUpdateAgvErrorAsync(newErrors, resolvedErrors, correlationId);
            LogErrors(newErrors, agv);
            _metricCollector.CollectSafetyFieldViolationMetrics(correlationId, newErrors, resolvedErrors);
            IReadOnlyList<AgvError> allErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);
            await publishEndpoint.Publish(new AgvErrorChanged(correlationId, allErrors.ToList()));
        }

        foreach (var error in newErrors)
        {
            position ??= _agvDb.Positions.GetPosition(correlationId).AgvPosition;
            await publishEndpoint.Publish(new AgvErrorReceived(error, position, agv.SerialNumber));
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError> errors, var position) = context.Message;

        IReadOnlyList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);

        List<AgvError> newErrors = [];
        List<AgvError> previousErrors = activeErrors.ToList();
        List<AgvError> resolvedErrors = previousErrors.Where(p => !errors.Any(p.IsSameError) && p.Source != AgvErrorSource.System).ToList();

        if (errors.Count != 0)
        {
            newErrors = errors.Where(e => !previousErrors.Any(e.IsSameError)).ToList();
        }

        await HandleErrorChangesAsync(context, newErrors, resolvedErrors, correlationId, position);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddSystemAgvErrors> context)
    {
        (var correlationId, List<AgvError> newErrors, var position) = context.Message;

        await HandleErrorChangesAsync(context, newErrors, [], correlationId, position);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateSystemAgvErrors> context)
    {
        (var correlationId, List<AgvError> errorsToAdd, List<AgvError> errorsToResolve, var position) = context.Message;

        await HandleErrorChangesAsync(context, errorsToAdd, errorsToResolve, correlationId, position);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ResolveSystemAgvError> context)
    {
        var (correlationId, errors) = context.Message;

        await HandleErrorChangesAsync(context, [], errors, correlationId, null);
    }
}