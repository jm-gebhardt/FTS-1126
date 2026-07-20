using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler.Metrics;

/// <summary>
/// Measures the latency between an action being dispatched and the agv reporting it started.
/// </summary>
[UsedImplicitly]
public class ActionStartLatencyEventHandler(IMetricCollectionService metricCollectionService) :
    IConsumer<StartActionCommand>,
    IConsumer<AbortAndStartActionCommand>,
    IConsumer<ActionStartedByAgv>,
    IConsumer<ActionAbortedByAgv>,
    IConsumer<ActionFailedByAgv>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<StartActionCommand> context)
    {
        metricCollectionService.RecordActionDispatched(context.Message.Action.CorrelationId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        metricCollectionService.RecordActionDispatched(context.Message.Action.CorrelationId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ActionStartedByAgv> context)
    {
        metricCollectionService.RecordActionStarted(context.Message.CorrelationId, context.Message.AgvCorrelationId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ActionAbortedByAgv> context)
    {
        metricCollectionService.DiscardDispatched(context.Message.CorrelationId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ActionFailedByAgv> context)
    {
        metricCollectionService.DiscardDispatched(context.Message.CorrelationId);
        return Task.CompletedTask;
    }
}