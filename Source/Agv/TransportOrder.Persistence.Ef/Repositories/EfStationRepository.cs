using AutoMapper;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
[CanBeCaptiveDependency]
public class EfStationRepository(
    IDbContextFactory<TransportOrderDbContext> dbContextFactory,
    ISemaphoreProvider<IGraphRepositorySemaphore> semaphoreProvider,
    IMapper mapper
) : IStationRepository
{
    // additional padding for the stations
    private const double Padding = 0.2;

    /// <inheritdoc cref="IStationRepository"/>>
    public Task AddStations(AgvGraph agvGraph)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            (double maxWidth, double maxLength) stationSize = await GetLargestAgvDimensionAsync(context);

            List<GraphNode> nodeStations = agvGraph.Nodes
                .Where(efNode => efNode.IsStation())
                .ToList();

            List<Station> existingStations = await context.Set<Station>()
                .Where(s => nodeStations.Select(n => n.Id).Contains(s.Id))
                .ToListAsync();
            List<GraphNode> newNodes = nodeStations.Where(efNode => !existingStations.Select(s => s.Id).Contains(efNode.Id)).ToList();

            var map = (await context.Set<Domain.Models.Map.Map>()
                    .Include(g => g.Nodes).ToListAsync())
                .Where(m => newNodes.Select(ns => ns.Id).All(s => m.Nodes.Select(n => n.Id).Contains(s)))
                .OrderBy(m => m.ModificationDate)
                .FirstOrDefault();

            foreach (var station in existingStations)
            {
                station.SetSize(stationSize.maxWidth + Padding, stationSize.maxLength + Padding);
                if (station.MapId == null)
                {
                    station.SetMap(map?.CorrelationId);
                }
                context.Update(station).CurrentValues.SetValues(station);
            }

            IEnumerable<Station> newStations = newNodes.ToList().Select(n =>
            {
                var station = new Station(n.Name, n.GetPrimaryType(), n.GetAdditionalTypes());
                station.SetSize(stationSize.maxWidth + Padding, stationSize.maxLength + Padding);
                station.SetMap(map?.CorrelationId);
                return station;
            });

            context.AddRange(newStations.ToList());

            await context.SaveChangesAsync();
        });
    }

    private async Task<(double, double)> GetLargestAgvDimensionAsync(TransportOrderDbContext context)
    {
        IQueryable<AgvData> agvData = context.Set<AgvData>().AsNoTracking();
        double maxWidth = 0.0;
        double maxLength = 0.0;
        if (await agvData.AnyAsync())
        {
            var agvDimension = new
            {
                MaxWidth = agvData.Max(a => a.Width),
                MaxLength = agvData.Max(a => a.Length)
            };

            maxWidth = agvDimension.MaxWidth;
            maxLength = agvDimension.MaxLength;
        }

        return (maxWidth, maxLength);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station> GetStation(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var station = await context.Set<Station>()
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id);

        if (station == null)
        {
            throw new NodeNotFoundException(id, "Station not found");
        }

        var nodeLock = await context.Set<NodeLock>()
            .AsNoTracking()
            .Where(nl => nl.NodeId == id)
            .Join(context.Set<GraphNode>().AsNoTracking(),
                nl => nl.NodeId,
                g => g.Id,
                (nl, g) => new
                {
                    NodeLock = nl,
                    GraphNode = g
                })
            .FirstOrDefaultAsync();

        if (nodeLock != null)
        {
            station.NodeLock = new NodeLock(
                mapper.Map<GraphNode>(nodeLock.GraphNode),
                nodeLock.NodeLock.NodeState,
                nodeLock.NodeLock.IsLocked,
                nodeLock.NodeLock.AgvCorrelationId,
                nodeLock.NodeLock.ModifiedDate);
        }

        station.Agvs = await context.Set<ActionGroup>()
            .Where(a => a.Actions.Any(ac =>
                ac.Target.NodeId!.Value == station.Id &&
                ac.State == ActionState.Started))
            .Select(a => a.AssignedAgv.GetValueOrDefault())
            .ToListAsync();

        return station;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station?> GetRequiredStationBaseInformation(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.Set<Station>()
            .SingleOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<ICollection<Station>> GetStations(Guid? mapId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var query = context.Set<Station>()
            .AsNoTracking()
            .GroupJoin(
                context.Set<NodeLock>().AsNoTracking(),
                s => s.Id,
                nl => nl.NodeId,
                (s, nodeLocks) => new { s, nodeLocks }
            )
            .SelectMany(
                x => x.nodeLocks.DefaultIfEmpty(),
                (x, nl) => new { x.s, nl }
            )
            .GroupJoin(
                context.Set<GraphNode>().AsNoTracking(),
                x => x.nl != null ? x.nl.NodeId : (Guid?) null,
                g => g.Id,
                (x, graphNodes) => new { x.s, x.nl, graphNodes }
            )
            .SelectMany(
                x => x.graphNodes.DefaultIfEmpty(),
                (x, g) => new
                {
                    Station = x.s,
                    NodeLock = x.nl,
                    GraphNode = g,
                    // ReSharper disable once AccessToDisposedClosure
                    Agvs = context.Set<ActionGroup>()
                        .Where(a => a.Actions.Any(ac => ac.Target.NodeId!.Value == x.s.Id && ac.State == ActionState.Started))
                        .Select(a => a.AssignedAgv.GetValueOrDefault())
                        .Distinct()
                        .ToList()
                }
            );

        if (mapId != null)
        {
            query = query.Where(x => x.Station.MapId == mapId);
        }

        var rows = await query.ToListAsync();

        List<Station> stations = rows.Select(r =>
        {
            var station = r.Station;

            station.NodeLock = r.NodeLock == null ?
                null :
                new NodeLock(mapper.Map<GraphNode>(r.GraphNode), r.NodeLock.NodeState, r.NodeLock.IsLocked, r.NodeLock.AgvCorrelationId, r.NodeLock.ModifiedDate);

            station.Agvs = r.Agvs;
            return station;
        }).ToList();

        return stations;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<ICollection<Station>> GetStationsById(ICollection<Guid> ids)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        List<Guid> idsList = ids.ToList();

        List<Station> stations = await context.Set<Station>()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        return stations
            .OrderBy(s => idsList.IndexOf(s.Id))
            .ToList();
    }

    /// <inheritdoc cref="IStationRepository"/>
    public async Task Update(Station station)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(station);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IStationRepository"/>
    public async Task<bool> IsStationGroupOccupied(Guid stationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var stationWithLock = await context.Set<Station>()
            .GroupJoin(
                context.Set<NodeLock>(),
                s => s.Id,
                nl => nl.NodeId,
                (s, nodeLocks) => new { Station = s, NodeLock = nodeLocks.FirstOrDefault() })
            .Where(x => x.Station.Id == stationId)
            .SingleOrDefaultAsync();

        if (stationWithLock == null)
        {
            return false;
        }

        var station = stationWithLock.Station;
        if (station.StationGroup.IsNullOrEmpty())
        {
            // no station group, just single station
            return false;
        }

        var nodeLock = stationWithLock.NodeLock;

        if (nodeLock is { NodeState: NodeLockState.Reserved or NodeLockState.Occupied })
        {
            return true;
        }

        var stationsInGroup = await context.Set<Station>()
            .GroupJoin(
                context.Set<NodeLock>(),
                s => s.Id,
                nl => nl.NodeId,
                (s, nodeLocks) => new { Station = s, NodeLock = nodeLocks.FirstOrDefault() })
            .Where(x => station.StationGroup.Contains(x.Station.Id))
            .ToListAsync();

        return stationsInGroup.Any(sg => sg.NodeLock is { NodeState: NodeLockState.Reserved or NodeLockState.Occupied });
    }
}