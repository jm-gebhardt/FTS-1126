using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[UsedImplicitly]
public class SuccessQueueActionGroupRequestHandler : IConsumer<QueueActionGroup>
{
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        await context.RespondAsync(new Success());
    }
}