using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Publishes an add-or-update command when an AGV type has been created.
/// </summary>
[UsedImplicitly]
public class AgvTypeEventHandler : IConsumer<AgvTypeCreated>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvTypeCreated> context)
    {
        await context.Publish(new AddOrUpdateAgvType(context.Message.CorrelationId, context.Message.AgvType));
    }
}