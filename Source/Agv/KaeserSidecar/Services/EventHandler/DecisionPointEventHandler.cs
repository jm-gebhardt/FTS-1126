using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
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
    KaeserGraphConfiguration configuration,
    ITransportOrderDb transportOrderDb,
    TargetStation targetStation) : 
    IConsumer<AgvPassedDecisionPoint1>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedDecisionPoint1"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, int consideredStations) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.NodeId != string.Empty))
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        string? targetNode = await SelectTarget(consideredStations);
        if (targetNode == null)
        {
            logger.LogError("Could not select target for action group {ActionGroupId}", actionGroup.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.NodeId == string.Empty);
        await SetTarget(context, targetNode, agvStatus, actionGroup, action!.CorrelationId);
    }

    private async Task<string?> SelectTarget(int consideredStations)
    {
        string? selectedTarget = null;
        while (selectedTarget == null)
        {
            ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
            ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByNames(configuration.ConveyorStationMapping.Keys.ToArray());

            //Try to take the station that is not occupied, is not locked,  and was not chosen last (to compensate for the delay between setting target and starting action)
            ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
            foreach (var occupancy in availableStations)
            {
                if (occupancy.Count != 0 || (targetStation.Last == occupancy.Station && availableStations.Count > 1))
                {
                    continue;
                }
                selectedTarget = occupancy.Station;
                break;
            }
            if (selectedTarget == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
        
        return selectedTarget;
    }

    private ICollection<StationOccupancy> GetAvailableStations(int consideredStations, ICollection<StationOccupancy> unorderedOccupancies, ICollection<TransportOrder.Domain.Models.Station.Station> stations)
    {
        // By agreement conveyor stations are named "PS01", "PS02", "PS03", thus ordering by conveyor station
        // name ensures priority of PS01 over PS02 and PS03
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value); 
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Take(consideredStations))
        {
            var occupancy = unorderedOccupancies.SingleOrDefault(o => o.Station == conveyorStationMapping.Key) ??
                            new StationOccupancy(conveyorStationMapping.Key, 0, null);
            if (!stations.First(s => s.Name == occupancy.Station).IsLocked)
            {
                orderedOccupancies.Add(occupancy);
            }
        }
        return orderedOccupancies;
    }

    private async Task SetTarget(IPublishEndpoint context, string target, AgvStatus agvStatus, ActionGroup actionGroup, Guid action)
    {
        targetStation.Last = target;
        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvStatus.CorrelationId, actionGroup.CorrelationId);
        await context.Publish(new SetTargetForAction(action, actionGroup.CorrelationId, agvStatus.CorrelationId, target));
    }
}