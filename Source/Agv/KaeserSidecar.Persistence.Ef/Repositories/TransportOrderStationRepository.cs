using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderStationRepository"/>>
public class TransportOrderStationRepository : TransportOrder.Persistence.Ef.Repositories.EfStationRepository, ITransportOrderStationRepository
{
    private const float Tolerance = 1e-6f;

    /// <inheritdoc cref="ITransportOrderStationRepository"/>>
    public TransportOrderStationRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : base(dbContextFactory, new SemaphoreProvider<IGraphRepositorySemaphore>())
    {
        ContextFactory = dbContextFactory;
    }

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    /// <inheritdoc cref="ITransportOrderStationRepository"/>>
    public async Task<ICollection<Station>> GetStationsByNames(string[] stationNames)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<Station>().Where(s => stationNames.Contains(s.Name)).ToListAsync();
    }

    /// <inheritdoc cref="ITransportOrderStationRepository"/>>
    public async Task<string?> GetStationAtLocationOfNode(string nodeId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        DbSet<EfNode> graphNodes = context.Set<EfNode>();
        return (await graphNodes
            .Where(n => graphNodes
                .Where(g => g.Id == nodeId)
                .Any(g =>
                    n.X >= g.X - Tolerance && n.X <= g.X + Tolerance &&
                    n.Y >= g.Y - Tolerance && n.Y <= g.Y + Tolerance &&
                    n.Z >= g.Z - Tolerance && n.Z <= g.Z + Tolerance
                ))
            .ToListAsync())
            .Where(n => n.Type.ToString() != nameof(AgvGraph.NodeType.WayPoint))
            .Select(n => n.Id)
            .FirstOrDefault();
    }
}