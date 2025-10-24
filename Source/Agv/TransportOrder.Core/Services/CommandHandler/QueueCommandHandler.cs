using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using Queue = Gebhardt.Acc.TransportOrder.Domain.Models.Queue.Queue;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles <see cref="Domain.Models.Queue"/> related commands.
/// </summary>
[UsedImplicitly]
public class QueueCommandHandler(
    IQueueRepository queueRepository,
    ILogger<QueueCommandHandler> logger,
    IActionGroupRepository actionGroupRepository,
    ISemaphoreProvider<IQueueingSemaphore> queueSemaphoreProvider,
    IAgvDb agvDb,
    IBus bus
) :
    IConsumer<AddQueue>,
    IConsumer<GetQueueFillQuantities>,
    IConsumer<EvacuateQueue>,
    IConsumer<EvacuateQueueHead>,
    IConsumer<EvacuateOrAssignQueueHead>
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

        List<QueueFillQuantity> fillQuantities = queues.Select(q => new QueueFillQuantity(q.Name, q.AgvFillQuantity, q.Nodes.Count, q.Nodes.Count(n => n.GetCurrentAgv() == null))).ToList();

        await context.RespondAsync(new GotQueueFillQuantities(fillQuantities));
    }

    /// <summary>
    /// Handles the <see cref="EvacuateQueue"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<EvacuateQueue> context)
    {
        (string queueName, var evacuationTargetName, int waitingTime) = context.Message;
        var evacuationTarget = await agvDb.Graphs.TryGetNodeByIdAsync(evacuationTargetName);
        
        await queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            logger.LogInformation("Evacuating queue {Queue} to node {Node}", queueName, evacuationTargetName);
            var queue = await queueRepository.GetQueueByName(queueName);

            if (queue.AgvFillQuantity == 0)
            {
                logger.LogInformation("Not evacuating empty queue {Queue}", queue.Name);
                return;
            }

            List<Guid> agvs = queue.Agvs;
            if (!queue.AllAgvsEntered)
            {
                ICollection<Domain.Models.ActionGroup.ActionGroup> queueActionGroups = await actionGroupRepository.GetCurrentActionGroupsToTarget(queue.EntryNode!.Value);
                List<Guid> queueingAgvs = queueActionGroups.Where(a => a.AssignedAgv.HasValue).Select(a => a.AssignedAgv!.Value).ToList();
                agvs.AddRange(queueingAgvs);
            }

            foreach (var agv in agvs.Distinct().ToList())
            {
                logger.LogInformation("Queuing move action to evacuation node {Node} for agv {Agv}", evacuationTargetName, agv);
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
            isManual: true, // ensure agv escapes the queue
            creator: ActionGroupCreator.TransportOrder,
            doNotPause: true
        );
        await context.Publish(new QueueActionGroup(actionGroup, false));
    }

    /// <summary>
    /// Handles the <see cref="EvacuateQueueHead"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<EvacuateQueueHead> context)
    {
        (string queueName, var evacuationTargetId) = context.Message;
        var evacuationTarget = await agvDb.Graphs.GetNodeAsync(evacuationTargetId);
        await queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            logger.LogInformation("Evacuating head of queue {Queue}", queueName);
            var queue = await queueRepository.GetQueueByName(queueName);

            if (queue.AgvFillQuantity == 0)
            {
                logger.LogInformation("Not evacuating empty queue {Queue}", queue.Name);
                return;
            }
            var queueHeadNode = queue.Nodes.MinBy(n => n.Position);
            Guid? agv = queueHeadNode?.GetCurrentAgv();
            if (agv == null)
            {
                logger.LogInformation("No agv at head of queue {Queue}", queue.Name);
                return;
            }
            ICollection<Domain.Models.ActionGroup.ActionGroup> queueActionGroups = await actionGroupRepository.GetCurrentActionGroupsToTarget(evacuationTargetId);
            if (queueActionGroups.Any(q => q.AssignedAgv == agv))
            {
                logger.LogInformation("Evacuation moves for agv {AgvCorrelationId} already exists", agv);
                return;
            }
            List<Guid> queueTargets = queue.NonEntryNodes.Except([queueHeadNode?.NodeId ?? Guid.Empty]).ToList();
            ICollection<Domain.Models.ActionGroup.ActionGroup> queueInternalMoves = await actionGroupRepository.GetNotStartedActionGroupsToTargetsForAgv(agv.Value, queueTargets);
            await Task.WhenAll(queueInternalMoves.Select(ag => context.Publish(new CancelOrAbortActionGroup(ag.CorrelationId))));

            logger.LogInformation("Queuing move action to evacuation node {Node} for agv {Agv}", evacuationTargetId, agv);
            var actionGroupId = Guid.NewGuid();
            await QueueNewActionGroup(context,
                [
                    new AgvAction(Guid.NewGuid(),
                        ActionType.Move,
                        evacuationTarget,
                        actionGroupId)
                ],
                Queue.EvacuationOrderId,
                agv.Value,
                actionGroupId);
        });
    }

    /// <inheritdoc cref="QueueCommandHandler"/>>
    public async Task Consume(ConsumeContext<EvacuateOrAssignQueueHead> context)
    {
        (string queueName, var evacuationTargetId, var triggeringAgv, string stationName) = context.Message;

        var evacuationTarget = await agvDb.Graphs.GetNodeAsync(evacuationTargetId);
        await queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await queueRepository.GetQueueByName(queueName);

            if (queue.AgvFillQuantity == 0)
            {
                logger.LogInformation("Not evacuating empty queue {Queue}", queue.Name);
                return;
            }
            var queueHeadNode = queue.Nodes.MinBy(n => n.Position);
            Guid? agv = queueHeadNode?.GetCurrentAgv();
            if (agv == null)
            {
                logger.LogInformation("No agv at head of queue {Queue}", queue.Name);
                return;
            }

            // Agv is already about to get evacuated, assignment to order may be too late
            ICollection<Domain.Models.ActionGroup.ActionGroup> queueActionGroups = await actionGroupRepository.GetCurrentActionGroupsToTarget(evacuationTargetId);
            if (queueActionGroups.Any(q => q.AssignedAgv == agv))
            {
                logger.LogInformation("Evacuation moves for agv {AgvCorrelationId} already exists, not evacuating or assigning", agv);
                return;
            }

            if (queueHeadNode?.OccupiedAgv == null) // if the head is only reserved, not occupied
            {  
                // If the agv reserved for the queue head has already been assigned to a pick at station, do not evacuate or try to assign to another one,
                // it will run the other one, once reaching the queue head
                ICollection<Domain.Models.ActionGroup.ActionGroup> picksAtStation = await actionGroupRepository.GetCurrentActionGroupsToTarget(stationName.ToGuid());
                if (picksAtStation.Any(p => p.AssignedAgv == agv))
                {
                    logger.LogInformation("Agv {AgvCorrelationId} at queue head already assigned to pick at {Station}, not evacuating or assigning", agv, stationName);
                    return;
                }
            }

            if (await TryAssignHeadToOrder(context, triggeringAgv, agv, stationName))
            {
                return;
            }

            logger.LogInformation("Evacuating head of queue {Queue}", queueName);
            
            List<Guid> queueTargets = queue.NonEntryNodes.Except([queueHeadNode?.NodeId ?? Guid.Empty]).ToList();
            ICollection<Domain.Models.ActionGroup.ActionGroup> queueInternalMoves = await actionGroupRepository.GetNotStartedActionGroupsToTargetsForAgv(agv.Value, queueTargets);
            await Task.WhenAll(queueInternalMoves.Select(ag => context.Publish(new CancelOrAbortActionGroup(ag.CorrelationId))));

            logger.LogInformation("Queuing move action to evacuation node {Node} for agv {Agv}", evacuationTargetId, agv);
            var actionGroupId = Guid.NewGuid();
            await QueueNewActionGroup(context,
                [
                    new AgvAction(Guid.NewGuid(),
                        ActionType.Move,
                        evacuationTarget,
                        actionGroupId)
                ],
                Queue.EvacuationOrderId,
                agv.Value,
                actionGroupId);
        });
    }

    private async Task<bool> TryAssignHeadToOrder(ConsumeContext<EvacuateOrAssignQueueHead> context, Guid triggeringAgv, [DisallowNull] Guid? agv, string stationName)
    {
        var targetStation = await agvDb.Graphs.GetNodeAsync(stationName.ToGuid());
        bool headRunsOrder = false;
        try
        {
            IRequestClient<TryAssignDelayedOrderToAgv>? requestClient = context.CreateRequestClient<TryAssignDelayedOrderToAgv>(bus);
            Response<AssignedDelayedOrder> response = await requestClient.GetResponse<AssignedDelayedOrder>(new TryAssignDelayedOrderToAgv(triggeringAgv, agv.GetValueOrDefault(), targetStation));
            headRunsOrder = response.Message.CorrelationId != null;
            if (!headRunsOrder)
            {
                logger.LogInformation("Queue head {AgvCorrelationId} will run order {ActionGroupCorrelationId}, not evacuating it.", agv, response.Message.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Did not get response to {RequestName} request: {Exception}", nameof(TryAssignDelayedOrderToAgv), ex);
        }
        return headRunsOrder;
    }
}