using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
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
public class EfStationRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, ISemaphoreProvider<IGraphRepositorySemaphore> semaphoreProvider) : IStationRepository
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
                .Where(efNode => efNode.Type != AgvGraph.NodeType.WayPoint)
                .ToList();

            List<Station> existingStations = await context.Set<Station>()
                .Where(s => nodeStations.Select(n => n.Id).Contains(s.Name))
                .ToListAsync();
            List<GraphNode> newNodes = nodeStations.Where(efNode => !existingStations.Select(s => s.Name).Contains(efNode.Id)).ToList();
           
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
                var station = new Station(n.Id, n.Type, []);
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
            var agvDimension = agvData.GroupBy(a => 1)
                .Select(g => new
                {
                    MaxWidth = g.Max(a => a.Width),
                    MaxLength = g.Max(a => a.Length)
                }).First();

            maxWidth = agvDimension.MaxWidth;
            maxLength = agvDimension.MaxLength;
        }

        return (maxWidth, maxLength);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station> GetStation(Guid id)
    {
        var context = await dbContextFactory.CreateDbContextAsync();

        var result = await context.Set<Station>()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                Station = s,
                NodeLock = context.Set<NodeLock>().FirstOrDefault(n => n.NodeId == s.Name),
                Agvs = context.Set<ActionGroup>()
                    .Where(a => a.Actions.Any(ac => ac.Target.NodeId == s.Name && ac.State == ActionState.Started))
                    .Select(a => a.AssignedAgv.GetValueOrDefault())
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (result == null)
        {
            throw new NodeNotFoundException(id, "Station not found");
        }

        result.Station.NodeLock = result.NodeLock;
        result.Station.Agvs = result.Agvs;
        await context.DisposeAsync();
        return result.Station;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station> GetStation(string name)
    {
        var context = await dbContextFactory.CreateDbContextAsync();

        var result = await context.Set<Station>()
            .Where(s => s.Name == name)
            .Select(s => new
            {
                Station = s,
                NodeLock = context.Set<NodeLock>().FirstOrDefault(n => n.NodeId == s.Name),
                Agvs = context.Set<ActionGroup>()
                    .Where(a => a.Actions.Any(ac => ac.Target.NodeId == s.Name && ac.State == ActionState.Started))
                    .Select(a => a.AssignedAgv.GetValueOrDefault())
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (result == null)
        {
            throw new NodeNotFoundException(name);
        }

        result.Station.NodeLock = result.NodeLock;
        result.Station.Agvs = result.Agvs;
        await context.DisposeAsync();
        return result.Station;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station?> GetRequiredStationBaseInformation(string name)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.Set<Station>()
            .SingleOrDefaultAsync(s => s.Name == name);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<ICollection<Station>> GetStations(string? mapName = null)
    {
        var context = await dbContextFactory.CreateDbContextAsync();

        var result = context.Set<Station>()
            .AsNoTracking()
            .Select(s => new
            {
                Station = s,
                NodeLock = context.Set<NodeLock>().FirstOrDefault(n => n.NodeId == s.Name),
                Agvs = context.Set<ActionGroup>()
                    .Where(a => a.Actions.Any(ac => ac.Target.NodeId == s.Name && ac.State == ActionState.Started))
                    .Select(a => a.AssignedAgv.GetValueOrDefault())
                    .ToList()
            });

        if (mapName != null)
        {
            result = result.Where(x => x.Station.MapName == mapName);
        }

        var stations = await result.ToListAsync();
        await context.DisposeAsync();
        return stations.Select(s =>
        {
            s.Station.NodeLock = s.NodeLock;
            s.Station.Agvs = s.Agvs;
            return s.Station;
        }).ToList();
    }
    
    /// <inheritdoc cref="IStationRepository"/>
    public async Task Update(Station station)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(station);
        await context.SaveChangesAsync();
    }
}
