using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler:
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupStarted>,
    IConsumer<SetTargetForAction>,
    IConsumer<AgvPassedLoadTriggerPoint>,
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
    /// Handles the <see cref="AgvPassedLoadTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedLoadTriggerPoint> context)
    {
        (var agv, Guid? actionGroupId) = context.Message;
        await context.Publish(new PassLoadTriggerPoint(agv, actionGroupId.GetValueOrDefault()));
    }

    /// <summary>
    /// Handles the <see cref="PickActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, timestamp) = context.Message;
        await context.Publish(new FinishPick(correlationId, actionCorrelationId, timestamp));
    }
}