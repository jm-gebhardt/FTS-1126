using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Provides handlers for trigger changes.
/// </summary>
[UsedImplicitly]
public class TriggerChangedConsumer(ITriggerPublisher triggerPublisher) : 
    IConsumer<TriggerChanged>
{
    /// <summary>
    /// Handles the <see cref="TriggerChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerChanged> context)
    {
        await triggerPublisher.SendTriggerChangedAsync(context.Message.Trigger);
    }
}