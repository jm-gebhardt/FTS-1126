using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Node;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IQueueManager"/>
public class QueueManager : BaseNodeReservationManager, IQueueManager
{
    private readonly IQueueRepository _queueRepository;

    /// <inheritdoc />
    public QueueManager(IAgvDb agvDb,
        IQueueRepository queueRepository,
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        ILogger<BaseNodeReservationManager> logger,
        ISemaphoreProvider<IParkingSemaphore> semaphoreProvider) : base(agvDb, nodeStateRepository, routeService, logger, semaphoreProvider) // use parking semaphore to allow park nodes to be queue nodes
    {
        _queueRepository = queueRepository;
    }

    /// <inheritdoc />
    protected override string Description => "Queue";

    private string[] _queueNodes = [];

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => _queueNodes.Contains(node.Id);

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<string?> TryEnterQueue(Agv agv)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await GetQueue(agv);
            string? entryNode = queue?.TryEnterQueue();
            if (entryNode == null)
            {
                Logger.LogWarning("No queue available for entering agv {AgvCorrelationId} to queue", agv.CorrelationId);
                return null;
            }
            agv.IsQueueing = true;
            await AgvDb.Agvs.UpdateAgvAsync(agv);
            await _queueRepository.UpdateQueueSize(queue!.Id, queue.AgvFillQuantity);
            Logger.LogInformation("Queue {QueueName} selected for entering agv {AgvCorrelationId}", queue.Name, agv.CorrelationId);
            return entryNode;
        });
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<string?> TryEnqueueAgv(Guid agvCorrelationId, string entryNode)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await _queueRepository.GetQueueByEntryNode(entryNode);
            if (queue == null)
            {
                return null;
            }
            if (queue.IsEnqueued(agvCorrelationId))
            {
                Logger.LogWarning("Agv {AgvCorrelation} has already been enqueued to queue {QueueName}", agvCorrelationId, queue.Name);
                return null;
            }
            if (queue.TryEnqueue(agvCorrelationId, out string? target))
            {
                Logger.LogInformation("Enqueuing agv {AgvCorrelationId} to node {TargetNode} of queue {QueueName}", agvCorrelationId, target, queue.Name);
                if (entryNode != target)
                {
                    await NodeStateRepository.ForceReserveNodeAsync(target, agvCorrelationId);
                }
                else
                {
                    await NodeStateRepository.ForceOccupyNodeAsync(target, agvCorrelationId);
                }
                return target;
            }
            Logger.LogWarning("Could not enqueue agv {AgvCorrelation} to queue {QueueName}", agvCorrelationId, queue.Name);
            return null;
        });
    }

    private async Task<Queue?> GetQueue(Agv agv)
    {
        ICollection<Queue> queues = await _queueRepository.GetQueuesWithCapacity();
        if (queues.Count == 0)
        {
            Logger.LogWarning("No queues with capacity available for enqueuing agv {AgvCorrelation}", agv.CorrelationId);
            return null;
        }
        _queueNodes = queues.Select(q => q.EntryNode!).ToArray();
        try
        {
            var targetNode = await PickQueue(agv);
            return queues.First(q => q.EntryNode == targetNode.Id);
        }
        catch
        {
            var queue = queues.First();
            return queue;
        }
    }

    private async Task<GraphNode> PickQueue(Agv agv)
    {
        var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
        var node = await PickNode(agv, agvData, agv.AgvStatus?.LastNodeId);
        return node;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<Dictionary<Guid, string>?> TryDequeueAgv(Guid agvCorrelationId)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await _queueRepository.GetQueueForAgv(agvCorrelationId);
            if (queue == null)
            {
                Logger.LogDebug("Could not dequeue agv {AgvCorrelationId}, no queue found", agvCorrelationId);
                return null;
            }
            if (!queue.IsAgvAtQueueHead(agvCorrelationId, queue.Head?.NodeId ?? string.Empty))
            {
                Logger.LogDebug("Could not dequeue agv from queue {QueueName}, agv {AgvCorrelationId} is not at head position", queue.Name, agvCorrelationId);
                return null;
            }

            queue.TryDequeue(out Dictionary<Guid, string> agvsToMove);
            await _queueRepository.UpdateQueueSize(queue.Id, queue.AgvFillQuantity);
            Logger.LogInformation("Dequeued agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
            foreach (KeyValuePair<Guid, string> agvToMove in agvsToMove)
            {
                await NodeStateRepository.ForceReserveNodeAsync(agvToMove.Value, agvToMove.Key);
            }
            return agvsToMove;
        });
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<Dictionary<Guid, string>?> TryEscapeQueue(Guid agvCorrelationId)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var queue = await _queueRepository.GetQueueForAgv(agvCorrelationId);
            if (queue == null)
            {
                Logger.LogDebug("Agv {AgvCorrelationId} could not escape queue, no queue found", agvCorrelationId);
                return null;
            }
            string? node = queue.TryEscapeQueue(agvCorrelationId, out Dictionary<Guid, string> agvsToMove);
            if (node == null)
            {
                Logger.LogDebug("Could not escape agv {AgvCorrelationId} from queue {QueueName}", agvCorrelationId, queue.Name);
                return null;
            }
            await _queueRepository.UpdateQueueSize(queue.Id, queue.AgvFillQuantity);
            foreach (KeyValuePair<Guid, string> agvToMove in agvsToMove)
            {
                await NodeStateRepository.ForceReserveNodeAsync(agvToMove.Value, agvToMove.Key);
            }
            Logger.LogInformation("Agv {AgvCorrelationId} escaped from queue {QueueName}", agvCorrelationId, queue.Name);
            return agvsToMove;
        });
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task TryReachQueueHead(Agv agv, string nodeId)
    {
        var queue = await _queueRepository.GetQueueForAgv(agv.CorrelationId);
        if (queue == null)
        {
            Logger.LogDebug("Agv {AgvCorrelationId} could not reach queue head {Head}, no queue found", agv.CorrelationId, nodeId);
            return;
        }
        if (queue.IsAgvAtQueueHead(agv.CorrelationId, nodeId))
        {
            agv.IsQueueing = false;
            await AgvDb.Agvs.UpdateAgvAsync(agv);
            Logger.LogInformation("Agv {AgvCorrelationId} reached queue head {Head}", agv.CorrelationId, nodeId);
            return;
        }
        Logger.LogDebug("Agv {AgvCorrelationId} could not reach queue head {Head}, agv does not occupy head {NodeId}", agv.CorrelationId, nodeId, queue.Head?.NodeId);
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<ICollection<Guid>> GetAgvsAtQueueHeads()
    {
        var agvs = new List<Guid>();
        foreach (var queue in await _queueRepository.GetOccupiedQueues())
        {
            if (queue.Head?.State != null)
            {
                agvs.Add(queue.Head!.State!.AgvCorrelationId!.Value);
            }
        }
        return agvs;
    }

    /// <inheritdoc cref="IQueueManager"/>
    public async Task<bool> IsOnAQueueNode(Guid agvCorrelationId)
    {
        var queue = await _queueRepository.GetQueueForAgv(agvCorrelationId);
        return queue?.Nodes.Any(n => n.State?.IsOccupiedByAgv(agvCorrelationId) ?? false) ?? false;
    }
}