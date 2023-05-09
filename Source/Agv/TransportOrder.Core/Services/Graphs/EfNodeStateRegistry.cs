using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs
{
    /// <summary>
    /// Entity framework implementation of <see cref="INodeStateRegistry"/>
    /// </summary>
    public class EfNodeStateRegistry : INodeStateRegistry

    {
        private readonly SemaphoreSlim _dbLock = new(1, 1);
        private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
        private readonly ILogger<EfNodeStateRegistry> _logger;

        /// <summary>
        /// 
        /// </summary>
        public EfNodeStateRegistry(ILogger<EfNodeStateRegistry> logger, IDbContextFactory<TransportOrderDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> LockNodeAsync(string nodeId)
        {
            try
            {
                await _dbLock.WaitAsync();
                if (!IsLocked(nodeId))
                {
                    _logger.LogDebug("Locking node {NodeId}", nodeId);
                    return await UpdateNodeLockAsync(nodeId, true);
                }
                _logger.LogDebug("Could not lock node {NodeId} because it is already locked", nodeId);
                return false;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> UnlockNodeAsync(string nodeId)
        {
            try
            {
                await _dbLock.WaitAsync();
                if (IsLocked(nodeId))
                {
                    _logger.LogDebug("Unlocking node {NodeId}", nodeId);
                    return await UpdateNodeLockAsync(nodeId, false);
                }
                _logger.LogDebug("Could not unlock node {NodeId} because it is not locked", nodeId);
                return false;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> ReserveNodeAsync(string nodeId, Guid agvCorrelationId)
        {
            try
            {
                await _dbLock.WaitAsync();
                if (!IsLocked(nodeId) && IsFree(nodeId))
                {
                    _logger.LogDebug("Reserving node {NodeId}, for Agv {CorrelationId}", nodeId, agvCorrelationId);
                    return await UpdateNodeAsync(nodeId, NodeLockState.Reserved, agvCorrelationId);
                }
                _logger.LogDebug("Could not reserve node {NodeId} for Agv {CorrelationId} because node is not free or unlocked", nodeId, agvCorrelationId);
                return false;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> UnReserveNodeAsync(Guid agvCorrelationId)
        {
            try
            {
                await _dbLock.WaitAsync();
                var nodeLock = GetNodeLock(agvCorrelationId);
                if (nodeLock != null)
                {
                    _logger.LogDebug("UnReserve node {NodeId}, reserved for Agv {CorrelationId}", nodeLock.NodeId, agvCorrelationId);
                    return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
                }
                _logger.LogDebug("Could not unreserve any node because no node is reserved for Agv {CorrelationId}", agvCorrelationId);
                return false;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> OccupyNodeAsync(string nodeId, Guid agvCorrelationId)
        {
            var currentState = GetNodeLock(nodeId);
            if (currentState is {IsLocked: true} or {NodeState: NodeLockState.Occupied})
            {
                _logger.LogDebug("Could not not occupy node {NodeId} because it is locked or already occupied", nodeId);
                return false;
            }
            _logger.LogDebug("Occupy node {NodeId} with AGV {CorrelationId}", nodeId, agvCorrelationId);
            return await UpdateNodeAsync(nodeId, NodeLockState.Occupied, agvCorrelationId);
        }

        /// <inheritdoc />
        public async Task<bool> UnOccupyNodeAsync(Guid agvCorrelationId)
        {
            try
            {
                await _dbLock.WaitAsync();
                var nodeLock = GetNodeLock(agvCorrelationId);
                if (nodeLock is {NodeState: NodeLockState.Occupied})
                {
                    _logger.LogDebug("UnOccupy node {NodeId}, occupied by Agv {CorrelationId}", nodeLock.NodeId, agvCorrelationId);
                    return await UpdateNodeAsync(nodeLock.NodeId, NodeLockState.Free, null);
                }
                _logger.LogDebug("Could not unoccupy any node because no node is occupied by Agv {CorrelationId}", agvCorrelationId);
                return false;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> FreeNodeAsync(string nodeId)
        {
            try
            {
                await _dbLock.WaitAsync();
                _logger.LogDebug("Free node {NodeId}", nodeId);
                return await UpdateNodeAsync(nodeId, NodeLockState.Free, null);
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> FilterFreeUnlockedNodes(IEnumerable<Node> nodes)
        {
            List<NodeLock> blockedNodes = _dbContextFactory.CreateDbContext()
                .Set<NodeLock>()
                .Where(n => n.NodeState != NodeLockState.Free || n.IsLocked).ToList();

            return nodes.Where(n => blockedNodes.All(n2 => n2.NodeId != n.Id));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private NodeLock? GetNodeLock(string nodeId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Set<NodeLock>()
                .AsNoTracking()
                .FirstOrDefault(n => n.NodeId == nodeId);
        }

        private NodeLock? GetNodeLock(Guid agvCorrelationId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Set<NodeLock>()
                .AsNoTracking()
                .FirstOrDefault(n => n.AgvCorrelationId == agvCorrelationId);
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
                await dbContext.AddAsync(new NodeLock(nodeId, nodeState, false, agvCorrelationId, DateTime.Now));
            }
            else if (currentLock.NodeState != nodeState)
            {
                dbContext.Update(new NodeLock(nodeId, nodeState, currentLock.IsLocked, agvCorrelationId, DateTime.Now));
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
                await dbContext.AddAsync(new NodeLock(nodeId, NodeLockState.Free, isLocked, null, DateTime.Now));
            }
            else if (currentLock.IsLocked != isLocked)
            {
                dbContext.Update(new NodeLock(nodeId, currentLock.NodeState, isLocked, currentLock.AgvCorrelationId, DateTime.Now));
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
}