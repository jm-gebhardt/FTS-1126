using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles <see cref="Domain.Models.Queue"/> related commands.
/// </summary>
[UsedImplicitly]
public class QueueCommandHandler(
    IQueueRepository queueRepository,
    ILogger<QueueCommandHandler> logger,
    IActionGroupRepository actionGroupRepository,
    ISemaphoreProvider<IQueueingSemaphore> queueSemaphoreProvider) :
    IConsumer<AddQueue>,
    IConsumer<GetQueueFillQuantities>,
    IConsumer<EvacuateQueue>,
    IConsumer<EvacuateQueueHead>
{
    /// <summary>
    /// Handles the <see cref="AddQueue"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddQueue> context)
    {
        (string name, ICollection<QueueNode> queueNodes) = context.Message;

        logger.LogInformation("Adding queue {QueueName}", name);
        var queue = new Queue(name);
        foreach (var node in queueNodes)
        {
            queue.AddNode(node.NodeId, node.Position);
            logger.LogInformation("Adding node {NodeId} to queue {QueueName} at position {QueuePosition}", name, node.NodeId, node.Position);
        }

        await queueRepository.AddQueue(queue);

        await context.RespondAsync(new GotQueue(queue));
    }

    /// <summary>
    /// Handles the <see cref="GetQueueFillQuantities"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetQueueFillQuantities> context)
    {
        ICollection<Queue> queues = await queueRepository.GetQueues();

        List<QueueFillQuantity> fillQuantities = queues.Select(q => new QueueFillQuantity(q.Name, q.AgvFillQuantity, q.Nodes.Count)).ToList();

        await context.RespondAsync(new GotQueueFillQuantities(fillQuantities));
    }

    /// <summary>
    /// Handles the <see cref="EvacuateQueue"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<EvacuateQueue> context)
    {
        (string queueName, string evacuationTarget, int waitingTime) = context.Message;
        await queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            logger.LogInformation("Evacuating queue {Queue} to node {Node}", queueName, evacuationTarget);
            var queue = await queueRepository.GetQueueByName(queueName);

            if (queue.AgvFillQuantity == 0)
            {
                logger.LogInformation("Not evacuating empty queue {Queue}", queue.Name);
                return;
            }

            List<Guid> agvs = queue.Agvs;
            if (!queue.AllAgvsEntered)
            {
                ICollection<Domain.Models.ActionGroup.ActionGroup> queueActionGroups = await actionGroupRepository.GetCurrentActionGroupsToTarget(queue.EntryNode!);
                List<Guid> queueingAgvs = queueActionGroups.Where(a => a.AssignedAgv.HasValue).Select(a => a.AssignedAgv!.Value).ToList();
                agvs.AddRange(queueingAgvs);
            }

            foreach (var agv in agvs.Distinct().ToList())
            {
                logger.LogInformation("Queuing move action to evacuation node {Node} for agv {Agv}", evacuationTarget, agv);
                var actionGroupId = Guid.NewGuid();
                await QueueNewActionGroup(context,
                    [
                        new AgvAction(Guid.NewGuid(),
                            ActionType.Move,
                            evacuationTarget,
                            actionGroupId)
                    ],
                    $"Queue Evacuation {ActionType.Move}",
                    agv,
                    actionGroupId);
                // Delay queueing the next manual move
                // to prevent queue internal moves, i.e.
                // moves towards the queue head even though all agvs 
                // are expected to escape eventually.
                await Task.Delay(TimeSpan.FromSeconds(waitingTime));
            }
        });
    }

    private async Task QueueNewActionGroup(IPublishEndpoint context, ICollection<AgvAction> actions, string orderId, Guid agvCorrelationId, Guid actionGroupId)
    {
        var actionGroup = new ActionGroup(
            actionGroupId,
            orderId,
            actions,
            assignedAgv: agvCorrelationId,
            isManual: true,
            creator: ActionGroupCreator.TransportOrder
        );
        await context.Publish(new QueueActionGroup(actionGroup, false));
    }

    /// <summary>
    /// Handles the <see cref="EvacuateQueueHead"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<EvacuateQueueHead> context)
    {
        (string queueName, string evacuationTarget) = context.Message;
        await queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            logger.LogInformation("Evacuating head of queue {Queue}", queueName);
            var queue = await queueRepository.GetQueueByName(queueName);

            if (queue.AgvFillQuantity == 0)
            {
                logger.LogInformation("Not evacuating empty queue {Queue}", queue.Name);
                return;
            }

            Guid? agv = queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv();
            if (agv == null)
            {
                logger.LogInformation("No agv at head of queue {Queue}", queue.Name);
                return;
            }
            ICollection<Domain.Models.ActionGroup.ActionGroup> queueActionGroups = await actionGroupRepository.GetCurrentActionGroupsToTarget(evacuationTarget);
            if (queueActionGroups.Any(q => q.AssignedAgv == agv))
            {
                logger.LogInformation("Evacuation moves for agv {AgvCorrelationId} already exists", agv );
                return;
            }

            logger.LogInformation("Queuing move action to evacuation node {Node} for agv {Agv}", evacuationTarget, agv);
            var actionGroupId = Guid.NewGuid();
            await QueueNewActionGroup(context,
                [
                    new AgvAction(Guid.NewGuid(),
                        ActionType.Move,
                        evacuationTarget,
                        actionGroupId)
                ],
                $"Queue Evacuation {ActionType.Move}",
                agv.Value,
                actionGroupId);
        });
    }
}