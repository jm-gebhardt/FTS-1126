using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Provides a handler for jam state related events.
/// </summary>
[UsedImplicitly]
public class JamStateEventHandler:
    IConsumer<JamStateReceived>
{
    /// <summary>
    /// Handles the <see cref="JamStateReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<JamStateReceived> context)
    {
        (string station, int state, var timeStamp) = context.Message;
        await context.Publish(new UpdateJamState(station, state, timeStamp));
    }
}