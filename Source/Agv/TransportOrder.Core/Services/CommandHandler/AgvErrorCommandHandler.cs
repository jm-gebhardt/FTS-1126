using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain;
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
        (bool errorChanged, List<AgvError> newErrors, List<AgvError> resolvedErrors) = await _agvDb.Errors.AddOrUpdateAgvErrorStatus(errors, correlationId);
        _logger.LogDebug("Received {Message} with {Errors} errors. Error changed: {ErrorChanged}", nameof(UpdateErrorStatus), errors, errorChanged);
        if (errorChanged)
        {
            _metricCollector.CollectSafetyFieldViolationMetrics(agv.CorrelationId, newErrors, resolvedErrors);
            await context.Publish(new AgvErrorChanged(context.Message.CorrelationId, errors));
        }
        foreach (var error in newErrors)
        {
            await context.Publish(new AgvErrorReceived(error, position, agv.SerialNumber));
        }
    }
}