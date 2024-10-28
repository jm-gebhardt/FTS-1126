using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class FactoryTimeRepository : IFactoryTimeRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    /// Constructor of the FactoryTimeRepository.
    /// </summary>
    /// <param name="dbContextFactory"></param>
    public FactoryTimeRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task<FactoryTime> GetFactoryTime()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<FactoryTime>().FirstAsync();
    }

    /// <inheritdoc />
    public async Task SetFactoryTime(TimeZoneInfo factoryTimeZone)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<FactoryTime> dbSet = context.Set<FactoryTime>();
        var factoryTime = await dbSet.FirstOrDefaultAsync();

        if (factoryTime == null)
        {
            dbSet.Add(new FactoryTime(factoryTimeZone));
        }
        else
        {
            factoryTime.FactoryTimeZone = factoryTimeZone;
        }

        await context.SaveChangesAsync();
    }
}