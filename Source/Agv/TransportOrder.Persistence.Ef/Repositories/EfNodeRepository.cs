using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfNodeRepository : INodeRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <inheritdoc cref="INodeRepository"/>
    public EfNodeRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TransportOrderDbContext> CreateDbContextAsync()
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    /// <inheritdoc/>
    public async Task<ICollection<NodeLock>> GetNodeStatesForMapAsync(Guid mapCorrelationId)
    {
        await using var context = await CreateDbContextAsync();

        List<string> nodeIds = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Where(m => m.CorrelationId == mapCorrelationId)
            .SelectMany(g => g.Nodes)
            .Select(n => n.Id)
            .Distinct()
            .ToListAsync();

        if (!nodeIds.Any())
        {
            throw new MapNotFoundException(mapCorrelationId);
        }

        List<NodeLock> nodeStates = await context.Set<NodeLock>()
            .Where(nl => nodeIds.Contains(nl.NodeId))
            .ToListAsync();

        // Add state for nodes without an existing lock
        nodeStates.AddRange(nodeIds.Except(nodeStates.Select(n => n.NodeId))
            .Select(id => new NodeLock(id, NodeLockState.Free, false, null, DateTime.MinValue)));
        return nodeStates;
    }
}