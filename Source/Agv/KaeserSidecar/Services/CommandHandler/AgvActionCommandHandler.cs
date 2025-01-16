using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
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
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IStationRepository stationRepository) :
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
        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId && configuration.ConveyorStationMapping.Keys.Contains(a.Nodes.FirstOrDefault()??""));
        if (action == null)
        {
            logger.LogWarning("Action {ActionCorrelationId} has no first node that is in the conveyor station mapping", actionCorrelationId);
            return;
        }
        string? station = action.Nodes.FirstOrDefault();

        if (action.ActionType == ActionType.Pick)
        {
            var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
            string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? targetStation))
            {
                await context.Publish(new AgvStartedPick(targetStation, order.CorrelationId, pickTarget));
            }
        }
        logger.LogInformation("Removing agv {Agv} from station {Station}", agvCorrelationId, station);
        stationRepository.FreeStation(station!, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="ActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId && configuration.ConveyorStationMapping.Keys.Contains(a.Target.NodeId!));
        if (action == null)
        {
            return;
        }

        if (action.ActionType == ActionType.Pick)
        {
            var order = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
            string pickTarget = order.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            if (configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? _))
            {
                await context.Publish(new AgvFinishedPick(order.CorrelationId, pickTarget));
            }
        }

        string station = action.Target.NodeId!;
        logger.LogInformation("Adding agv {Agv} to station {Station}", agvCorrelationId, station);
        stationRepository.OccupyStation(station, agvCorrelationId);
    }
}