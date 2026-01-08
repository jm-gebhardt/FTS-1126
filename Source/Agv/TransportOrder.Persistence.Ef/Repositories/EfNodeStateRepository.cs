using AutoMapper;
using Gebhardt.Acc.Common;
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
[CanBeCaptiveDependency]
public class GraphNodeStateRepository : INodeStateRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ISemaphoreProvider<INodeStateDbSemaphore> _semaphoreProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<GraphNodeStateRepository> _logger;

    /// <summary>
    /// 
    /// </summary>
    public GraphNodeStateRepository(
        IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ISemaphoreProvider<INodeStateDbSemaphore> semaphoreProvider,
        IMapper mapper,
        ILogger<GraphNodeStateRepository> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _semaphoreProvider = semaphoreProvider;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> LockNodeAsync(Guid nodeId)
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
    public Task<bool> UnlockNodeAsync(Guid nodeId)
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
    public Task<bool> ReserveNodeAsync(Guid nodeId, Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(nodeId) && IsFree(nodeId))
            {
                _logger.LogDebug("Reserving node {NodeId}, for Agv {AgvCorrelationId}", nodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeId, NodeLockState.Reserved, agvCorrelationId);
            }
            _logger.LogDebug("Could not reserve node {NodeId} for agv {AgvCorrelationId} because node is locked or not free", nodeId, agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> ForceReserveNodeAsync(Guid nodeId, Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!IsLocked(nodeId))
            {
                _logger.LogDebug("Force reserving node {NodeId}, for Agv {AgvCorrelationId}", nodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeId, NodeLockState.Reserved, agvCorrelationId, true);
            }
            _logger.LogDebug("Could not reserve node {NodeId} for agv {AgvCorrelationId} because node is locked", nodeId, agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> UnReserveNodeAsync(Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var nodeLock = await GetNodeLockInState(agvCorrelationId, NodeLockState.Reserved);
            if (nodeLock != null)
            {
                _logger.LogDebug("UnReserve node {NodeId}, reserved for Agv {AgvCorrelationId}", nodeLock.NodeId, agvCorrelationId);
                return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
            }
            // ReSharper disable once StringLiteralTypo
            _logger.LogDebug("Could not unreserve any node because no node is reserved for agv {AgvCorrelationId}", agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public async Task<bool> OccupyNodeAsync(Guid nodeId, Guid agvCorrelationId)
    {
        var currentState = await GetNodeStateAsync(nodeId);
        if (currentState is { IsLocked: true } or { NodeState: NodeLockState.Occupied })
        {
            _logger.LogDebug("Could not not occupy node {NodeId} because it is locked or already occupied", nodeId);
            return false;
        }

        _logger.LogDebug("Occupy node {NodeId} with AGV {CorrelationId}", nodeId, agvCorrelationId);
        return await UpdateNodeAsync(nodeId, NodeLockState.Occupied, agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<bool> ForceOccupyNodeAsync(Guid nodeId, Guid agvCorrelationId)
    {
        var currentState = await GetNodeStateAsync(nodeId);
        if (currentState is { IsLocked: true })
        {
            _logger.LogDebug("Could not not occupy node {NodeId} with agv {AgvCorrelationId} because it is locked", nodeId, agvCorrelationId);
            return false;
        }

        _logger.LogDebug("Occupy node {NodeId} with AGV {AgvCorrelationId}", nodeId, agvCorrelationId);
        return await UpdateNodeAsync(nodeId, NodeLockState.Occupied, agvCorrelationId, force: true);
    }

    /// <inheritdoc />
    public Task<bool> UnOccupyNodeAsync(Guid agvCorrelationId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var nodeLock = await GetNodeLockInState(agvCorrelationId, NodeLockState.Occupied);
            if (nodeLock != null)
            {
                return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
            }
            // ReSharper disable once StringLiteralTypo
            _logger.LogDebug("Could not unoccupy any node because no node is occupied by agv {AgvCorrelationId}", agvCorrelationId);
            return false;
        });
    }

    /// <inheritdoc />
    public Task<bool> FreeNodeAsync(Guid nodeId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            _logger.LogInformation("Free node {NodeId}", nodeId);
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
    public async Task<NodeLock?> GetNodeStateAsync(Guid nodeId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var data = await dbContext.Set<NodeLock>()
            .AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(),
                nl => nl.NodeId,
                n => n.Id,
                (nl, n) => new { nl, n })
            .Where(t => t.nl.NodeId == nodeId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        var nodeLock = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).SingleOrDefault();

        return nodeLock;
    }

    /// <inheritdoc />
    public async Task<bool> IsAnyNodeReservedForAgv(Guid agvCorrelationId, ICollection<Guid> nodes)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<NodeLock>()
            .AnyAsync(n => nodes.Contains(n.NodeId) && n.AgvCorrelationId == agvCorrelationId && n.NodeState == NodeLockState.Reserved);
    }

    /// <inheritdoc />
    public async Task<NodeLock?> GetReservedNodeLockForAgv(Guid agvCorrelationId, ICollection<Guid> nodes)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var data = await dbContext.Set<NodeLock>()
            .AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(), nl => nl.NodeId, n => n.Id, (nl, n) => new { nl, n })
            .Where(t => nodes.Contains(t.nl.NodeId) && t.nl.AgvCorrelationId == agvCorrelationId && t.nl.NodeState == NodeLockState.Reserved)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();
        var nodeLock = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).SingleOrDefault();

        return nodeLock;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>?> GetLockedNodes()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<NodeLock>()
            .Where(nl => nl.IsLocked)
            .Select(nl => nl.NodeId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<NodeLock>> GetNodeLocksForAgv(Guid agvCorrelationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var data = await dbContext.Set<NodeLock>()
            .AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(), nl => nl.NodeId, n => n.Id, (nl, n) => new { nl, n })
            .Where(t => t.nl.AgvCorrelationId == agvCorrelationId).Select(t => new { t.nl, t.n }).ToListAsync();
        List<NodeLock> nodeLocks = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).ToList();
        return nodeLocks;
    }

    private bool IsLocked(Guid nodeId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.IsLocked);
    }

    /// <inheritdoc />
    public async Task<bool> IsNodeReservedAsync(Guid nodeId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState == NodeLockState.Reserved);
    }

    private bool IsFree(Guid nodeId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return !dbContext.Set<NodeLock>()
            .Any(n => n.NodeId == nodeId && n.NodeState != NodeLockState.Free);
    }

    private async Task<NodeLock?> GetNodeLockInState(Guid agvCorrelationId, NodeLockState nodeLockState)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var data = await dbContext.Set<NodeLock>()
            .AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(), nl => nl.NodeId, n => n.Id, (nl, n) => new { nl, n })
            .Where(t => t.nl.AgvCorrelationId == agvCorrelationId && t.nl.NodeState == nodeLockState)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();
        var nodeLock = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).SingleOrDefault();

        return nodeLock;
    }

    private async Task<bool> UpdateNodeAsync(Guid nodeId, NodeLockState nodeState, Guid? agvCorrelationId, bool force = false)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var data = await dbContext.Set<NodeLock>().AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(), nl => nl.NodeId, n => n.Id, (nl, n) => new { nl, n }).Where(t => t.nl.NodeId == nodeId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();
        var currentLock = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).SingleOrDefault();

        if (currentLock == null)
        {
            var node = await dbContext.Set<GraphNode>().AsNoTracking().SingleOrDefaultAsync(n => n.Id == nodeId);
            await dbContext.AddAsync(new NodeLock(_mapper.Map<GraphNode>(node), nodeState, false, agvCorrelationId, DateTime.UtcNow));
            return await dbContext.SaveChangesAsync() > 0;
        }

        if (currentLock.NodeState != nodeState || force)
        {
            dbContext.Update(new NodeLock(currentLock.Node, nodeState, currentLock.IsLocked, agvCorrelationId, DateTime.UtcNow));
            return await dbContext.SaveChangesAsync() > 0;
        }

        // node already in state and no forced update
        return false;
    }

    private async Task<bool> UpdateNodeLockAsync(Guid nodeId, bool isLocked)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var data = await dbContext.Set<NodeLock>().AsNoTracking()
            .Join(dbContext.Set<GraphNode>().AsNoTracking(), nl => nl.NodeId, n => n.Id, (nl, n) => new { nl, n })
            .Where(t => t.nl.NodeId == nodeId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();
        var currentLock = data.Select(x =>
        {
            x.nl.Node = _mapper.Map<GraphNode>(x.n);
            return x.nl;
        }).SingleOrDefault();

        if (currentLock == null)
        {
            var node = await dbContext.Set<GraphNode>().AsNoTracking().SingleOrDefaultAsync(n => n.Id == nodeId);
            await dbContext.AddAsync(new NodeLock(_mapper.Map<GraphNode>(node), NodeLockState.Free, isLocked, null, DateTime.UtcNow));
        }
        else if (currentLock.IsLocked != isLocked)
        {
            dbContext.Update(new NodeLock(currentLock.Node, currentLock.NodeState, isLocked, currentLock.AgvCorrelationId, DateTime.UtcNow));
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