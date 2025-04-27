using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
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
    IPrismatCommunicationHostOrderRepository hostOrderRepository) :
    IConsumer<ActionStarted>,
    IConsumer<ActionCompleted>
{
    /// <summary>
    /// Handles the <see cref="ActionStarted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionStarted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);

        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (action == null)
        {
            logger.LogWarning("Action {ActionCorrelationId} not found", actionCorrelationId);
            return;
        }
        string? station = action.Nodes.FirstOrDefault();
        if (station == null)
        {
            logger.LogWarning("Action {ActionCorrelationId} has no start node", actionCorrelationId);
            return;
        }
        bool gotPickAssignedOnStation = action.Nodes.First() == action.Nodes.Last();

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
    /// Handles the <see cref="ActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;

        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (action == null)
        {
            return;
        }

        if (actionGroup.IsMovingCargo)
        {
            var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);

            if (action.ActionType == ActionType.Pick)
            {
                string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
                {
                    await context.Publish(new AgvFinishedPick(order.CorrelationId, pickTarget));
                }
            }

            if (action.ActionType == ActionType.Drop)
            {
                string dropTarget = order.Actions.Single(a => a.ActionType == ActionType.Drop).Node;
                if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
                {
                    await context.Publish(new AgvFinishedDrop(order.CorrelationId, dropTarget));
                }
            }
        }

        string station = action.Target.NodeId!;
        await context.Publish( new OccupyStation(station, agvCorrelationId));
    }
}