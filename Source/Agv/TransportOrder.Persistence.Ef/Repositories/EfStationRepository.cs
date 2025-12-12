using AutoMapper;
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
                if (station.MapName == null)
                {
                    station.SetMap(map?.MapId);
                }
                context.Update(station).CurrentValues.SetValues(station);
            }

            IEnumerable<Station> newStations = newNodes.ToList().Select(n =>
            {
                var station = new Station(n.Name, n.GetPrimaryType(), n.GetAdditionalTypes());
                station.SetSize(stationSize.maxWidth + Padding, stationSize.maxLength + Padding);
                station.SetMap(map?.MapId);
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
        var stationWithNode = await context.Set<Station>()
            .Where(s => s.Id == id)
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
                x => x.nl != null ? x.nl.NodeId : (Guid?)null,
                g => g.Id,
                (x, graphNodes) => new { x.s, x.nl, graphNodes }
            )
            .SelectMany(
                x => x.graphNodes.DefaultIfEmpty(),
                (x, g) => new { Station = x.s, NodeLock = x.nl, GraphNode = g }
            )
            .SingleOrDefaultAsync();

        if (stationWithNode == null)
        {
            throw new NodeNotFoundException(id, "Station not found");
        }

        var station = stationWithNode.Station;
        station.NodeLock = stationWithNode.NodeLock == null ?
            null :
            new NodeLock(mapper.Map<GraphNode>(stationWithNode.GraphNode), stationWithNode.NodeLock.NodeState, stationWithNode.NodeLock.IsLocked,
                stationWithNode.NodeLock.AgvCorrelationId, stationWithNode.NodeLock.ModifiedDate);

        station.Agvs = await context.Set<ActionGroup>()
            .Where(a => a.Actions.Any(ac => ac.Target.NodeId!.Value == station.Id && ac.State == ActionState.Started))
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
    public async Task<ICollection<Station>> GetStations(string? mapName = null)
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
                x => x.nl != null ? x.nl.NodeId : (Guid?)null,
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

        if (mapName != null)
        {
            query = query.Where(x => x.Station.MapName == mapName);
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

    /// <inheritdoc cref="IStationRepository"/>
    public async Task Update(Station station)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(station);
        await context.SaveChangesAsync();
    }
}