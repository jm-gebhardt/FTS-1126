using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles station related events.
/// </summary>
[UsedImplicitly]
public class StationEventHandler(KaeserGraphConfiguration configuration, ILogger<StationEventHandler> logger) :
    IConsumer<StationLocked>,
    IConsumer<StationUnlocked>
{
    /// <summary>
    /// Handles the <see cref="StationLocked"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<StationLocked> context)
    {
        string lockedStation = context.Message.StationNodeId;
        if (lockedStation == configuration.StationMapping[configuration.PrismatAklPickTarget])
        {
            string newTarget = configuration.SelectNextShortLoopTarget();
            logger.LogInformation("Short loop target {StationName} was locked, using {Target} from now on", lockedStation, newTarget);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="StationLocked"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<StationUnlocked> context)
    {
        string unlockedStation = context.Message.StationNodeId;
        if (unlockedStation == configuration.GetOriginalShortLoopTarget())
        {
            string newTarget = configuration.ResetShortLoopTarget();
            logger.LogInformation("Original short loop target {StationName} was unlocked, using {Target} from now on", unlockedStation, newTarget);
        }
        return Task.CompletedTask;
    }
}