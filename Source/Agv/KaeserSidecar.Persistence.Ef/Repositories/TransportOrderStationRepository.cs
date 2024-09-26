using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderStationRepository"/>>
public class TransportOrderStationRepository: EfStationRepository, ITransportOrderStationRepository
{
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
}