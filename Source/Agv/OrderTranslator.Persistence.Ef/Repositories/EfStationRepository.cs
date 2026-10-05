using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Station = Gebhardt.Acc.OrderTranslator.Domain.Models.Station.Station;

namespace Gebhardt.Acc.OrderTranslator.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
[CanBeCaptiveDependency]
public class EfStationRepository(IDbContextFactory<OrderTranslatorDbContext> contextFactory) :
    EfRepository<Station, OrderTranslatorDbContext, IDbContextFactory<OrderTranslatorDbContext>>(contextFactory), IStationRepository
{
    /// <inheritdoc cref="IStationRepository"/>
    public async Task<ICollection<Station>> GetStationsByLane(string lane)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return (await context.Set<Station>().ToListAsync())
            .Where(s => s.InputLanes.Contains(lane) || s.OutputLanes.Contains(lane)).ToList();
    }

    /// <inheritdoc cref="IStationRepository"/>
    public async Task<Station?> GetStationByName(string name)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<Station>()
            .SingleOrDefaultAsync(s => s.Name == name);
    }

    /// <inheritdoc cref="IStationRepository"/>
    public async Task AddOrUpdateAsync(Station station)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var dbSet = context.Set<Station>();

        var existingStation = await dbSet.FirstOrDefaultAsync(s => s.Name == station.Name);
        if (existingStation == null)
        {
            dbSet.Add(station);
        }
        else
        {
            context.Entry(existingStation).CurrentValues.SetValues(station);
        }

        await context.SaveChangesAsync();
    }
}