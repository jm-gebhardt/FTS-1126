using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles station related events.
/// </summary>
[UsedImplicitly]
public class StationEventHandler(KaeserGraphConfiguration configuration, IShortLoopWeTargetStationRepository targetStationRepository,  ILogger<StationEventHandler> logger) :
    IConsumer<StationLocked>,
    IConsumer<StationUnlocked>,
    IConsumer<AgvLeftStation>
{
    /// <summary>
    /// Handles the <see cref="StationLocked"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StationLocked> context)
    {
        string lockedStation = context.Message.StationNodeId;
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
    /// Handles the <see cref="StationLocked"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        var shortLoopTarget = await targetStationRepository.GetTargetStation();
        string unlockedStation = context.Message.StationNodeId;
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
        await context.Publish(new TryQueueDelayedOrder(context.Message.Source, context.Message.Target));
    }
}