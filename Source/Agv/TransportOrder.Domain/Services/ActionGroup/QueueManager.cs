using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IQueueManager"/>
public class QueueManager(
    IAgvDb agvDb,
    IQueueRepository queueRepository,
    IRouteService routeService,
    ILogger<QueueManager> logger,
    IGraphRepository graphRepository,
    IActionGroupRepository actionGroupRepository,
    ISemaphoreProvider<IQueueingSemaphore> semaphoreProvider) :  IQueueManager
{
    /// <inheritdoc cref="IQueueManager"/>
    public async Task<string?> TryEnterQueue(Agv agv)
    {
        return await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await GetQueue(agv);
            string? entryNode = queue?.TryEnterQueue();
            if (entryNode == null || queue == null)
            {
                logger.LogWarning("No queue available for entering agv {AgvCorrelationId} to queue", agv.CorrelationId);
                return null;
            }
            await queueRepository.UpdateQueueAsync(queue);
            logger.LogInformation("Queue {QueueName} selected for entering agv {AgvCorrelationId}", queue.Name, agv.CorrelationId);
            return entryNode;
        });
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<(Dictionary<Guid, string>? agvsToMove, string? queueEntry, bool swapped)> TrySwapAgv(Agv agvInside, Agv agvOutside)
    {
        var queueOfAgvInside = await queueRepository.GetQueueForAgv(agvInside.CorrelationId);
        if (queueOfAgvInside != null)
        {
            queueOfAgvInside.TrySwapAgv(agvInside.CorrelationId, out Dictionary<Guid, string> agvsToMove);
            logger.LogInformation("Queue {QueueName} selected for swapping in agv {AgvCorrelationId} with {AgvToSwapOut}", queueOfAgvInside.Name, agvOutside.CorrelationId, agvInside.CorrelationId);
            await queueRepository.UpdateQueueAsync(queueOfAgvInside);
            return (agvsToMove, queueOfAgvInside.EntryNode!, true);
        }

        // Try getting different queue
        var queue = await GetQueue(agvOutside);
        if (queue == null)
        {
            logger.LogWarning("No queue available for swapping in agv {AgvCorrelationId} with {AgvToSwapOut}", agvOutside.CorrelationId, agvInside.CorrelationId);
            return (null, null, false);
        }
        string? entryNode = queue.TryEnterQueue();
        if (entryNode != null)
        {
            logger.LogInformation("Queue {QueueName} selected for queueing agv {AgvCorrelationId} ", queue.Name, agvOutside.CorrelationId);
            await queueRepository.UpdateQueueAsync(queue);
            return (null, entryNode, true);
        }
        logger.LogWarning("No entry node available for queue {Queue} for queueing agv {AgvCorrelationId}", queue.Id, agvOutside.CorrelationId);
        return (null, null, false);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<string?> TryEnqueueAgv(Guid agvCorrelationId, string entryNode)
    {
        var queue = await queueRepository.GetQueueByEntryNode(entryNode);
        if (queue == null)
        {
            return null;
        }
        if (queue.IsEnqueued(agvCorrelationId))
        {
            logger.LogWarning("Agv {AgvCorrelation} has already been enqueued to queue {QueueName}", agvCorrelationId, queue.Name);
            return null;
        }
        if (queue.TryEnqueue(agvCorrelationId, out string? target))
        {
            logger.LogInformation("Enqueuing agv {AgvCorrelationId} to node {TargetNode} of queue {QueueName}", agvCorrelationId, target, queue.Name);
            await queueRepository.UpdateQueueAsync(queue);
            return target;
        }
        logger.LogWarning("Could not enqueue agv {AgvCorrelation} to queue {QueueName}", agvCorrelationId, queue.Name);
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
        string[] queueEntryNodeIds = queues.Select(q => q.EntryNode!).ToArray();
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

    private async Task<GraphNode> PickQueue(Agv agv, string[] queueNodeIds)
    {
        var agvData = await agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
        ICollection<GraphNode> queueNodes = await graphRepository.GetNodesByIdsAsync(queueNodeIds);
        var node = await routeService.GetNodeWithLowestCostsAsync(agv, queueNodes.ToList(), agvData);
        return node;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<Dictionary<Guid, string>?> TryDequeueAgv(Guid agvCorrelationId, string? agvLastNodeId)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        if (queue == null)
        {
            logger.LogDebug("Could not dequeue agv {AgvCorrelationId}, no queue found", agvCorrelationId);
            return null;
        }
        if (!queue.IsAgvAtQueueHead(agvCorrelationId))
        {
            logger.LogWarning("Could not dequeue agv from queue {QueueName}, agv {AgvCorrelationId} is not at head position, but at {AgvLastNodeId}, {AgvAtHead} is at assigned to head", queue.Name, agvCorrelationId, agvLastNodeId, queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv());
            return null;
        }

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove);
        await queueRepository.UpdateQueueAsync(queue);
        logger.LogInformation("Dequeued agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
        return agvsToMove;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<(Dictionary<Guid, string>? agvsToMove, int? queueSize)> TryEscapeQueue(Guid agvCorrelationId, string? agvLastNodeId)
    {
        var queue = await queueRepository.GetQueueForAgv(agvCorrelationId);
        if (queue == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} not enqueued, checking for escaping from queue entry node", agvCorrelationId);
            return await TryEscapeFromQueueEntryNode(agvCorrelationId, agvLastNodeId);
        }
        string? node = queue.TryEscapeQueue(agvCorrelationId, out Dictionary<Guid, string> agvsToMove);
        if (node == null)
        {
            logger.LogWarning("Could not escape agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
            return (null, null);
        }
        await queueRepository.UpdateQueueAsync(queue);
        logger.LogInformation("Agv {AgvCorrelationId} escaped from queue {QueueName}", agvCorrelationId, queue.Name);
        return (agvsToMove, queue.AgvFillQuantity);
    }

    private async Task<(Dictionary<Guid, string>? agvsToMove, int? queueSize)> TryEscapeFromQueueEntryNode(Guid agvCorrelationId, string? agvLastNodeId)
    {
        if (agvLastNodeId == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} could not escape queue, no last node id {AgvLastNodeId} of agv ", agvCorrelationId, agvLastNodeId);
            return (null, null);
        }
        var queue = await queueRepository.GetQueueByEntryNode(agvLastNodeId);
        if (queue == null)
        {
            logger.LogDebug("Agv {AgvCorrelationId} could not escape queue, no queue for last node id {AgvLastNodeId} of agv found", agvCorrelationId, agvLastNodeId);
            return (null, null);
        }
        queue.RemoveAgv(agvCorrelationId);
        await queueRepository.UpdateQueueAsync(queue);
        logger.LogInformation("Agv {AgvCorrelationId} escaped from queue {QueueName}", agvCorrelationId, queue.Name);
        return (new Dictionary<Guid, string>(), queue.AgvFillQuantity);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> TryReachQueueNode(Agv agv, string nodeId)
    {
        Queue? queue = null;
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            queue = await queueRepository.GetQueueForAgv(agv.CorrelationId);
            if (queue == null)
            {
                logger.LogDebug("Agv {AgvCorrelationId} could not reach queue head {Head}, no queue found", agv.CorrelationId, nodeId);
                return;
            }
            queue.ReachNode(nodeId, agv.CorrelationId);
            await queueRepository.UpdateQueueAsync(queue);
            if (queue.IsAgvAtQueueHead(agv.CorrelationId))
            {
                logger.LogInformation("Agv {AgvCorrelationId} reached queue head {Head}", agv.CorrelationId, nodeId);
                return;
            }
            logger.LogDebug("Agv {AgvCorrelationId} could not reach queue head {Head}, agv does not occupy head {NodeId}, {AgvAtQueueHead} is assigned to head", agv.CorrelationId, nodeId, queue.Head?.NodeId, queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv());
        });
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
    public async Task<bool> RemoveAgvFromQueueEntry(Guid agvCorrelationId, string? targetNode)
    {
        bool removed = false;
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Queue? queue;
            if (targetNode != null)
            {
                queue = await queueRepository.GetQueueByEntryNode(targetNode);
            }
            else
            {
                ICollection<Queue> queues = await queueRepository.GetQueues();
                var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
                var queueNode = await PickQueue(agv, queues.Select(q => q.EntryNode).Where(e => e != null).ToArray()!);
                queue = queues.FirstOrDefault(q => q.EntryNode == queueNode.Id);
            }
            if (queue == null)
            {
                logger.LogDebug("Agv {AgvCorrelationId} could not be removed from queue, no queue for target node {TargetNode} of agv found", agvCorrelationId, targetNode);
                return;
            }

            queue.RemoveAgv(agvCorrelationId);
                await queueRepository.UpdateQueueAsync(queue);
                removed = true;
                logger.LogInformation("Removing agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
        });
        return removed;
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetQueueInternalActionGroups(Agv agv)
    {
        if (!agv.IsQueueing)
        {
            return [];
        }
        var queue = await queueRepository.GetQueueForAgv(agv.CorrelationId);

        if (queue == null)
        {
            return [];
        }

        ICollection<Models.ActionGroup.ActionGroup> queueInternalMoves = await actionGroupRepository.GetNotStartedActionGroupsToTargetsForAgv(agv.CorrelationId, queue.NonEntryNodes);
        return queueInternalMoves.Select(ag => ag.CorrelationId).ToList();
    }
}