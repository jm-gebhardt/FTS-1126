using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler :
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupInvalid>,
    IConsumer<PickActionCompleted>,
    IConsumer<ActionAborted>,
    IConsumer<DroppingAgvPassedDoppelspielTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (actionGroup, correlationId, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new FinishHostOrder(correlationId, timestamp, actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupStarted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, _, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, timestamp));
        }
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
    /// Handles the <see cref="DroppingAgvPassedDoppelspielTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<DroppingAgvPassedDoppelspielTriggerPoint> context)
    {
        (_, var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext, string? pickNode) = context.Message;
        await context.Publish(new PassTriggerPoint(agvCorrelationId, droppingHostOrderCorrelationId, nextPickHostOrderCorrelationId, shouldPickNext, pickNode));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupInvalid"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupInvalid> context)
    {
        (var correlationId, _, ICollection<string> validationErrors) = context.Message;

        if (validationErrors.Contains(HostOrderFailureReasons.ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup))
        {
            await context.Publish(new CancelInvalidHostOrder(correlationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupInvalid"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var (_, action) = context.Message;

        await context.Publish(new AbortHostOrder(action.ParentId));
    }
}