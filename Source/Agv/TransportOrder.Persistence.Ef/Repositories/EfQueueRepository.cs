using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IQueueRepository"/>>
public class EfQueueRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IQueueRepository
{
    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task AddQueue(Queue queue)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        List<string> nodeIds = queue.Nodes.Select(n => n.NodeId).ToList();
        Dictionary<string, EfNode> efNodes = await context.Set<EfNode>().Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id);
        Dictionary<string, NodeLock> nodeLocks = await context.Set<NodeLock>().Where(n => nodeIds.Contains(n.NodeId)).ToDictionaryAsync(n => n.NodeId);

        foreach (var node in queue.Nodes)
        {
            if (efNodes.TryGetValue(node.NodeId, out var efNode))
            {
                node.Node = efNode;
            }

            if (nodeLocks.TryGetValue(node.NodeId, out var nodeLock))
            {
                node.State = nodeLock;
            }
            else
            {
                node.State = new NodeLock(node.NodeId, NodeLockState.Free, false, null, DateTime.UtcNow);
                context.Add(node.State);
            }
        }

        context.Add(queue);
        await context.AddAsync(queue);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueues()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .AsSplitQuery().ToListAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<Queue> GetQueueById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var queue = await context.Set<Queue>()
            .AsSplitQuery()
            .Where(q => q.Id == id).SingleOrDefaultAsync();
        return queue ?? throw new QueueNotFoundException(id);
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task RemoveQueue(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var queue = await context.Set<Queue>()
            .AsSplitQuery()
            .Where(q => q.Id == id).SingleOrDefaultAsync();
        if (queue == null)
        {
            throw new QueueNotFoundException(id);
        }

        context.Remove(queue);
        await context.SaveChangesAsync();
    }
}