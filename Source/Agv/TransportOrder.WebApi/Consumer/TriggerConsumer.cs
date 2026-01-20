using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Provides handlers for triggers.
/// </summary>
[UsedImplicitly]
public class TriggerConsumer(ITriggerPublisher triggerPublisher) :
    IConsumer<TriggerChanged>,
    IConsumer<TriggerMarkedForDeletion>,
    IConsumer<TriggerRestored>
{
    /// <inheritdoc cref="TriggerChanged"/>>
    public Task Consume(ConsumeContext<TriggerChanged> context)
    {
        return triggerPublisher.SendTriggerChangedAsync(context.Message.Trigger);
    }

    /// <inheritdoc cref="TriggerMarkedForDeletion"/>>
    public Task Consume(ConsumeContext<TriggerMarkedForDeletion> context)
    {
        return triggerPublisher.SendTriggerDeletedAsync(context.Message.TriggerId);
    }

    /// <inheritdoc cref="TriggerRestored"/>>
    public Task Consume(ConsumeContext<TriggerRestored> context)
    {
        return triggerPublisher.SendTriggerChangedAsync(context.Message.Trigger);
    }
}