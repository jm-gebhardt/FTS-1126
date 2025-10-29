using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles station-related events.
/// </summary>
[UsedImplicitly]
public class StationEventHandler(
    KaeserGraphConfiguration configuration,
    IShortLoopWeTargetStationRepository targetStationRepository,
    IStationRepository stationRepository,
    ILogger<StationEventHandler> logger) :
    IConsumer<StationLocked>,
    IConsumer<StationUnlocked>,
    IConsumer<AgvLeftStation>,
    IConsumer<FreeStationOccupancy>,
    IConsumer<UnblockStation>,
    IConsumer<OccupyStation>
{
    /// <summary>
    /// Handles the <see cref="StationLocked"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StationLocked> context)
    {
        string lockedStation = context.Message.StationNodeId;

        await targetStationRepository.LockStation(lockedStation);

        var shortLoopTarget = await targetStationRepository.GetTargetStation();

        if (lockedStation == shortLoopTarget.Current)
        {
            shortLoopTarget.UseNextAvailable(configuration);
            configuration.SetShortLoopTarget(shortLoopTarget.Current);
            await targetStationRepository.UpdateAsync(shortLoopTarget);
            logger.LogInformation("Short loop target {StationName} was locked, using {Target} from now on", lockedStation, shortLoopTarget.Current);
        }
    }

    /// <summary>
    /// Handles the <see cref="StationUnlocked"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        string unlockedStation = context.Message.StationNode.Name;

        await targetStationRepository.UnlockStation(unlockedStation);

        var shortLoopTarget = await targetStationRepository.GetTargetStation();

        if (unlockedStation == shortLoopTarget.Primary && shortLoopTarget.Current != shortLoopTarget.Primary)
        {
            shortLoopTarget.Reset();
            configuration.SetShortLoopTarget(shortLoopTarget.Current);
            await targetStationRepository.UpdateAsync(shortLoopTarget);
            logger.LogInformation("Original short loop target {StationName} was unlocked, using {Target} from now on", unlockedStation, shortLoopTarget.Current);
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvLeftStation"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvLeftStation> context)
    {
        logger.LogInformation("Reporting station free with {Source} and target {Target}", context.Message.Source, context.Message.Target);
        await context.Publish(new TryQueueDelayedOrder(context.Message.Source, context.Message.Target));
    }

    /// <summary>
    /// Handles the <see cref="FreeStationOccupancy"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStationOccupancy> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var station = await stationRepository.GetStationByOccupyingAgv(agvCorrelationId);
        if (station?.NodeId != context.Message.NodeId)
        {
            logger.LogWarning("AGV {AgvCorrelationId} is not occupying station {StationId}, but {OccupiedStation}", agvCorrelationId, context.Message.NodeId, station?.NodeName ?? "none");
            return;
        }

        await context.Publish(new RemoveAgvFromStation(station.Id, agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="UnblockStation"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        var stationId = context.Message.StationId;

        var station = await stationRepository.GetStationById(stationId);

        if (station == null)
        {
            logger.LogWarning("Cannot unblock station {StationId}; No station with that Id exists", stationId);
            return;
        }

        if (station.AgvCorrelationId == null)
        {
            logger.LogWarning("Cannot unblock station {StationId}; It is not blocked", stationId);
            return;
        }

        await context.Publish(new RemoveAgvFromStation(station.Id, station.AgvCorrelationId.Value));
    }

    /// <summary>
    /// Handles the <see cref="OccupyStation"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        var (node, agvCorrelationId) = context.Message;
        var occupiedStation = await stationRepository.GetStationByOccupyingAgv(agvCorrelationId);

        if (occupiedStation != null && occupiedStation.NodeId == node.Id)
        {
            logger.LogWarning("Agv {AgvCorrelationId} already occupies station {NodeName}", agvCorrelationId, node.Name);
            return;
        }
        if (occupiedStation != null)
        {
            logger.LogWarning("Agv {AgvCorrelationId} still occupies another station {Station}, removing it before occupying {NodeId}", agvCorrelationId, occupiedStation.NodeName, node.Name);
            await context.Publish(new RemoveAgvFromStation(occupiedStation.Id, agvCorrelationId));
        }

        var station = await stationRepository.GetStationByNodeId(node.Id);
        if (station != null)
        {
            await context.Publish(new AddAgvToStation(station.Id, agvCorrelationId));
            return;
        }
        logger.LogDebug("Station {NodeId} not found, cannot occupy", node.Name);
    }
}