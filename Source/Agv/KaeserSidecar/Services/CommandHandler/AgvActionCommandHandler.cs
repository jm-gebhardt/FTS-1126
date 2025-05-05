using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;

/// <summary>
/// Handles agv action related messages.
/// </summary>
[UsedImplicitly]
public class AgvActionCommandHandler(
    ILogger<AgvActionCommandHandler> logger,
    ITransportOrderDb transportOrderDb,
    KaeserGraphConfiguration configuration,
    IConveyorCommunicator conveyorCommunicator,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    ITargetSelector targetSelector) :
    IConsumer<StartActionCommand>,
    IConsumer<PickActionCompleted>,
    IConsumer<DropActionCompleted>,
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionAborted>
{
    /// <summary>
    /// Handles the <see cref="StartActionCommand"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var action = context.Message.Action;
        var actionGroup = await transportOrderDb.ActionGroups.GetActionGroupAsync(action.ParentId);
        if (actionGroup == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", action.CorrelationId, action.ParentId);
            return;
        }

        logger.LogInformation("Handling start of action {CorrelationId} for agv {AgvCorrelationId}", action.CorrelationId, agvCorrelationId);
        string[] nodes = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId).Nodes;
        logger.LogInformation("Nodes of action: {Nodes}", string.Join(",", nodes));
        string? station = nodes.FirstOrDefault();
        if (station == null)
        {
            logger.LogWarning("Action {ActionCorrelationId} has no start node", action.CorrelationId);
            return;
        }
        bool gotPickAssignedOnStation = nodes.First() == nodes.Last();

        if (action.ActionType == ActionType.Pick && gotPickAssignedOnStation)
        {
            var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
            string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? targetStation))
            {
                await context.Publish(new AgvStartedPick(targetStation, order.CorrelationId, pickTarget));
                return;
            }
        }
        await context.Publish(new FreeStation(station, agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="PickActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, _) = context.Message;
        await HandleActionCompletion(context, correlationId, actionCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="DropActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<DropActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, _) = context.Message;
        await HandleActionCompletion(context, correlationId, actionCorrelationId);
    }

    private async Task HandleActionCompletion(IPublishEndpoint context, Guid correlationId, Guid actionCorrelationId)
    {
        var actionGroup = await transportOrderDb.ActionGroups.GetActionGroupAsync(correlationId);
        if (actionGroup == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, correlationId);
            return;
        }

        var agvCorrelationId = actionGroup.AssignedAgv!.Value;

        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (action == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        if (action.ShouldSkip)
        {
            logger.LogWarning("Ignoring completion of skipped action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        logger.LogInformation("Agv {AgvCorrelationId} finished {ActionType} {ActionId} of action group {ActionGroupCorrelationId}", actionGroup.AssignedAgv, action.ActionType, action.CorrelationId, actionGroup.CorrelationId);

        string station = action.Target.NodeId!; 
        await context.Publish(new OccupyStation(station, agvCorrelationId));

        if (actionGroup.IsManual)
        {
            return;
        }

        var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);

        switch (action.ActionType)
        {
            case ActionType.Pick:
            {
                string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
                {
                    await context.Publish(new AgvFinishedPick(order.CorrelationId, pickTarget));
                    await targetSelector.Dequeue(action.CorrelationId);
                }
                return;
            }

            case ActionType.Drop:
            {
                string dropTarget = order.Actions.Single(a => a.ActionType == ActionType.Drop).Node;
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
                {
                    await context.Publish(new AgvFinishedDrop(order.CorrelationId, dropTarget));
                    conveyorCommunicator.ReportPalletDrop(action.Target.NodeId!, dropTarget, order.Hu);
                    await targetSelector.Dequeue(action.CorrelationId);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var actionGroup = await transportOrderDb.ActionGroups.GetActionGroupAsync(context.Message.CorrelationId);
        if (actionGroup == null)
        {
            logger.LogWarning("Could not find group {ActionGroupCorrelationId} to handle action completion", context.Message.CorrelationId);
            return;
        }

        if (!actionGroup.IsManual)
        {
            return;
        }
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;

        var action = actionGroup.Actions.OrderBy(a => a.CompletedDate).Last();
        if (action.ActionType == ActionType.Drop)
        {
            return;
        }

        logger.LogInformation("Agv {AgvCorrelationId} finished {ActionType} {ActionId} of action group {ActionGroupCorrelationId}", action.ActionType, actionGroup.AssignedAgv, action.CorrelationId, actionGroup.CorrelationId);
        string station = action.Target.NodeId!;
        await context.Publish(new OccupyStation(station, agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var action = context.Message.Action;
        if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
        {
            await targetSelector.Dequeue(action.CorrelationId);
        }
    }
}
