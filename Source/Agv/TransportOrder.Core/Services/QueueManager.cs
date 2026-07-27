using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Events.Queue;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Core.Services;

/// <inheritdoc cref="IQueueManager"/>
public class QueueManager(
    IAgvDb agvDb,
    IQueueRepository queueRepository,
    IRouteService routeService,
    ILogger<QueueManager> logger,
    IGraphRepository graphRepository,
    IActionGroupRepository actionGroupRepository,
    IMapper mapper,
    IPublishEndpoint publishEndpoint,
    ISemaphoreProvider<IQueueingSemaphore> semaphoreProvider) : IQueueManager
{
    /// <inheritdoc cref="IQueueManager"/>
    public Task<Guid?> TryEnterQueue(Agv agv)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await GetQueue(agv);
            Guid? entryNode = queue?.TryEnterQueue();
            if (entryNode == null || queue == null)
            {
                logger.LogWarning("No queue available for entering agv {AgvCorrelationId} to queue {@Queue}", agv.CorrelationId, queue == null ? "null" : queue.Name);
                return null;
            }
            await UpdateQueueAsync(queue);
            logger.LogInformation("Queue {QueueName} selected for entering agv {AgvCorrelationId} for {@Queue}", queue.Name, agv.CorrelationId, queue);
            return entryNode;
        });
    }

    private async Task<bool> UpdateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        Task<bool> updateTask = queueRepository.UpdateQueueAsync(queue, cancellationToken);
        var publishTask = Task.CompletedTask;
        if (queue.QueueReservationChanged)
        {
            var queueMessages = mapper.Map<Common.Messages.Queue.Queue>(queue);
            publishTask = publishEndpoint.Publish(new QueueChanged(queueMessages), cancellationToken);
        }
        queue.ClearChangedFlag();
        await Task.WhenAll(updateTask, publishTask);

        return updateTask.Result;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<(Dictionary<Guid, Guid>? agvsToMove, Guid? queueEntry, bool swapped)> TrySwapAgv(Agv agvInside, Agv agvOutside)
    {
        var queueOfAgvInside = await queueRepository.GetQueueForAgv(agvInside.CorrelationId);
        if (queueOfAgvInside != null)
        {
            queueOfAgvInside.TrySwapAgv(agvInside.CorrelationId, out Dictionary<Guid, Guid> agvsToMove);
            logger.LogInformation("Queue {QueueName} selected for swapping in agv {AgvCorrelationId} with {AgvToSwapOut} for {@Queue}", queueOfAgvInside.Name, agvOutside.CorrelationId, agvInside.CorrelationId, queueOfAgvInside);
            await UpdateQueueAsync(queueOfAgvInside);
            return (agvsToMove, queueOfAgvInside.EntryNode!, true);
        }

        // Try getting different queue
        var queue = await GetQueue(agvOutside);
        if (queue == null)
        {
            logger.LogWarning("No queue available for swapping in agv {AgvCorrelationId} with {AgvToSwapOut}", agvOutside.CorrelationId, agvInside.CorrelationId);
            return (null, null, false);
        }
        Guid? entryNode = queue.TryEnterQueue();
        if (entryNode != null)
        {
            logger.LogInformation("Queue {QueueName} selected for queueing agv {AgvCorrelationId} for {@Queue}", queue.Name, agvOutside.CorrelationId, queue);
            await UpdateQueueAsync(queue);
            return (null, entryNode, true);
        }
        logger.LogWarning("No entry node available for queue {QueueName} for queueing agv {AgvCorrelationId} for {@Queue}", queue.Name, agvOutside.CorrelationId, queue);
        return (null, null, false);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<Guid?> TryEnqueueAgv(Guid agvCorrelationId, Guid entryNode)
    {
        var queue = await queueRepository.GetQueueByEntryNode(entryNode);
        if (queue == null)
        {
            return null;
        }
        if (queue.IsEnqueued(agvCorrelationId))
        {
            logger.LogWarning("Agv {AgvCorrelation} has already been enqueued to queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
            return null;
        }
        if (queue.TryEnqueue(agvCorrelationId, out Guid? target))
        {
            logger.LogInformation("Enqueuing agv {AgvCorrelationId} to node {TargetNode} of queue {QueueName} for {@Queue}", agvCorrelationId, target, queue.Name, queue);
            await UpdateQueueAsync(queue);
            return target.Value;
        }
        logger.LogWarning("Could not enqueue agv {AgvCorrelation} to queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
        return null;
    }

    private async Task<Queue?> GetQueue(Agv agv)
    {
        ICollection<Queue> queues = await queueRepository.GetQueuesWithCapacity();
        if (queues.Count == 0)
        {
            logger.LogWarning("No queues with capacity available for enqueuing agv {AgvCorrelation}", agv.CorrelationId);
            return null;
        }
        Guid[] queueEntryNodeIds = queues.Select(q => q.EntryNode!.Value).ToArray();
        try
        {
            var targetNode = await PickQueue(agv, queueEntryNodeIds);
            return queues.First(q => q.EntryNode == targetNode.Id);
        }
        catch
        {
            var queue = queues.First();
            return queue;
        }
    }

    private async Task<GraphNode> PickQueue(Agv agv, Guid[] queueNodeIds)
    {
        var agvData = await agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
        ICollection<GraphNode> queueNodes = await graphRepository.GetNodesByIdsAsync(queueNodeIds);
        var node = await routeService.GetNodeWithLowestCostsAsync(agv, queueNodes.ToList(), agvData);
        return node;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<Dictionary<Guid, Guid>?> TryDequeueAgv(Guid agvCorrelationId, Guid? agvLastNodeId)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        if (queue == null)
        {
            logger.LogDebug("Could not dequeue agv {AgvCorrelationId}, no queue found", agvCorrelationId);
            return null;
        }
        if (!queue.IsAgvAtQueueHead(agvCorrelationId))
        {
            logger.LogWarning("Could not dequeue agv from queue {QueueName}, agv {AgvCorrelationId} is not at head position, but at {AgvLastNodeId}, {AgvAtHead} is at assigned to head for {@Queue}", queue.Name, agvCorrelationId, agvLastNodeId, queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv(), queue);
            return null;
        }

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove);
        await UpdateQueueAsync(queue);
        logger.LogInformation("Dequeued agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
        return agvsToMove;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<(Dictionary<Guid, Guid>? agvsToMove, int? queueSize)> TryEscapeQueue(Guid agvCorrelationId, Guid? agvLastNodeId, Guid? queueId = null, Guid? queueNodeId = null)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        if (queue == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} not enqueued, checking for escaping from queue entry node", agvCorrelationId);
            return await TryEscapeFromQueueEntryNode(agvCorrelationId, agvLastNodeId, queueId, queueNodeId);
        }
        if (queueId != null && queue.Id != queueId)
        {
            logger.LogInformation("Agv {AgvCorrelationId} could not escape queue, agv is queueing on different queue {CurrentQueueId} than requested {RequestedQueueId}", agvCorrelationId, queue.Id, queueId);
            return (null, null);
        }
        Guid? node = queue.TryEscapeQueue(agvCorrelationId, out Dictionary<Guid, Guid> agvsToMove, queueNodeId);
        if (node == null)
        {
            logger.LogWarning("Could not escape agv {AgvCorrelationId} from queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
            return (null, null);
        }

        queue.ConsideredAgvs.Remove(agvCorrelationId);

        if (await UpdateQueueAsync(queue))
        {
            logger.LogInformation("Agv {AgvCorrelationId} escaped from queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
            return (agvsToMove, queue.AgvFillQuantity);
        }
        logger.LogWarning("Could not escape agv {AgvCorrelationId} from queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
        return (null, null);
    }

    private async Task<(Dictionary<Guid, Guid>? agvsToMove, int? queueSize)> TryEscapeFromQueueEntryNode(Guid agvCorrelationId, Guid? agvLastNodeId, Guid? queueId = null, Guid? queueNodeId = null)
    {
        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (!agv.IsQueueing)
        {
            logger.LogInformation("Agv {AgvCorrelationId} could not escape queue entry node, agv is not queueing ", agvCorrelationId);
            return (null, null);
        }

        if (agvLastNodeId == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} could not escape queue, no last node id {AgvLastNodeId} of agv ", agvCorrelationId, agvLastNodeId);
            return (null, null);
        }
        var queue = await queueRepository.GetQueueByEntryNode(agvLastNodeId.Value);
        if (queue == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} could not escape queue, no queue for last node id {AgvLastNodeId} of agv found", agvCorrelationId, agvLastNodeId);
            return (null, null);
        }
        if (queueId != null && queue.Id != queueId)
        {
            logger.LogInformation("Agv {AgvCorrelationId} could not escape queue entry node, agv is queueing on different queue {CurrentQueueId} than requested {RequestedQueueId}", agvCorrelationId, queue.Id, queueId);
            return (null, null);
        }
        if (queueNodeId != null && queueNodeId != queue.EntryNode)
        {
            logger.LogInformation("Agv {AgvCorrelationId} could not escape queue entry node, requested queue node {RequestedQueueNodeId} is not the entry node {EntryNodeId} of queue {QueueName}", agvCorrelationId, queueNodeId, queue.EntryNode, queue.Name);
            return (null, null);
        }
        queue.RemoveAgv(agvCorrelationId);
        queue.ConsideredAgvs.Remove(agvCorrelationId);
        await UpdateQueueAsync(queue);
        logger.LogInformation("Agv {AgvCorrelationId} escaped from queue {QueueName}", agvCorrelationId, queue.Name);
        return (new Dictionary<Guid, Guid>(), queue.AgvFillQuantity);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> TryReachQueueNode(Agv agv, Guid nodeId, CancellationToken cancellationToken = default)
    {
        Queue? queue = null;
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            queue = await queueRepository.GetQueueForAgv(agv.CorrelationId, cancellationToken);
            if (queue == null)
            {
                logger.LogDebug("Agv {AgvCorrelationId} could not reach queue head {Head}, no queue found", agv.CorrelationId, nodeId);
                return;
            }
            queue.ReachNode(nodeId, agv.CorrelationId);
            await UpdateQueueAsync(queue, cancellationToken);
            if (queue.IsAgvAtQueueHead(agv.CorrelationId))
            {
                logger.LogInformation("Agv {AgvCorrelationId} reached queue head {Head} for {@Queue}", agv.CorrelationId, nodeId, queue);
                return;
            }
            logger.LogInformation("Agv {AgvCorrelationId} could not reach queue head {Head}, agv does not occupy head {NodeId}, {AgvAtQueueHead} is assigned to head for {@Queue}", agv.CorrelationId, nodeId, queue.Head?.NodeId, queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv(), queue);
        }, cancellationToken);
        return queue != null && queue.IsAgvAtQueueHead(agv.CorrelationId);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<ICollection<Guid>> GetAgvsAtQueueHeads()
    {
        var agvs = new List<Guid>();
        foreach (var queue in await queueRepository.GetOccupiedQueues())
        {
            if (queue.Head?.OccupiedAgv != null)
            {
                agvs.Add(queue.Head!.OccupiedAgv!.Value);
            }
        }
        return agvs;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> IsOnAQueueNode(Guid agvCorrelationId)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        return queue?.Nodes.Any(n => n.OccupiedAgv != null && n.OccupiedAgv == agvCorrelationId) ?? false;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> RemoveAgvFromQueueEntry(Guid agvCorrelationId, Guid? targetNodeId)
    {
        bool removed = false;
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Queue? queue;
            if (targetNodeId != null && targetNodeId != Guid.Empty)
            {
                queue = await queueRepository.GetQueueByEntryNode(targetNodeId.Value);
            }
            else
            {
                ICollection<Queue> queues = await queueRepository.GetQueues();
                var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
                var queueNode = await PickQueue(agv, queues.Select(q => q.EntryNode).Where(e => e != Guid.Empty).Select(e => e!.Value).ToArray());
                queue = queues.FirstOrDefault(q => q.EntryNode == queueNode.Id);
            }
            if (queue == null)
            {
                logger.LogDebug("Agv {AgvCorrelationId} could not be removed from queue, no queue for target node {TargetNode} of agv found", agvCorrelationId, targetNodeId);
                return;
            }

            queue.RemoveAgv(agvCorrelationId);
            await UpdateQueueAsync(queue);
            removed = true;
            logger.LogInformation("Removing agv {AgvCorrelationId} from queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
        });
        return removed;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> RemoveAgvFromQueue(Guid agvCorrelationId, Guid? queueNodeId, Guid? queueId)
    {
        bool removed = false;
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Queue? queue = null;
            if (queueId != null)
            {
                queue = await queueRepository.GetQueueByIdIfExisting(queueId);
                if (queue == null)
                {
                    logger.LogInformation("Agv {AgvCorrelationId} could not be removed from queue, no queue found for requested queue id {RequestedQueueId}", agvCorrelationId, queueId);
                    return;
                }

                if (!queue.IsEnqueued(agvCorrelationId))
                {
                    logger.LogInformation("Agv {AgvCorrelationId} could not be removed from queue, agv is not enqueued in queue {QueueName}", agvCorrelationId, queue.Name);
                    return;
                }
            }

            if (queueNodeId != null)
            {
                queue ??= await queueRepository.GetQueueByQueueNodeId(queueNodeId.Value);
                var queueNode = queue?.Nodes.FirstOrDefault(n => n.NodeId == queueNodeId);
                if (queueNode == null)
                {
                    logger.LogInformation("Agv {AgvCorrelationId} could not be removed from queue, no queue node found for requested queue node id {RequestedQueueNodeId} in queue {QueueName}", agvCorrelationId, queueNodeId, queue?.Name);
                    return;
                }
            }

            queue ??= await queueRepository.GetQueueForAgv(agvCorrelationId);
            if (queue == null)
            {
                logger.LogInformation("Agv {AgvCorrelationId} could not be removed from queue, no queue for agv found", agvCorrelationId);
                return;
            }

            queue.RemoveAgv(agvCorrelationId);
            await UpdateQueueAsync(queue);
            removed = true;
            logger.LogInformation("Removing agv {AgvCorrelationId} from queue {QueueName} for {@Queue}", agvCorrelationId, queue.Name, queue);
        });
        return removed;
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetQueueInternalActionGroups(Agv agv, Guid? queueId = null)
    {
        if (!agv.IsQueueing)
        {
            logger.LogInformation("Agv {AgvCorrelationId} is not queueing, no internal action groups available", agv.CorrelationId);
            return [];
        }
        var queue = await queueRepository.GetQueueForAgv(agv.CorrelationId);

        if (queue == null)
        {
            logger.LogWarning("Agv {AgvCorrelationId} is queueing, but no queue found, no internal action groups available", agv.CorrelationId);
            return [];
        }

        if (queueId != null && queue.Id != queueId)
        {
            logger.LogInformation("Agv {AgvCorrelationId} is queueing on different queue {CurrentQueueId} than requested {RequestedQueueId}, no internal action groups available", agv.CorrelationId, queue.Id, queueId);
            return [];
        }

        ICollection<ActionGroup> queueInternalMoves = await actionGroupRepository.GetNotStartedActionGroupsToTargetsForAgv(agv.CorrelationId, queue.NonEntryNodes);
        logger.LogInformation("Agv {AgvCorrelationId} has {Count} internal action groups for queue {QueueName} for {@Queue}", agv.CorrelationId, queueInternalMoves.Count, queue.Name, queue);
        return queueInternalMoves.Select(ag => ag.CorrelationId).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetQueueEvacuationMoves(Guid agvCorrelationId, Guid? queueId = null)
    {
        List<ActionGroup> queueEvacuationMoves = (await actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId))
            .Where(a => string.Equals(a.OrderId, Queue.EvacuationOrderId)).ToList();

        if (queueId != null)
        {
            var queue = await queueRepository.GetQueueByIdIfExisting(queueId);
            if (queue == null)
            {
                logger.LogInformation("Agv {AgvCorrelationId} has no evacuation moves, no queue found for requested queue id {RequestedQueueId}", agvCorrelationId, queueId);
                return [];
            }
            queueEvacuationMoves = queueEvacuationMoves
                .Where(a => a.Actions.Count == 1 &&
                            a.Actions.Single().ActionType == ActionType.Move &&
                            a.Actions.Single().Target.Node != null &&
                            queue.Nodes.Any(qn => qn.NodeId == a.Actions.Single().Nodes.FirstOrDefault()?.Id))
                .ToList();
            logger.LogInformation("Agv {AgvCorrelationId} has {Count} evacuation moves for queue {QueueName}", agvCorrelationId, queueEvacuationMoves.Count, queue.Name);
        }

        return queueEvacuationMoves.Select(a => a.CorrelationId).ToList();
    }

    /// <inheritdoc />
    public Task<ICollection<Queue>> GetQueuesOnRoute(ICollection<Guid> nodes)
    {
        return queueRepository.GetQueuesOnRoute(nodes);
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvInQueue(Guid agvCorrelationId)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        // AGV is reserved for or occupies queue node
        if (queue != null && queue.Agvs.Contains(agvCorrelationId))
        {
            return true;
        }
        // AGV is marked as queueing (independent of queue moves, as they may have been aborted)
        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (agv.IsQueueing)
        {
            return true;
        }
        // AGV has running or created queue moves
        IReadOnlyList<ActionGroup> currentActionGroups = await actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId);
        ICollection<Queue> queues = await queueRepository.GetQueues();
        IEnumerable<Guid> queueNodes = queues.SelectMany(q => q.Nodes.Where(n => n.Node != null).Select(n => n.Node!.Id)).ToList();
        return currentActionGroups.Where(ag => ag.Actions.Count == 1 && !ag.IsManual)
            .Any(a => a.Actions.Single().ActionType == ActionType.Move && a.Actions.Single().Target.Node != null && queueNodes.Contains(a.Actions.Single().Target.NodeId!.Value));
    }
    
    /// <inheritdoc />
    public async Task AdjustAgvFillQuantity(Guid queueId)
    {
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await queueRepository.GetQueueByIdIfExisting(queueId);
            if (queue == null)
            {
                logger.LogWarning("Could not adjust AGV fill quantity, no queue found for queue id {QueueId}", queueId);
                return;
            }
            int oldFillQuantity = queue.AgvFillQuantity;
            if (queue.EntryNode != null)
            {
                int agvsMovingToQueue = (await actionGroupRepository.GetCurrentActionGroupsToTarget(queue.EntryNode.Value)).Count;
                queue.AdjustAgvFillQuantity(agvsMovingToQueue);
            }

            await UpdateQueueAsync(queue);
            logger.LogInformation("Adjusted AGV fill quantity for queue {QueueName} from {OldFillQuantity} to {NewFillQuantity}", queue.Name, oldFillQuantity, queue.AgvFillQuantity);
        });
    }
}