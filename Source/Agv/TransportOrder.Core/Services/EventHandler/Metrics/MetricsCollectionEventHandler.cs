using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler.Metrics;

/// <summary>
/// Handles events that are relevant for metrics collection.
/// </summary>
[UsedImplicitly]
public class MetricsCollectionEventHandler(IMetricCollectionService metricCollectionService, IActionGroupRepository actionGroupRepository, IMapper mapper) :
    IConsumer<ActionAbortedByAgv>,
    IConsumer<PickActionCompleted>,
    IConsumer<DropActionCompleted>,
    IConsumer<ChargeActionCompleted>,
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionAborted>,
    IConsumer<SaveActionGroupAbortion>,
    IConsumer<AgvStatusReceived>,
    IConsumer<AgvErrorChanged>,
    IConsumer<JamStateChanged>,
    IConsumer<AgvOnlineChanged>
{
    /// <summary>
    /// Handles the <see cref="ActionAbortedByAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAbortedByAgv> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId);
        if (actionGroup.AssignedAgv == null || action.State is ActionState.Completed or ActionState.Canceled || actionGroup.AssignedAgv != context.Message.AgvCorrelationId)
        {
            return;
        }

        if (action.ActionType == ActionType.Charge)
        {
            // collect metrics for aborted charge actions
            await metricCollectionService.CollectStationOrderCompletionMetrics(action.ActionType, action.Target.NodeId!.Value);
        }
    }

    /// <summary>
    /// Handles the <see cref="PickActionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var actionCorrelationId = context.Message.ActionCorrelationId;
        await CollectStationOrderCompletionMetric(actionCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="DropActionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DropActionCompleted> context)
    {
        var actionCorrelationId = context.Message.ActionCorrelationId;
        await CollectStationOrderCompletionMetric(actionCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="ChargeActionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargeActionCompleted> context)
    {
        var actionCorrelationId = context.Message.ActionCorrelationId;
        await CollectStationOrderCompletionMetric(actionCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.ActionGroup.CorrelationId);
        await metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(context.Message.Action.CorrelationId);
        await metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="SaveActionGroupAbortion"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<SaveActionGroupAbortion> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(context.Message.AbortedActionId);
        await metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="AgvStatusReceived"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<AgvStatusReceived> context)
    {
        var agvStatus = mapper.Map<AgvStatus>(context.Message.AgvStatus);
        metricCollectionService.CollectBatteryChargeMetrics(agvStatus);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="AgvErrorChanged"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<AgvErrorChanged> context)
    {
        var (correlationId, _, newErrors, resolvedErrors) = context.Message;
        metricCollectionService.CollectSafetyFieldViolationMetrics(correlationId, newErrors, resolvedErrors);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="JamStateChanged"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<JamStateChanged> context)
    {
        (var stationId, bool currentJamState, var lastChangedDate, bool previousJamState, var currentChangeDate) = context.Message;
        metricCollectionService.CollectJamStateMetrics(stationId, lastChangedDate, previousJamState, currentChangeDate, currentJamState);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="AgvOnlineChanged"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<AgvOnlineChanged> context)
    {
        metricCollectionService.CollectOfflineDuration(context.Message.CorrelationId, context.Message.IsNowOnline);
        return Task.CompletedTask;
    }

    private async Task CollectStationOrderCompletionMetric(Guid actionCorrelationId)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId);
        await metricCollectionService.CollectStationOrderCompletionMetrics(action.ActionType, action.Target.NodeId!.Value);
    }
}