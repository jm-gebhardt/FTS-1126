using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
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
/// <remarks>
/// Provides handlers for station-related commands.
/// </remarks>
/// <inheritdoc cref="StationCommandHandler"/>>
[UsedImplicitly]
public class StationCommandHandler(INodeRepository nodeRepository,
    IStationRepository stationRepository,
    IGraphRepository graphRepository,
    ILogger<StationCommandHandler> logger,
    ILoggingService loggingService) :
    IConsumer<LockStation>,
    IConsumer<UnlockStation>,
    IConsumer<Common.Messages.Maps.MapSaved>,
    IConsumer<UpdateStationJamState>,
    IConsumer<GetLockedStations>,
    IConsumer<StationConfigurationReceived>,
    IConsumer<UpdateStationAdditionalInfo>
{
    /// <summary>
    /// Handles the <see cref="LockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<LockStation> context)
    {
        (var id, string user, string reason, string lockDescription, var lockOrderHandling) = context.Message;

        var station = await stationRepository.GetStation(id);
        var node = await nodeRepository.GetNodeAsync(id);
        bool lockResult = node.Lock();
        if (!lockResult)
        {
            logger.LogError("Lock of station {StationName} failed ", station.Name);
            return;
        }

        station.NodeLock = node.Lock;
        await nodeRepository.UpdateLock(node);

        station.Lock(user, reason, lockDescription, lockOrderHandling);

        await stationRepository.Update(station);

        await loggingService.LogStation(logger, LogLevel.Information, LogCodes.StationLocked, id, "Station Locked",
            $"User {user} locked station because {reason}",
            new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Reason, reason } });
        await context.Publish(new StationLocked(station.Name));

        if (node.Lock != null)
        {
            await context.Publish(new NodeStateChanged(node.Lock, node.Name, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
        }
    }

    /// <summary>
    /// Handles the <see cref="UnlockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnlockStation> context)
    {
        (var id, string user) = context.Message;

        var station = await stationRepository.GetStation(id);
        var node = await nodeRepository.GetNodeAsync(id);
        bool unlockResult = node.Unlock();

        if (!unlockResult)
        {
            logger.LogError("Unlock of station {StationName} failed ", station.Name);
            return;
        }

        await nodeRepository.UpdateLock(node);

        station.Unlock();
        station.NodeLock = node.Lock;
        await stationRepository.Update(station);

        if (node.Lock != null)
        {
            await context.Publish(new NodeStateChanged(node.Lock, node.Name, station.Id, user, station.LockReason, station.LockDescription));
        }

        await loggingService.LogStation(logger, LogLevel.Information, LogCodes.StationUnlocked, id, "Station Unlocked",
            $"User {user} unlocked station",
            new Dictionary<string, object?> { { LogLabels.User, user } });

        await context.Publish(new StationUnlocked(node));
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
        //publish first to transfer old and new state
        await context.Publish(new JamStateChanged(station.Id, context.Message.HasPalletJam, station.PalletJamChangedDate, station.HasPalletJam, now));
        station.HasPalletJam = context.Message.HasPalletJam;
        station.PalletJamChangedDate = now;
        await stationRepository.Update(station);
        logger.LogInformation("Jam state of station {StationName} updated to {JamState}", station.Name, station.HasPalletJam);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetLockedStations> context)
    {
        ICollection<Station> stations = await stationRepository.GetStations();

        await context.RespondAsync(new GotLockedStations(stations.Where(s => s.IsLocked).Select(s => s.Name).ToList()));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StationConfigurationReceived> context)
    {
        var (stationId, stationConfiguration) = context.Message;

        var station = await stationRepository.GetStation(stationId);
        station.Configure(stationConfiguration);
        await stationRepository.Update(station);

        logger.LogInformation("Configured station {Station}", station.Name);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateStationAdditionalInfo> context)
    {
        (var stationId, Dictionary<string, object?> additionalInformation) = context.Message;
        var station = await stationRepository.GetStation(stationId);
        station.UpdateAdditionalInfo(additionalInformation);
        await stationRepository.Update(station);

        logger.LogInformation("Updated additional information of station {Station}", station.Name);
    }
}