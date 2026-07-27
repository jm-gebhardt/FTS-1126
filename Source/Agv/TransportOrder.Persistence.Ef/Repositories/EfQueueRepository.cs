using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IQueueRepository"/>>
public class EfQueueRepository(
    IDbContextFactory<TransportOrderDbContext> dbContextFactory,
    ILogger<EfQueueRepository> logger) : IQueueRepository
{
    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task AddQueue(Queue queue, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Guid> nodeIds = queue.Nodes.Select(n => n.NodeId).ToList();
        Dictionary<Guid, GraphNode> efNodes = await context.Set<GraphNode>().Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, cancellationToken);

        foreach (var node in queue.Nodes)
        {
            if (efNodes.TryGetValue(node.NodeId, out var efNode))
            {
                node.Node = efNode;
            }
        }

        context.Add(queue);
        await context.AddAsync(queue, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<bool> UpdateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            context.Set<Queue>().Update(queue);
            return await context.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency conflict when updating queue {QueueId} {QueueName}. Another operation modified this queue. Retry configured",
                queue.Id, queue.Name);

           throw;
        }
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task UpdateQueueSize(Guid queueId, int newSize, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var queue = await context.Set<Queue>().FindAsync(new object[] { queueId }, cancellationToken);
            if (queue == null)
            {
                throw new QueueNotFoundException(queueId);
            }
            context.Entry(queue).Property(q => q.AgvFillQuantity).CurrentValue = newSize;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency conflict when updating queue size for queue {QueueId}. Retry configured",
                queueId);
            throw;
        }
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueues(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Queue>()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueByIdIfExisting(Guid? id, CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            return null;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Queue>()
            .Where(q => q.Id == id).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue> GetQueueById(Guid id, CancellationToken cancellationToken = default)
    {
        var queue = await GetQueueByIdIfExisting(id, cancellationToken);
        return queue ?? throw new QueueNotFoundException(id);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue> GetQueueByName(string name, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var queue = await context.Set<Queue>()
            .Where(q => q.Name == name)
            .SingleOrDefaultAsync(cancellationToken);
        return queue ?? throw new QueueNotFoundException(name);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task RemoveQueue(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var queue = await context.Set<Queue>()
            .Where(q => q.Id == id).SingleOrDefaultAsync(cancellationToken);
        if (queue == null)
        {
            throw new QueueNotFoundException(id);
        }

        context.Remove(queue);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueuesWithCapacity(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.Set<Queue>()
                .ToListAsync(cancellationToken))
            .Where(q => q.AgvFillQuantity != q.Nodes.Count).ToList();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Guid>> GetBlockedQueueNodes(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<Queue> queues = await context.Set<Queue>()
            .ToListAsync(cancellationToken);

        List<Guid> reservedOrOccupiedNodes = queues
            .Where(q => q.AgvFillQuantity > 0)
            .SelectMany(q => q.Nodes.Where(n => n.GetCurrentAgv() != null).Select(n => n.NodeId))
            .Distinct().ToList();
        List<Guid> reservedEntryNodes = queues.Where(q => q is { AllAgvsEntered: false, AgvFillQuantity: > 0 })
            .Where(q => q.EntryNode != null && !reservedOrOccupiedNodes.Contains(q.EntryNode.Value))
            .Select(q => q.EntryNode!.Value)
            .ToList();

        reservedOrOccupiedNodes.AddRange(reservedEntryNodes);
        return reservedOrOccupiedNodes;
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueByEntryNode(Guid entryNode, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Queue>()
            .Where(q => q.Nodes.OrderBy(p => p.Position).Last().NodeId == entryNode)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetOccupiedQueues(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.Set<Queue>()
                .ToListAsync(cancellationToken))
            .Where(q => q.Nodes.OrderBy(p => p.Position).First().GetCurrentAgv() != null).ToList();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueForAgv(Guid agvCorrelationId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.Set<Queue>()
            .ToListAsync(cancellationToken)).SingleOrDefault(q => q.Nodes.Any(n => n.AgvIsAssigned(agvCorrelationId)));
    }

    /// <inheritdoc />
    public async Task<Queue?> GetQueueByQueueNodeId(Guid queueNodeId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Queue>()
            .Where(q => q.Nodes.Any(n => n.NodeId == queueNodeId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueuesOnRoute(ICollection<Guid> nodes, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Queue>()
            .Where(q => q.Nodes.Any(n => nodes.Contains(n.NodeId)))
            .ToListAsync(cancellationToken);
    }
}