using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
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
    public async Task<ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser)>> GetNodeStatesForMapAsync(Guid mapCorrelationId)
    {
        await using var context = await CreateDbContextAsync();

        List<EfNode> nodes = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Where(m => m.CorrelationId == mapCorrelationId)
            .SelectMany(x => x.Nodes)
            .ToListAsync();

        List<string> nodeIds = nodes.Select(n => n.Id).ToList();

        List<Station> stations = await context.Set<Station>()
            .Where(x => nodeIds.Contains(x.Name))
            .ToListAsync();

        List<NodeLock> nodeLocks = await context.Set<NodeLock>()
            .Where(x => nodeIds.Contains(x.NodeId))
            .ToListAsync();

        var currentTime = DateTime.MinValue;  
        var result = (from n in nodes
                      join s in stations
                      on n.Id equals s.Name into stations_join
                      from sj in stations_join.DefaultIfEmpty()
                      join nl in nodeLocks 
                      on n.Id equals nl.NodeId into nl_join
                      from nj in nl_join.DefaultIfEmpty()
                      select new 
                      {
                          Node = n,
                          Station = sj,
                          NodeLock = nj ?? new NodeLock(n.Id, NodeLockState.Free, false, null, currentTime)
                      })
                      .ToList();

        return result.Select(x => (x.NodeLock, x.Station?.Id, x.Station?.LockReason, x.Station?.LockDescription, x.Station?.LockedBy)).ToList();
    }
}