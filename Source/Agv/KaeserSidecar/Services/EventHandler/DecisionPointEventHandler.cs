using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles messages related to the decision trigger point
/// </summary>
[UsedImplicitly]
public class DecisionPointEventHandler (ILogger<DecisionPointEventHandler> logger, IStationRepository stationRepository, IPrismatCommunicationHostOrderRepository hostOrderRepository, IConveyorCommunicator conveyorCommunicator, KaeserGraphConfiguration configuration): 
    IConsumer<AgvPassedDecisionPoint1>
{
    private static string _lastChosenStation = string.Empty;

    /// <summary>
    /// Handles the <see cref="AgvPassedDecisionPoint1"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (var _, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, int consideredStations) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.NodeId != string.Empty))
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }

        string targetNode = await SelectTarget(context, actionGroup, consideredStations, agvStatus);
        await ReportTargetToConveyor(targetNode, actionGroup);
    }

    private async Task<string> SelectTarget(ConsumeContext<AgvPassedDecisionPoint1> context, ActionGroup? actionGroup, int consideredStations, AgvStatus agvStatus)
    {
        ICollection<StationOccupancy> occupancies = stationRepository.GetStationOccupancy();
        var action = actionGroup!.Actions.First(a => a.Target.NodeId == string.Empty).CorrelationId;

        //Take station that is not occupied and was not chosen last (to compensate for the delay between setting target and starting action)
        foreach (var occupancy in occupancies.Take(consideredStations).OrderBy(o => o.Station))
        {
            if (occupancy.Count != 0 || _lastChosenStation == occupancy.Station)
            {
                continue;
            }
            await SetTarget(context, occupancy.Station, agvStatus, actionGroup, action);
            return occupancy.Station;
        }
        // or the one of 1&2 that is least occupied and was not chosen last 
        string target = occupancies.OrderBy(o => o.Station).Take(2).OrderBy(o => o.Count).First(o => o.Station != _lastChosenStation).Station;
        await SetTarget(context, target, agvStatus, actionGroup, action);
        return target;
    }

    private async Task SetTarget(IPublishEndpoint context, string target, AgvStatus agvStatus, ActionGroup actionGroup, Guid action)
    {
        _lastChosenStation = target;
        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvStatus.CorrelationId, actionGroup.CorrelationId);
        await context.Publish(new SetTargetForAction(action, actionGroup.CorrelationId, agvStatus.CorrelationId, target));
    }

    private async Task ReportTargetToConveyor(string targetNode, ActionGroup actionGroup)
    {
        if (!configuration.ConveyorStationMapping.TryGetValue(targetNode, out string? targetStation))
        {
            logger.LogError("No mapping of target node {Node} to station naming in conveyor communication configured ", targetNode);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.NodeId == string.Empty);
        bool isDropping = action is { ActionType: ActionType.Drop };
        string? pickTarget = await GetPickStation(actionGroup, isDropping);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, isDropping, pickTarget);
    }

    private async Task<string?> GetPickStation(ActionGroup actionGroup, bool isDropping)
    {
        string? pickTarget = null;
        HostOrder? pendingPickOrder;
        if (isDropping)
        {
            pendingPickOrder = await hostOrderRepository.GetNextIncompleteOrderWithUnknownPickTarget();
            if (pendingPickOrder != null)
            {
                pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            }
            return pickTarget;
        }

        pendingPickOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        return pickTarget;
    }
}