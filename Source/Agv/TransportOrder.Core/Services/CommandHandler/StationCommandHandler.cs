using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
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
    ILogger<StationCommandHandler> logger) :
    IConsumer<LockStation>,
    IConsumer<UnlockStation>
{
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
        station.Lock(user, reason, lockDescription, lockOrderHandling);

        await stationRepository.Update(station);

        logger.LogStationInformation(LogCodes.StationLocked, id, "Station Locked", $"User {user} locked station because {reason}");
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
        station.Unlock();
        await stationRepository.Update(station);

        logger.LogStationInformation(LogCodes.StationUnlocked, id, "Station Unlocked", "Station was unlocked");
    }
}