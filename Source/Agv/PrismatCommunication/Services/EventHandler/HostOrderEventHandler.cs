using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for host order related events.
/// </summary>
[UsedImplicitly]
public class HostOrderEventHandler(IHostOrderRepository hostOrderRepository) : 
    IConsumer<DecisionPointPassed>,
    IConsumer<CargoTriggerPointPassed>
{
    /// <summary>
    /// Handles the <see cref="DecisionPointPassed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DecisionPointPassed> context)
    {
        var order = await hostOrderRepository.GetNextIncompleteOrderWithUnknownPickTarget();
        if (order != null)
        {
            await context.Publish(new QueuePickDropActionGroupForAgv(order.CorrelationId, context.Message.AssignedAgv, context.Message.TargetNodeId));
        }
    }

    /// <summary>
    /// Handles the <see cref="CargoTriggerPointPassed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<CargoTriggerPointPassed> context)
    {
        var order = await hostOrderRepository.GetNextIncompleteOrderWithUnknownDropTarget();
        if (order != null)
        {
            await context.Publish(new QueueActionGroupWithUnknownDropTargetForAgv(order.CorrelationId, context.Message.Agv));
        }
    }
}