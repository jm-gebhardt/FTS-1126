using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[UsedImplicitly]
public class SuccessQueueActionGroupRequestHandler : IConsumer<QueueActionGroup>
{
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        await context.RespondAsync(new Success());
    }
}