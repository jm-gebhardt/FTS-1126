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

    private static IOrderedQueryable<FactoryTime> GetOrderedFactoryTime(TransportOrderDbContext context) => context.Set<FactoryTime>().OrderBy(f => f.Id);
    
    /// <inheritdoc />
    public async Task<FactoryTime> GetFactoryTime()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await GetOrderedFactoryTime(context).FirstAsync();
    }

    /// <inheritdoc />
    public async Task SetFactoryTime(TimeZoneInfo factoryTimeZone)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var factoryTime = await GetOrderedFactoryTime(context).FirstOrDefaultAsync();

        if (factoryTime == null)
        {
            context.Set<FactoryTime>().Add(new FactoryTime(factoryTimeZone)
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000000")
            });
        }
        else
        {
            factoryTime.FactoryTimeZone = factoryTimeZone;
        }

        await context.SaveChangesAsync();
    }
}