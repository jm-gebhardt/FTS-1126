using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles messages related to the decision trigger point
/// </summary>
[UsedImplicitly]
public class DecisionPointEventHandler (ILogger<DecisionPointEventHandler> logger, IStationRepository stationRepository): 
    IConsumer<AgvPassedDecisionPoint1>
{
    private string _lastChosenStation = string.Empty;

    /// <summary>
    /// Handles the <see cref="AgvPassedDecisionPoint1"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (var _, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, int consideredStations) = context.Message;
        ICollection<StationOccupancy> occupancies = stationRepository.GetStationOccupancy();
        
        var action = actionGroup!.Actions.First(a => a.State == ActionState.Started).CorrelationId;

        //Take station that is not occupied and was not chosen last (to compensate for the delay between setting target and starting action)
        foreach (var occupancy in occupancies.Take(consideredStations).OrderBy(o => o.Station))
        {
            if (occupancy.Count != 0 || _lastChosenStation == occupancy.Station)
            {
                continue;
            }
            await SetTarget(context, occupancy.Station, agvStatus, actionGroup, action);
            return;
        }
        // or the one of 1&2 that is least occupied and was not chosen last 
        string target = occupancies.OrderBy(o => o.Station).Take(2).OrderBy(o => o.Count).First(o => o.Station != _lastChosenStation).Station;
        await SetTarget(context, target, agvStatus, actionGroup, action);
    }

    private async Task SetTarget(IPublishEndpoint context, string target, AgvStatus agvStatus, ActionGroup actionGroup, Guid action)
    {
        _lastChosenStation = target;
        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvStatus.CorrelationId, actionGroup.CorrelationId);
        await context.Publish(new SetTargetForAction(action, actionGroup.CorrelationId, agvStatus.CorrelationId, target));
    }
}