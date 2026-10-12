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
    public async Task<Queue> AddQueue(Queue queue)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        
        var existing = await context.Set<Queue>().SingleOrDefaultAsync(q => q.Name == queue.Name);
        if (existing != null)
        {
            return MatchDefinition(existing, queue);
        }
        
        var nodeIds = queue.Nodes.Select(n => n.NodeId).ToList();
        var efNodes = await context.Set<GraphNode>().Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id);
        var missingNodes = nodeIds.Where(id => !efNodes.ContainsKey(id)).Distinct().ToList();
        if (missingNodes.Count != 0)
        {
            throw new QueueNodesNotReadyException(queue.Name, missingNodes);
        }

        foreach (var node in queue.Nodes)
        {
            if (efNodes.TryGetValue(node.NodeId, out var efNode))
            {
                node.Node = efNode;
            }
        }

        await context.AddAsync(queue);
        await context.SaveChangesAsync();
        return queue;
    }

    private static Queue MatchDefinition(Queue existing, Queue requested)
    {
        if (existing.Nodes.Count != requested.Nodes.Count ||
            !existing.Nodes.Select(n => (n.NodeId, n.Position)).ToHashSet()
                .SetEquals(requested.Nodes.Select(n => (n.NodeId, n.Position))))
        {
            throw new QueueDefinitionConflictException(requested.Name);
        }
        return existing;
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<bool> UpdateQueueAsync(Queue queue)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        try
        {
            context.Set<Queue>().Update(queue);
            return await context.SaveChangesAsync() > 0;
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
    public async Task UpdateQueueSize(Guid queueId, int newSize)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        try
        {
            var queue = await context.Set<Queue>().FindAsync(queueId);
            if (queue == null)
            {
                throw new QueueNotFoundException(queueId);
            }
            context.Entry(queue).Property(q => q.AgvFillQuantity).CurrentValue = newSize;
            await context.SaveChangesAsync();
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
    public async Task<ICollection<Queue>> GetQueues()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .ToListAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueByIdIfExisting(Guid? id)
    {
        if (id == null)
        {
            return null;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .Where(q => q.Id == id).SingleOrDefaultAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue> GetQueueById(Guid id)
    {
        var queue = await GetQueueByIdIfExisting(id);
        return queue ?? throw new QueueNotFoundException(id);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue> GetQueueByName(string name)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var queue = await context.Set<Queue>()
            .Where(q => q.Name == name)
            .SingleOrDefaultAsync();
        return queue ?? throw new QueueNotFoundException(name);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task RemoveQueue(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var queue = await context.Set<Queue>()
            .Where(q => q.Id == id).SingleOrDefaultAsync();
        if (queue == null)
        {
            throw new QueueNotFoundException(id);
        }

        context.Remove(queue);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueuesWithCapacity()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Set<Queue>()
                .ToListAsync())
            .Where(q => q.AgvFillQuantity != q.Nodes.Count).ToList();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Guid>> GetBlockedQueueNodes()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var queues = await context.Set<Queue>()
            .ToListAsync();

        var reservedOrOccupiedNodes = queues
            .Where(q => q.AgvFillQuantity > 0)
            .SelectMany(q => q.Nodes.Where(n => n.GetCurrentAgv() != null).Select(n => n.NodeId))
            .Distinct().ToList();
        var reservedEntryNodes = queues.Where(q => q is { AllAgvsEntered: false, AgvFillQuantity: > 0 })
            .Where(q => q.EntryNode != null && !reservedOrOccupiedNodes.Contains(q.EntryNode.Value))
            .Select(q => q.EntryNode!.Value)
            .ToList();

        reservedOrOccupiedNodes.AddRange(reservedEntryNodes);
        return reservedOrOccupiedNodes;
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueByEntryNode(Guid entryNode)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .Where(q => q.Nodes.OrderBy(p => p.Position).Last().NodeId == entryNode)
            .SingleOrDefaultAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetOccupiedQueues()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Set<Queue>()
                .ToListAsync())
            .Where(q => q.Nodes.OrderBy(p => p.Position).First().GetCurrentAgv() != null).ToList();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue?> GetQueueForAgv(Guid agvCorrelationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Set<Queue>()
            .ToListAsync()).SingleOrDefault(q => q.Nodes.Any(n => n.AgvIsAssigned(agvCorrelationId)));
    }

    /// <inheritdoc />
    public async Task<Queue?> GetQueueByQueueNodeId(Guid queueNodeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .Where(q => q.Nodes.Any(n => n.NodeId == queueNodeId))
            .SingleOrDefaultAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueuesOnRoute(ICollection<Guid> nodes)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .Where(q => q.Nodes.Any(n => nodes.Contains(n.NodeId)))
            .ToListAsync();
    }
}