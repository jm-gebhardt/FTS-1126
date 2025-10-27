using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Handles queue related events.
/// </summary>
[UsedImplicitly]
public class QueueEventHandler(IActionGroupRepository actionGroupRepository, IQueueRepository queueRepository) : 
    IConsumer<DropActionCompleted>,
    IConsumer<PickActionCompleted>,
    IConsumer<AgvLastNodeIdChanged>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DropActionCompleted> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);

        if (actionGroup.AssignedAgv == null)
        {
            return;
        }

        await context.Publish(new RemoveAgvFromQueueConsideration(actionGroup.AssignedAgv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);

        if (actionGroup.AssignedAgv == null)
        {
            return;
        }

        await context.Publish(new RemoveAgvFromQueueConsideration(actionGroup.AssignedAgv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvLastNodeIdChanged> context)
    {
        (var agvCorrelationId, _, Guid? newNodeId ) = context.Message;
        if (newNodeId == null)
        {
            return;
        }
        var queue = await queueRepository.GetQueueByEntryNode(newNodeId.Value);

        if (queue == null)
        {
            return;
        }

        await context.Publish(new AddAgvToQueueConsideration(agvCorrelationId, queue.Id));
    }
}