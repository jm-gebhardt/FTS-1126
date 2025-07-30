using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler(IHostOrderRepository hostOrderRepository, ILogger<ActionGroupEventHandler> logger, KaeserGraphConfiguration configuration) :
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupInvalid>,
    IConsumer<PickActionCompleted>,
    IConsumer<ActionAborted>,
    IConsumer<DroppingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<ActionGroupReassignedOrReset>
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
        try
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            if (configuration.IsDropAtAkl(hostOrder.Target))
            {
                string? pickTarget = configuration.GetDoppelspielPickTargetForDrop(hostOrder.Target);
                if (pickTarget != null && hostOrder.AssignedAgv != null)
                {
                    logger.LogInformation("Pick action of drop at akl target {Target} run by agv {AgvCorrelationId} finished, try queuing next pick for {PickTarget}", hostOrder.Target, hostOrder.AssignedAgv, pickTarget);
                    await context.Publish(new TryQueueNextAklPick(pickTarget, hostOrder.AssignedAgv.Value));
                }
            }
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Order {CorrelationId} not created by prismat", correlationId);
        }

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
    /// Handles the <see cref="ActionAborted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        (_, var action, bool isTemporary) = context.Message;

        if (isTemporary)
        {
            return;
        }
        await context.Publish(new AbortHostOrder(action.ParentId));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupReassignedOrReset"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupReassignedOrReset> context)
    {
        var (actionGroupId, creator) = context.Message;

        if (creator != ActionGroupCreator.Wms)
        {
            return;
        }

        await context.Publish(new ResetDoppelspielPartner(actionGroupId));
    }
}