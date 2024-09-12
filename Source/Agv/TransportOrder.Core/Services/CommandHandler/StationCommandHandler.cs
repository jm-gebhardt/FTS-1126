using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for node- and station-related commands.
/// </summary>
/// <inheritdoc cref="StationCommandHandler"/>>
[UsedImplicitly]
public class StationCommandHandler(INodeStateRepository nodeStateRepository, IStationRepository stationRepository, ILogger<StationCommandHandler> logger) : 
    IConsumer<FreeNode>,
    IConsumer<LockStation>, 
    IConsumer<UnlockStation>
{
    /// <summary>
    /// Handles the <see cref="FreeNode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        string nodeId = context.Message.NodeId;
        var station = await stationRepository.GetStation(nodeId);
        if (!await nodeStateRepository.FreeNodeAsync(nodeId))
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked.", nodeId);
        }

        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        await context.Publish(new NodeStateChanged(nodeState, station.Id));
    }

    /// <summary>
    /// Handles the <see cref="LockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<LockStation> context)
    {
        (var id, string? user, string reason, string lockDescription, var lockOrderHandling) = context.Message;

        var station = await stationRepository.GetStation(id);

        bool lockResult = await nodeStateRepository.LockNodeAsync(station.Name);

        if (lockResult)
        {
            var nodeState = await nodeStateRepository.GetNodeStateAsync(station.Name);

            if (nodeState != null)
            {
                await context.Publish(new NodeStateChanged(nodeState, id));
            }
        }

        await stationRepository.LockStation(id, user, reason, lockDescription, lockOrderHandling);

        logger.LogStationInformation(LogCodes.StationLocked, id, "Station Locked", $"User {user} locked station because {reason}");

        await context.RespondAsync(new LockedUnlockedStation(lockResult));
    }

    /// <summary>
    /// Handles the <see cref="UnlockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnlockStation> context)
    {
        var id = context.Message.CorrelationId;

        var station = await stationRepository.GetStation(id);

        bool unlockResult = await nodeStateRepository.UnlockNodeAsync(station.Name);

        if (unlockResult)
        {
            var nodeState = await nodeStateRepository.GetNodeStateAsync(station.Name);

            if (nodeState != null)
            {
                await context.Publish(new NodeStateChanged(nodeState, id));
            }
        }

        await stationRepository.UnlockStation(id);

        logger.LogStationInformation(LogCodes.StationUnlocked, id, "Station Unlocked", "Station was unlocked");

        await context.RespondAsync(new LockedUnlockedStation(unlockResult));
    }
}