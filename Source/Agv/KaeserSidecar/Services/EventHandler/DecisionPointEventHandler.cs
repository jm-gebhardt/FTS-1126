using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles messages related to the decision trigger point
/// </summary>
[UsedImplicitly]
public class DecisionPointEventHandler(
    ILogger<DecisionPointEventHandler> logger,
    IStationRepository stationRepository,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IConveyorCommunicator conveyorCommunicator,
    KaeserGraphConfiguration configuration,
    IAgvSettingsService agvSettingsService,
    ITransportOrderDb transportOrderDb) : 
    IConsumer<AgvPassedDecisionPoint1>
{
    private string _lastChosenStation = string.Empty;

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
        bool hasLowBatteryLevel = await agvSettingsService.CheckAgvHasLowBatteryLevel(actionGroup.AssignedAgv.GetValueOrDefault());
        string? targetNode = await SelectTarget(consideredStations);
        if (targetNode == null)
        {
            logger.LogError("Could not select target for action group {ActionGroupId}", actionGroup.CorrelationId);
            return;
        }
        await ReportTarget(context, agvStatus, targetNode, actionGroup, hasLowBatteryLevel);
    }

    private async Task<string?> SelectTarget(int consideredStations)
    {
        ICollection<StationOccupancy> occupancies = stationRepository.GetStationOccupancy();
        ICollection<Station> stations = await transportOrderDb.Stations.GetStationsByNames(configuration.ConveyorStationMapping.Keys.ToArray());
       
        //Try to take the station that is not occupied, is not locked,  and was not chosen last (to compensate for the delay between setting target and starting action)
        ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
        foreach (var occupancy in availableStations)
        {
            if (occupancy.Count != 0 || (_lastChosenStation == occupancy.Station && availableStations.Count > 1))
            {
                continue;
            }
            return occupancy.Station;
        }

        // or the one of WE01&WE02 that is least occupied and was not chosen last and is not locked
        availableStations = GetAvailableStations(Math.Min(consideredStations, 2), occupancies, stations);
        foreach (var occupancy in availableStations.OrderBy(o => o.Count))
        {
            if (_lastChosenStation != occupancy.Station || availableStations.Count <= 1)
            {
                return occupancy.Station;
            }
        }
        return null;
    }

    private ICollection<StationOccupancy> GetAvailableStations(int consideredStations, ICollection<StationOccupancy> unorderedOccupancies, ICollection<Station> stations)
    {
        // By agreement conveyor stations are named "WE01", "WE02", "WE03", thus ordering by conveyor station
        // name ensures priority of WE01 over WE02 and WE03
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value); 
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Take(consideredStations))
        {
            var occupancy = unorderedOccupancies.SingleOrDefault(o => o.Station == conveyorStationMapping.Key) ??
                            new StationOccupancy(conveyorStationMapping.Key, 0);
            if (!stations.First(s => s.Name == occupancy.Station).IsLocked)
            {
                orderedOccupancies.Add(occupancy);
            }
        }
        return orderedOccupancies;
    }

    private async Task SetTarget(IPublishEndpoint context, string target, AgvStatus agvStatus, ActionGroup actionGroup, Guid action, bool shouldNotPickNext)
    {
        _lastChosenStation = target;
        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvStatus.CorrelationId, actionGroup.CorrelationId);
        await context.Publish(new SetTargetForAction(action, actionGroup.CorrelationId, agvStatus.CorrelationId, target, shouldNotPickNext));
    }

    private async Task ReportTarget(IPublishEndpoint context, AgvStatus agvStatus, string targetNode, ActionGroup actionGroup, bool hasLowBatteryLevel)
    {
        if (!configuration.ConveyorStationMapping.TryGetValue(targetNode, out string? targetStation))
        {
            logger.LogError("No mapping of target node {Node} to station naming in conveyor communication configured ", targetNode);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.NodeId == string.Empty);
        bool isDropping = action is { ActionType: ActionType.Drop };
        string? pickTarget = await GetPickStation(actionGroup, isDropping, hasLowBatteryLevel);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, isDropping, pickTarget);
        bool shouldNotPickNext = hasLowBatteryLevel || pickTarget == null;
        await SetTarget(context, targetNode, agvStatus, actionGroup, action!.CorrelationId, shouldNotPickNext);
    }

    private async Task<string?> GetPickStation(ActionGroup actionGroup, bool isDropping, bool hasLowBatteryLevel)
    {
        string? pickTarget = null;
        HostOrder? pendingPickOrder;
        if (isDropping && hasLowBatteryLevel)
        {
            logger.LogInformation("Reporting long loop order {Order} for dropping at WE03 at {Time}", actionGroup.OrderId, DateTime.UtcNow);
            return pickTarget;
        }
        if (isDropping)
        {
            pendingPickOrder = await hostOrderRepository.GetNextIncompleteOrderWithUnknownPickTarget();
            if (pendingPickOrder != null)
            {
                pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
                logger.LogInformation("Reporting long loop order {Order} for picking at target {Target} after dropping order {DropOrder} at {Time}", pendingPickOrder.OrderId, pickTarget, actionGroup.OrderId, DateTime.UtcNow);
            }
            return pickTarget;
        }

        pendingPickOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        logger.LogInformation("Reporting long loop order {Order} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pickTarget, DateTime.UtcNow);
        return pickTarget;
    }
}