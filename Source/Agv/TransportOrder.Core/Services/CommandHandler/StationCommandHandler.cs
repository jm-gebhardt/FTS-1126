using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for station-related commands.
/// </summary>
/// <inheritdoc cref="StationCommandHandler"/>>
[UsedImplicitly]
public class StationCommandHandler(
    INodeStateRepository nodeStateRepository,
    IStationRepository stationRepository,
    IGraphRepository graphRepository,
    IMetricCollectionService metricCollector,
    ILogger<StationCommandHandler> logger) :
    IConsumer<LockStation>,
    IConsumer<UnlockStation>,
    IConsumer<Common.Messages.Maps.MapSaved>,
    IConsumer<UpdateStationJamState>
{
    /// <summary>
    /// Handles the <see cref="LockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<LockStation> context)
    {
        (var id, string user, string reason, string lockDescription, var lockOrderHandling) = context.Message;

        var station = await stationRepository.GetStation(id);

        bool lockResult = await nodeStateRepository.LockNodeAsync(station.Name);

        if (!lockResult)
        {
            logger.LogError("Lock of station {StationName} failed ", station.Name);
            return;
        }

        var nodeState = await nodeStateRepository.GetNodeStateAsync(station.Name);

        station.Lock(user, reason, lockDescription, lockOrderHandling);

        await stationRepository.Update(station);

        logger.LogStationInformation(LogCodes.StationLocked, id, "Station Locked",
            $"User {user} locked station because {reason}",
            new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Reason, reason } });
        await context.Publish(new StationLocked(station.Name));

        if (nodeState != null)
        {
            await context.Publish(new NodeStateChanged(nodeState, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
        }
    }

    /// <summary>
    /// Handles the <see cref="UnlockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnlockStation> context)
    {
        (var id, string user) = context.Message;

        var station = await stationRepository.GetStation(id);

        bool unlockResult = await nodeStateRepository.UnlockNodeAsync(station.Name);

        if (!unlockResult)
        {
            logger.LogError("Unlock of station {StationName} failed ", station.Name);
            return;
        }

        var nodeState = await nodeStateRepository.GetNodeStateAsync(station.Name);

        station.Unlock();
        await stationRepository.Update(station);

        if (nodeState != null)
        {
            await context.Publish(new NodeStateChanged(nodeState, station.Id, user, station.LockReason, station.LockDescription));
        }

        logger.LogStationInformation(LogCodes.StationUnlocked, id, "Station Unlocked",
            $"User {user} unlocked station",
            new Dictionary<string, object?> { { LogLabels.User, user } });
        await context.Publish(new StationUnlocked(station.Name));
    }

    /// <summary>
    /// Handles the <see cref="Common.Messages.Maps.MapSaved"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<Common.Messages.Maps.MapSaved> context)
    {
        var agvMap = context.Message.Map;
        ICollection<Guid> agvIds = await graphRepository.GetAgvIdsRelatedToNodesAsync(agvMap.Nodes);
        if (agvIds.Count == 0)
        {
            logger.LogDebug("No agvs assigned to map, do not update stations");
            return;
        }
        var agvGraph = await graphRepository.GetAgvGraphAsync(agvIds.First());
        await stationRepository.AddStations(agvGraph);
        logger.LogDebug("Stations updated");
    }

    /// <summary>
    /// Handles the <see cref="UpdateStationJamState"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateStationJamState> context)
    {
        var now = DateTime.UtcNow;
        var station = await stationRepository.GetStation(context.Message.CorrelationId);
        metricCollector.CollectJamStateMetrics(station.Id, station.PalletJamChangedDate, station.HasPalletJam, now, context.Message.HasPalletJam);
        station.HasPalletJam = context.Message.HasPalletJam;
        station.PalletJamChangedDate = now;
        await stationRepository.Update(station);
        logger.LogInformation("Jam state of station {StationName} updated to {JamState}", station.Name, station.HasPalletJam);
        await context.Publish(new JamStateChanged(station.Id, station.HasPalletJam));
    }
}