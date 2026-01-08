using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Station;

/// <inheritdoc/>
public class StationAgvLimitGuard(ILogger<StationAgvLimitGuard> logger, IStationRepository stationRepository, IActionGroupRepository actionGroupRepository) : IStationAgvLimitGuard
{
    /// <inheritdoc/>
    public async Task<bool> IsWithinLimitAsync([NotNullWhen(true)] Models.ActionGroup.ActionGroup? actionGroup)
    {
        if (actionGroup == null)
        {
            logger.LogWarning("Action group is null, cannot check station AGV limit");
            return false;
        }
        var pickAction = actionGroup.Actions.FirstOrDefault(a => a is { ActionType: ActionType.Pick, ShouldSkip: false, IsIncomplete: false });
        var dropAction = actionGroup.Actions.FirstOrDefault(a => a is { ActionType: ActionType.Drop, ShouldSkip: false, IsIncomplete: false });
        if (dropAction == null && pickAction == null)
        {
            return true;
        }
        if (dropAction == null && pickAction?.Target.DoppelspielMode != DoppelspielMode.Always)
        {
            logger.LogInformation("Action group {CorrelationId} is not evaluated for station limit, drop target is not set yet", actionGroup.CorrelationId);
            return true;
        }

        var limitedStation = dropAction != null ? dropAction.Target.Node!.Id : pickAction!.Target.Node!.Id;

        var station = await stationRepository.GetStation(limitedStation);
        if (station.StationGroup.IsNullOrEmpty())
        {
            logger.LogInformation("Action group {CorrelationId} is not evaluated for station limit, station {Station} is not part of station group", actionGroup.CorrelationId, station.Name);
            return true;
        }

        if (await stationRepository.IsStationGroupOccupied(limitedStation))
        {
            logger.LogWarning("Action group {CorrelationId} is not within station AGV limit because station group of target {Target} is occupied", actionGroup.CorrelationId, station.Name);
            return false;
        }

        List<Guid> stationGroup = [.. station.StationGroup, station.Id];
        if (await actionGroupRepository.HasActionGroupsForStationGroup(stationGroup, actionGroup.AssignedAgv))
        {
            logger.LogWarning("Action group {CorrelationId} is not within station AGV limit because there are current action groups for the station group of target {Target}", actionGroup.CorrelationId, station.Name);
            return false;
        }
        logger.LogInformation("Action group {CorrelationId} is within station AGV limit for target {Target}", actionGroup.CorrelationId, station.Name);
        return true;
    }
}