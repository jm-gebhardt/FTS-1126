using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler(KaeserGraphConfiguration configuration) :
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupStarted>,
    IConsumer<SetTargetForAction>,
    IConsumer<AgvPassedCargoTriggerPoint>,
    IConsumer<AgvPassedShortLoopTriggerPoint>,
    IConsumer<PickActionCompleted>
{
    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (actionGroup, correlationId, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new FinishHostOrder(correlationId, timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupStarted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, _, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms)
        {
            await context.Publish(new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="SetTargetForAction"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<SetTargetForAction> context)
    {
        (_, var actionGroupId, Guid? assignedAgv, string targetNodeId) = context.Message;
        await context.Publish(new PassDecisionPoint(actionGroupId, assignedAgv, targetNodeId));
    }

    /// <summary>
    /// Handles the <see cref="AgvPassedCargoTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedCargoTriggerPoint> context)
    {
        (var agv, Guid? actionGroupId, bool _) = context.Message;
        await context.Publish(new PassCargoTriggerPoint(agv, actionGroupId.GetValueOrDefault()));
    }

    /// <summary>
    /// Handles the <see cref="PickActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, timestamp) = context.Message;
        await context.Publish(new FinishPick(correlationId, actionCorrelationId, timestamp));
    }

    /// <summary>
    /// Handles the <see cref="AgvPassedShortLoopTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedShortLoopTriggerPoint> context)
    {
        (var agvData, _, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            return;
        }
        var action = actionGroup.Actions.First(a => a.State == ActionState.Started);
        if (!configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
        {
            return;
        }
        await context.Publish(new PassShortLoopTriggerPoint(agvData.CorrelationId, actionGroup.CorrelationId));
    }
}