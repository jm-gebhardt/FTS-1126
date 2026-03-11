using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles agv related events.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler:
    IConsumer<AllAgvsOffline>,
    IConsumer<AgvWentOnline>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AllAgvsOffline> context)
    {
        await context.Publish(new SetNotReady());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvWentOnline> context)
    {
        await context.Publish(new SetReady());
    }
}