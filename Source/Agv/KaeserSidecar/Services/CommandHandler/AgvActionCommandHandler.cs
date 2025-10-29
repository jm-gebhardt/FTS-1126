using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
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

        logger.LogInformation("Handling start of {ActionType} action {CorrelationId} for agv {AgvCorrelationId}", action.ActionType, action.CorrelationId, agvCorrelationId);
        var lastFinishedAction = await transportOrderDb.ActionGroups.GetLastCompletedActionAsync(agvCorrelationId);
        ICollection<GraphNode> nodes = action.Nodes;

        logger.LogInformation("Nodes of {ActionType} action: {Nodes}", action.ActionType, string.Join(",", nodes));
        //start node of current order is assumed to be the target node of last completed action of this AGV
        var startNode = lastFinishedAction?.Target.Node;
        if (startNode != null && startNode.Id != nodes.FirstOrDefault()?.Id)
        {
            logger.LogWarning("Target node of last completed action and first node of started action for AGV {AgvCorrelationId} differ. Last {LastTargetNode} Started {StartNode}", agvCorrelationId, startNode, nodes.FirstOrDefault());
        }
        if (startNode == null)
        {
            logger.LogWarning("Cannot determine start node for {ActionType} action {ActionCorrelationId} has no start node. Trying to get via node list", action.ActionType, action.CorrelationId);

            startNode = nodes.FirstOrDefault();
            if (startNode == null)
            {
                logger.LogWarning("Cannot finally determine start node for {ActionType} action {ActionCorrelationId}", action.ActionType, action.CorrelationId);
                return;
            }
        }
        bool gotPickAssignedOnStation = nodes.First().Id == nodes.Last().Id;

        if (action.ActionType == ActionType.Pick && gotPickAssignedOnStation && !actionGroup.IsManual)
        {
            var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
            string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            if (configuration.ConveyorStationMapping.TryGetValue(action.Target.Node!.Name, out string? targetStation))
            {
                logger.LogInformation("Agv {AgvCorrelationId} started {ActionType} {ActionCorrelationId} at station {Station}", agvCorrelationId, action.ActionType, action.CorrelationId, targetStation);
                await context.Publish(new AgvStartedPick(targetStation, order.CorrelationId, pickTarget));
                return;
            }
        }
        if (configuration.IsOccupancyLimitedNode(startNode.Name))
        {
            // free station only in case the start node is limited by agv occupancy.
            logger.LogInformation("{Message}: Agv {AgvCorrelationId} started {ActionType} action {ActionCorrelationId} at station {Station}, free station",
                nameof(StartActionCommand), agvCorrelationId, action.ActionType.ToString(), action.CorrelationId, startNode);
            await context.Publish(new FreeStationOccupancy(startNode.Id, agvCorrelationId));
        }
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

        var targetNode = action.Target.Node;
        if (!configuration.ConveyorStationMapping.TryGetValue(action.Target.Node!.Name, out string? _) || actionGroup.IsManual)
        {
            // Only publish for non-WE stations for automatic action groups, as WE stations are occupied already during target selection
            // For manual actions publish also for WE stations
            logger.LogInformation("{Message}: Agv {AgvCorrelationId} finished {ActionType} action {ActionId} for action group {ActionGroupCorrelationId}, occupying station {Station}",
                nameof(HandleActionCompletion), agvCorrelationId, action.ActionType, action.CorrelationId, actionGroup.CorrelationId, targetNode?.Name);
            await context.Publish(new OccupyStation(targetNode!, agvCorrelationId));
        }

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
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.Node!.Name, out string? _))
                {
                    await context.Publish(new AgvFinishedPick(order.CorrelationId, pickTarget));
                    await targetSelector.Dequeue(action.CorrelationId);
                }
                return;
            }

            case ActionType.Drop:
            {
                string dropTarget = order.Actions.Single(a => a.ActionType == ActionType.Drop).Node;
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.Node!.Name, out string? _))
                {
                    await context.Publish(new AgvFinishedDrop(order.CorrelationId, dropTarget));
                    conveyorCommunicator.ReportPalletDrop(action.Target.Node!.Name, dropTarget, order.Hu);
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

        if (!actionGroup.IsManual || actionGroup.IsChargingOrder)
        {
            return;
        }
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;

        var action = actionGroup.Actions.OrderBy(a => a.CompletedDate).Last();
        if (action is { ActionType: ActionType.Drop, ShouldSkip: false })
        {
            if (configuration.ConveyorStationMapping.TryGetValue(action.Target.Node!.Name, out string? _))
            {
                string? sap = action.GetSap();
                if (sap == null)
                {
                    logger.LogError("Sap not given on manual drop {ActionCorrelationId}, can not send tord", action.CorrelationId);
                    return;
                }
                if (actionGroup.HandlingUnit == null)
                {
                    logger.LogError("Hu not given for manual drop {ActionCorrelationId}, can not send tord", action.CorrelationId);
                    return;
                }
                conveyorCommunicator.ReportPalletDrop(action.Target.Node!.Name, sap, actionGroup.HandlingUnit);
            }
        }

        var station = action.Target.Node;
        if (station == null || (configuration.ConveyorStationMapping.TryGetValue(station.Name, out string? _) && !actionGroup.IsManual))
        {
            // If the station is a conveyor station, we have  already occupied it during the target selection
            return;
        }
        if (configuration.IsOccupancyLimitedNode(station.Name))
        {
            logger.LogInformation("{Message}: Agv {AgvCorrelationId} finished {ActionType} {ActionId} of action group {ActionGroupCorrelationId}, occupying station {Station}",
                nameof(ActionGroupCompleted), agvCorrelationId, action.ActionType, action.CorrelationId, actionGroup.CorrelationId, station);
            await context.Publish(new OccupyStation(station, agvCorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var action = context.Message.Action;
        var targetNode = action.Target.Node;
        if (configuration.ConveyorStationMapping.TryGetValue(targetNode!.Name, out string? _))
        {
            await targetSelector.Dequeue(action.CorrelationId);
        }
        var actionGroup = await transportOrderDb.ActionGroups.GetActionGroupAsync(action.ParentId);
        Guid? agvCorrelationId = actionGroup?.AssignedAgv;

        if (agvCorrelationId == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} or no AGV assigned to handle action aborted", action.CorrelationId, action.ParentId);
            return;
        }
        var agv = await transportOrderDb.Agvs.GetAgvAsync(agvCorrelationId.Value);
        if (configuration.IsOccupancyLimitedNode(targetNode.Name) && agv.AgvStatus?.LastNodeId != targetNode.Id)
        {
            logger.LogInformation("{Message}: Agv {AgvCorrelationId} aborted action {ActionCorrelationId} which was assigned to station {Station}, so try to free it",
                nameof(ActionAborted), action.CorrelationId, action.CorrelationId, targetNode.Name);
            await context.Publish(new FreeStationOccupancy(targetNode.Id, (Guid) agvCorrelationId));
        }
    }
}