using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class GraphNodeRepository : INodeRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="INodeRepository"/>
    public GraphNodeRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
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

        List<GraphNode> nodes = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Where(m => m.CorrelationId == mapCorrelationId)
            .SelectMany(x => x.Nodes)
            .ToListAsync();

        List<Guid> nodeIds = nodes.Select(n => n.Id).ToList();

        List<Station> stations = await context.Set<Station>()
            .Where(x => nodeIds.Contains(x.Id))
            .ToListAsync();

        List<NodeLock> nodeLocks = await context.Set<NodeLock>()
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
                        _mapper.Map<GraphNode>(x.Node),
                        nodeLock?.NodeState ?? NodeLockState.Free,
                        nodeLock?.IsLocked ?? false,
                        nodeLock?.AgvCorrelationId,
                        nodeLock?.ModifiedDate ?? currentTime
                    )
                }
            )
            .ToList();

        return result
            .Select(x => (x.NodeLock, x.Station?.Id, x.Station?.LockReason, x.Station?.LockDescription, x.Station?.LockedBy))
            .ToList();
    }
}