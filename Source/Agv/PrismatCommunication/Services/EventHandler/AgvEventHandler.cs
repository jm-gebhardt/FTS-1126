using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles agv related events.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler(IHostOrderRepository hostOrderRepository) :
    IConsumer<AllAgvsOffline>,
    IConsumer<AgvWentOnline>,
    IConsumer<EvacuateOrAssignQueueHead>,
    IConsumer<AgvLeftDoppelspielTrigger>
{
    /// <summary>
    /// Handles the <see cref="AllAgvsOffline"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AllAgvsOffline> context)
    {
        await context.Publish(new SetNotReady());
    }

    /// <summary>
    /// Handles the <see cref="AgvWentOnline"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvWentOnline> context)
    {
        await context.Publish(new SetReady());
    }

    /// <inheritdoc cref="AgvEventHandler"/>>
    public async Task Consume(ConsumeContext<EvacuateOrAssignQueueHead> context)
    {
        var hostOrder = await hostOrderRepository.GetCurrentHostOrderForAgv(context.Message.TriggeringAgv);
        if (hostOrder == null || hostOrder.CanRunDoppelspiel)
        {
            return;
        }
        await context.Publish(new ConsiderForDoppelspiel(hostOrder.CorrelationId));
    }

    /// <inheritdoc cref="AgvEventHandler"/>>
    public async Task Consume(ConsumeContext<AgvLeftDoppelspielTrigger> context)
    {
        await context.Publish(new QueueUnmatchedDoppelspielPicks(context.Message.TargetStationName));
    }
}