using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Entity framework implementation of <see cref="INodeStateRepository"/>
/// </summary>
public class EfNodeStateRepository : INodeStateRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ISemaphoreProvider<INodeStateDbSemaphore> _semaphoreProvider;
    private readonly ILogger<EfNodeStateRepository> _logger;

    /// <summary>
    /// 
    /// </summary>
    public EfNodeStateRepository(
        IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ISemaphoreProvider<INodeStateDbSemaphore> semaphoreProvider,
        ILogger<EfNodeStateRepository> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _semaphoreProvider = semaphoreProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> LockNodeAsync(string nodeId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(nodeId))
            {
                _logger.LogDebug("Locking node {NodeId}", nodeId);
                return await UpdateNodeLockAsync(nodeId, true);
            }
            _logger.LogDebug("Could not lock node {NodeId} because it is already locked", nodeId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> UnlockNodeAsync(string nodeId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (IsLocked(nodeId))
            {
                _logger.LogDebug("Unlocking node {NodeId}", nodeId);
                return await UpdateNodeLockAsync(nodeId, false);
            }
            _logger.LogDebug("Could not unlock node {NodeId} because it is not locked", nodeId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> ReserveNodeAsync(string nodeId, Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(nodeId) && IsFree(nodeId))
            {
                _logger.LogDebug("Reserving node {NodeId}, for Agv {CorrelationId}", nodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeId, NodeLockState.Reserved, agvCorrelationId);
            }
            _logger.LogDebug("Could not reserve node {NodeId} for agv {CorrelationId} because node is locked or not free.", nodeId, agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> UnReserveNodeAsync(Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var nodeLock = GetNodeLockInState(agvCorrelationId, NodeLockState.Reserved);
            if (nodeLock != null)
            {
                _logger.LogDebug("UnReserve node {NodeId}, reserved for Agv {CorrelationId}", nodeLock.NodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
            }
            // ReSharper disable once StringLiteralTypo
            _logger.LogDebug("Could not unreserve any node because no node is reserved for agv {CorrelationId}", agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public async Task<bool> OccupyNodeAsync(string nodeId, Guid agvCorrelationId)
    {
        var currentState = GetNodeLock(nodeId);
        if (currentState is { IsLocked: true } or { NodeState: NodeLockState.Occupied })
        {
            _logger.LogDebug("Could not not occupy node {NodeId} because it is locked or already occupied", nodeId);
            return false;
        }
        
        _logger.LogDebug("Occupy node {NodeId} with AGV {CorrelationId}", nodeId, agvCorrelationId);
        return await UpdateNodeAsync(nodeId, NodeLockState.Occupied, agvCorrelationId);
    }

    /// <inheritdoc />
    public Task<bool> UnOccupyNodeAsync(Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var nodeLock = GetNodeLockInState(agvCorrelationId, NodeLockState.Occupied);
            if (nodeLock != null)
            {
                _logger.LogDebug("UnOccupy node {NodeId}, occupied by agv {CorrelationId}", nodeLock.NodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
            }
            // ReSharper disable once StringLiteralTypo
            _logger.LogDebug("Could not unoccupy any node because no node is occupied by agv {CorrelationId}", agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> FreeNodeAsync(string nodeId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            _logger.LogDebug("Free node {NodeId}", nodeId);
            return await UpdateNodeAsync(nodeId, NodeLockState.Free, null);
        });
    }

    /// <inheritdoc />
    public IEnumerable<GraphNode> FilterFreeUnlockedNodes(IEnumerable<GraphNode> nodes)
    {
        List<NodeLock> blockedNodes = _dbContextFactory.CreateDbContext()
            .Set<NodeLock>()
            .Where(n => n.NodeState != NodeLockState.Free || n.IsLocked).ToList();

        return nodes.Where(n => blockedNodes.All(n2 => n2.NodeId != n.Id));
    }

    /// <inheritdoc />
    public async Task<NodeLock?> GetNodeStateAsync(string nodeId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext
            .Set<NodeLock>()
            .SingleOrDefaultAsync(nl => nl.NodeId == nodeId);
    }

    /// <inheritdoc />
    public async Task<bool> IsAnyNodeReservedForAgv(Guid agvCorrelationId, ICollection<string> nodes)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<NodeLock>()
            .AnyAsync(n => nodes.Contains(n.NodeId) && n.AgvCorrelationId == agvCorrelationId && n.NodeState == NodeLockState.Reserved);
    }

    /// <inheritdoc />
    public async Task<NodeLock?> GetReservedNodeLockForAgv(Guid agvCorrelationId, ICollection<string> nodes)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<NodeLock>()
            .AsNoTracking()
            .FirstOrDefaultAsync(n =>
                nodes.Contains(n.NodeId)
                && n.AgvCorrelationId == agvCorrelationId
                && n.NodeState == NodeLockState.Reserved);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>?> GetLockedNodes()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<NodeLock>()
            .Where(nl => nl.IsLocked).Select(nl => nl.NodeId)
            .ToListAsync();
   }

    private bool IsLocked(string nodeId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.IsLocked);
    }

    /// <inheritdoc />
    public async Task<bool> IsNodeReservedAsync(string nodeId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState == NodeLockState.Reserved);
    }

    private bool IsFree(string nodeId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return !dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState != NodeLockState.Free);
    }

    private NodeLock? GetNodeLock(string nodeId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Set<NodeLock>()
            .AsNoTracking()
            .FirstOrDefault(n => n.NodeId == nodeId);
    }

    private NodeLock? GetNodeLockInState(Guid agvCorrelationId, NodeLockState nodeLockState)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        return dbContext.Set<NodeLock>()
            .AsNoTracking()
            .FirstOrDefault(n => n.AgvCorrelationId == agvCorrelationId && n.NodeState == nodeLockState);
    }

    private async Task<bool> UpdateNodeAsync(string nodeId, NodeLockState nodeState, Guid? agvCorrelationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var currentLock = await dbContext
            .Set<NodeLock>()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NodeId == nodeId);
        if (currentLock == null)
        {
            await dbContext.AddAsync(new NodeLock(nodeId, nodeState, false, agvCorrelationId, DateTime.UtcNow));
        }
        else if (currentLock.NodeState != nodeState)
        {
            dbContext.Update(new NodeLock(nodeId, nodeState, currentLock.IsLocked, agvCorrelationId, DateTime.UtcNow));
        }
        else
        {
            // node already in state
            return false;
        }

        int result = await dbContext.SaveChangesAsync();
        return result > 0;
    }

    private async Task<bool> UpdateNodeLockAsync(string nodeId, bool isLocked)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var currentLock = await dbContext
            .Set<NodeLock>()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NodeId == nodeId);
        if (currentLock == null)
        {
            await dbContext.AddAsync(new NodeLock(nodeId, NodeLockState.Free, isLocked, null, DateTime.UtcNow));
        }
        else if (currentLock.IsLocked != isLocked)
        {
            dbContext.Update(new NodeLock(nodeId, currentLock.NodeState, isLocked, currentLock.AgvCorrelationId, DateTime.UtcNow));
        }
        else
        {
            // Node already locked
            return false;
        }

        int result = await dbContext.SaveChangesAsync();
        return result > 0;
    }
}