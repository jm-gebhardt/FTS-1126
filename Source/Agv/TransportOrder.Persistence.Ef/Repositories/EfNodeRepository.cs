using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
[CanBeCaptiveDependency]
public class GraphNodeRepository(ISemaphoreProvider<INodeStateDbSemaphore> semaphoreProvider, ILogger<GraphNodeRepository> logger, IDbContextFactory<TransportOrderDbContext> dbContextFactory) : INodeRepository
{
    private async Task<TransportOrderDbContext> CreateDbContextAsync()
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    /// <inheritdoc/>
    public async Task<ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName)>> GetNodeStatesForMapAsync(Guid mapCorrelationId)
    {
        await using var context = await CreateDbContextAsync();

        var nodes = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Where(m => m.CorrelationId == mapCorrelationId)
            .SelectMany(x => x.Nodes)
            .ToListAsync();

        var nodeIds = nodes.Select(n => n.Id).ToList();

        var stations = await context.Set<Station>()
            .Where(x => nodeIds.Contains(x.Id))
            .ToListAsync();

        var nodeLocks = await context.Set<NodeLock>()
            .Where(x => nodeIds.Contains(x.NodeId))
            .ToListAsync();

        var currentTime = DateTime.MinValue;

        var result = nodes
            .GroupJoin(
                stations,
                node => node.Id,
                station => station.Id,
                (node, stationGroup) => new { Node = node, Stations = stationGroup }
            )
            .SelectMany(
                x => x.Stations.DefaultIfEmpty(),
                (x, station) => new { x.Node, Station = station }
            )
            .GroupJoin(
                nodeLocks,
                x => x.Node.Id,
                nl => nl.NodeId,
                (x, nodeLockGroup) => new { x.Node, x.Station, NodeLocks = nodeLockGroup }
            )
            .SelectMany(
                x => x.NodeLocks.DefaultIfEmpty(),
                (x, nodeLock) => new
                {
                    x.Station,
                    NodeLock = new NodeLock(
                        x.Node,
                        nodeLock?.NodeState ?? NodeLockState.Free,
                        nodeLock?.IsLocked ?? false,
                        nodeLock?.AgvCorrelationId,
                        nodeLock?.ModifiedDate ?? currentTime
                    ),
                    x.Node.Name
                }
            )
            .ToList();

        return result
            .Select(x => (x.NodeLock, x.Station?.Id, x.Station?.LockReason, x.Station?.LockDescription, x.Station?.LockedBy, x.Name))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<GraphNode> GetNodeAsync(Guid nodeId)
    {
        await using var dbContext = await CreateDbContextAsync();

        var data = await dbContext.Set<GraphNode>()
            .LeftJoin(dbContext.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t => t.n.Id == nodeId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        var node = data.Select(x =>
        {
            x.n.Lock = x.nl;
            return x.n;
        }).Single();

        return node;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLock(GraphNode node, bool force = false, bool ignoreLocked = false)
    {
        var oldNode = await GetNodeAsync(node.Id);
        logger.LogInformation("Old node lock {Old} new node lock {New}", oldNode.Lock?.NodeState, node.Lock?.NodeState);
        if (oldNode.Lock is { IsLocked: true } && !node.Lock!.IsLocked)
        {
            return await UnlockNodeAsync(node, oldNode.Lock != null);
        }
        if (oldNode.Lock is not { IsLocked: true } && node.Lock!.IsLocked)
        {
            return await LockNodeAsync(node, oldNode.Lock != null);
        }
        if (node.Lock?.NodeState == NodeLockState.Reserved && !force)
        {
            return await ReserveNodeAsync(node, node.Lock!.AgvCorrelationId.GetValueOrDefault(), ignoreLocked);
        }
        if (node.Lock?.NodeState == NodeLockState.Reserved && force)
        {
            return await ForceReserveNodeAsync(node, node.Lock!.AgvCorrelationId.GetValueOrDefault(), ignoreLocked);
        }
        if (node.Lock?.NodeState == NodeLockState.Occupied && !force)
        {
            return await OccupyNodeAsync(node, node.Lock!.AgvCorrelationId.GetValueOrDefault(), ignoreLocked);
        }
        if (node.Lock?.NodeState == NodeLockState.Occupied && force)
        {
            return await ForceOccupyNodeAsync(node, node.Lock!.AgvCorrelationId.GetValueOrDefault(), ignoreLocked);
        }
        if (node.Lock?.NodeState == NodeLockState.Free)
        {
            return await FreeNodeAsync(node);
        }
        return await Task.FromResult(false);
    }

    private async Task<bool> UpdateNodeLockAsync(GraphNode node, bool updated, bool isLocked)
    {
        await using var dbContext = await CreateDbContextAsync();

        if (!updated)
        {
            await dbContext.AddAsync(new NodeLock(node, node.Lock?.NodeState ?? NodeLockState.Free, isLocked, node.Lock?.AgvCorrelationId, DateTime.UtcNow));
        }
        else 
        {
            dbContext.Update(new NodeLock(node, node.Lock?.NodeState ?? NodeLockState.Free, isLocked, node.Lock?.AgvCorrelationId, DateTime.UtcNow));
        }
        int result = await dbContext.SaveChangesAsync();
        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<ICollection<GraphNode>> GetNodesForAgv(Guid agvCorrelationId)
    {
        await using var dbContext = await CreateDbContextAsync();

        var data = await dbContext.Set<GraphNode>()
            .Join(dbContext.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t => t.nl.AgvCorrelationId == agvCorrelationId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        return data.Select(x =>
        {
            x.n.Lock = x.nl;
            return x.n;
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Guid>?> GetLockedNodes()
    {
        await using var dbContext = await CreateDbContextAsync();

        return await dbContext.Set<GraphNode>()
            .Join(dbContext.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t =>  t.nl.IsLocked)
            .Select(t => t.n.Id)
            .ToListAsync();
    }

    private Task<bool> LockNodeAsync(GraphNode node, bool updated)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(node.Id))
            {
                logger.LogDebug("Locking node {NodeId}", node.Id);
                return await UpdateNodeLockAsync(node, updated, true);
            }
            logger.LogDebug("Could not lock node {NodeId} because it is already locked", node.Id);
            return false;
        });
    }

    private Task<bool> UnlockNodeAsync(GraphNode node, bool updated)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (IsLocked(node.Id))
            {
                logger.LogDebug("Unlocking node {NodeId}", node.Id);
                return await UpdateNodeLockAsync(node, updated, false);
            }
            logger.LogDebug("Could not unlock node {NodeId} because it is not locked", node.Id);
            return false;
        });
    }

    private Task<bool> FreeNodeAsync(GraphNode node)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            logger.LogInformation("Free node {NodeId}", node.Id);
            return await UpdateNodeAsync(node, NodeLockState.Free, null);
        });
    }

    private Task<bool> ForceReserveNodeAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked = false)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(node.Id) || ignoreLocked)
            {
                logger.LogDebug("Force reserving node {NodeId}, for Agv {AgvCorrelationId}", node.Id, agvCorrelationId);
                return await UpdateNodeAsync(node, NodeLockState.Reserved, agvCorrelationId, true);
            }
            logger.LogDebug("Could not reserve node {NodeId} for agv {AgvCorrelationId} because node is locked", node.Id, agvCorrelationId);
            return false;
        });
    }

    private Task<bool> ReserveNodeAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked = false)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if ((!IsLocked(node.Id) || ignoreLocked) && IsFree(node.Id))
            {
                logger.LogDebug("Reserving node {NodeId}, for Agv {AgvCorrelationId}", node.Id, agvCorrelationId);
                return await UpdateNodeAsync(node, NodeLockState.Reserved, agvCorrelationId);
            }
            logger.LogDebug("Could not reserve node {NodeId} for agv {AgvCorrelationId} because node is locked or not free", node.Id, agvCorrelationId);
            return false;
        });
    }

    private bool IsLocked(Guid nodeId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.IsLocked);
    }

    private bool IsInState(Guid nodeId, NodeLockState state)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState == state);
    }

    private bool IsFree(Guid nodeId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return !dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState != NodeLockState.Free);
    }

    private async Task<bool> UpdateNodeAsync(GraphNode node, NodeLockState nodeState, Guid? agvCorrelationId, bool force = false)
    {
        await using var dbContext = await CreateDbContextAsync();
        var currentLock = await dbContext.Set<NodeLock>()
            .SingleOrDefaultAsync(n => n.NodeId == node.Id);

        if (currentLock == null)
        {
            await dbContext.AddAsync(new NodeLock(node, nodeState, false, agvCorrelationId, DateTime.UtcNow));
            return await dbContext.SaveChangesAsync() > 0;
        }

        if (currentLock.NodeState != nodeState || force)
        {
            dbContext.Update(new NodeLock(node, nodeState, currentLock.IsLocked, agvCorrelationId, DateTime.UtcNow));
            return await dbContext.SaveChangesAsync() > 0;
        }

        // node already in state and no forced update
        return false;
    }

    private async Task<bool> OccupyNodeAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked=false)
    {
        if ((IsLocked(node.Id) && !ignoreLocked) || IsInState(node.Id, NodeLockState.Occupied))
        {
            logger.LogDebug("Could not not occupy node {NodeId} because it is locked or already occupied", node.Id);
            return false;
        }

        logger.LogDebug("Occupy node {NodeId} with AGV {CorrelationId}", node.Id, agvCorrelationId);
        return await UpdateNodeAsync(node, NodeLockState.Occupied, agvCorrelationId);
    }

    private async Task<bool> ForceOccupyNodeAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked = false)
    {
        if (IsLocked(node.Id) && !ignoreLocked)
        {
            logger.LogDebug("Could not not occupy node {NodeId} with agv {AgvCorrelationId} because it is locked", node.Id, agvCorrelationId);
            return false;
        }

        logger.LogDebug("Occupy node {NodeId} with AGV {AgvCorrelationId}", node.Id, agvCorrelationId);
        return await UpdateNodeAsync(node, NodeLockState.Occupied, agvCorrelationId, force: true);
    }
}