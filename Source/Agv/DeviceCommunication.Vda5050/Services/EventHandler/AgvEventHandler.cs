using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles <see cref="AgvAddedOrUpdated"/>.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler : IConsumer<AgvDiscovered>, IConsumer<AgvTypeChanged>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvDiscovered> context)
    {
        await context.Publish(new AddAgv(context.Message.CorrelationId, context.Message.Agv, context.Message.AgvTypeId, context.Message.HasValidConfiguration));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvTypeChanged> context)
    {
        await context.Publish(new UpdateAgvType(context.Message.CorrelationId, context.Message.NewTypeId, context.Message.Agv, context.Message.HasValidConfiguration, context.Message.NeedsConfirmation));
    }
}