using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.OrderTranslator.Services.EventHandler;

/// <summary>
/// Handles host order related events.
/// </summary>
[UsedImplicitly]
public class HostOrderEventHandler:
    IConsumer<HostOrderCreated>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<HostOrderCreated> context)
    {
        await context.Publish(new CreateActionGroup(context.Message.Hu, context.Message.OrderDetails, context.Message.AssignedAgv));
    }
}