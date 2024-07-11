using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides handlers for trigger-related events
/// </summary>
[UsedImplicitly]
public class TriggerEventHandler:
    IConsumer<TriggerConfigured>
{
    /// <summary>
    /// Handles the <see cref="TriggerConfigured"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerConfigured> context)
    {
        await context.Publish(new CreateTrigger(context.Message.Properties, context.Message.ConfigurationParameters));
    }
}