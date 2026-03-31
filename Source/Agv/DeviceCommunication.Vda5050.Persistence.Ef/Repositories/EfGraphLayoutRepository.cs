using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfGraphLayoutRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IGraphLayoutRepository
{
    /// <inheritdoc/>
    public async Task StoreGraphLayoutsAsync(ICollection<GraphLayout> layouts)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        foreach (var layout in layouts)
        {
            var existing = await context.Set<GraphLayout>()
                .SingleOrDefaultAsync(e => e.LayoutId == layout.LayoutId && e.LayoutVersion == layout.LayoutVersion);

            if (existing != null)
            {
                var merged = BuildMergedLayout(existing, layout);

                context.Set<GraphLayout>().Remove(existing);
                await context.SaveChangesAsync();

                context.Set<GraphLayout>().Add(merged);
            }
            else
            {
                context.Set<GraphLayout>().Add(layout);
            }
        }

        await context.SaveChangesAsync();
    }

    private static GraphLayout BuildMergedLayout(GraphLayout existing, GraphLayout incoming)
    {
        Dictionary<Guid, GraphLayoutNode> nodeMap = existing.Nodes.ToDictionary(n => n.Id);
        foreach (var incomingNode in incoming.Nodes)
        {
            nodeMap[incomingNode.Id] = incomingNode;
        }

        Dictionary<Guid, GraphLayoutEdge> edgeMap = existing.Edges.ToDictionary(e => e.Id);
        foreach (var incomingEdge in incoming.Edges)
        {
            edgeMap[incomingEdge.Id] = incomingEdge;
        }

        Dictionary<string, GraphLayoutStation> stationMap = existing.Stations.ToDictionary(s => s.StationId);
        foreach (var incomingStation in incoming.Stations)
        {
            stationMap[incomingStation.StationId] = incomingStation;
        }

        return new GraphLayout
        {
            Id = existing.Id,
            LayoutId = existing.LayoutId,
            LayoutVersion = existing.LayoutVersion,
            LayoutName = incoming.LayoutName,
            StoredAt = incoming.StoredAt,
            Nodes = nodeMap.Values.ToList(),
            Edges = edgeMap.Values.ToList(),
            Stations = stationMap.Values.ToList()
        };
    }

    /// <inheritdoc/>
    public async Task<GraphLayout?> GetLatestLayoutAsync(string layoutId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.Set<GraphLayout>()
            .Where(e => e.LayoutId == layoutId)
            .OrderByDescending(e => e.StoredAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<GraphLayout>> GetAllLatestLayoutsAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.Set<GraphLayout>()
            .GroupBy(e => e.LayoutId)
            .Select(g => g.OrderByDescending(e => e.StoredAt).First())
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> HasVehicleTypeAsync(string vehicleTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        bool inNodes = await context.Set<GraphLayoutNode>()
            .AnyAsync(n => n.VehicleTypeProperties.Any(vp => vp.VehicleTypeId == vehicleTypeId));

        if (inNodes)
        {
            return true;
        }

        return await context.Set<GraphLayoutEdge>()
            .AnyAsync(e => e.VehicleTypeProperties.Any(vp => vp.VehicleTypeId == vehicleTypeId));
    }

    /// <inheritdoc/>
    public async Task<HashSet<string>> GetStoredVehicleTypeIdsAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        List<string> fromNodes = await context.Set<GraphLayoutNode>()
            .SelectMany(n => n.VehicleTypeProperties)
            .Select(vp => vp.VehicleTypeId)
            .Distinct()
            .ToListAsync();

        List<string> fromEdges = await context.Set<GraphLayoutEdge>()
            .SelectMany(e => e.VehicleTypeProperties)
            .Select(vp => vp.VehicleTypeId)
            .Distinct()
            .ToListAsync();

        return fromNodes.Concat(fromEdges).ToHashSet();
    }

    /// <inheritdoc/>
    public async Task<ICollection<GraphLayout>> GetLayoutsForVehicleTypeAsync(string vehicleTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        List<Guid> latestIds = await context.Set<GraphLayout>()
            .GroupBy(e => e.LayoutId)
            .Select(g => g.OrderByDescending(e => e.StoredAt).First().Id)
            .ToListAsync();

        return await context.Set<GraphLayout>()
            .Where(e => latestIds.Contains(e.Id))
            .Where(e =>
                e.Nodes.Any(n => n.VehicleTypeProperties.Any(vp => vp.VehicleTypeId == vehicleTypeId)) ||
                e.Edges.Any(ed => ed.VehicleTypeProperties.Any(vp => vp.VehicleTypeId == vehicleTypeId)))
            .ToListAsync();
    }
}