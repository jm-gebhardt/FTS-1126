using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository for AGV types.
/// </summary>
public class EfAgvTypeRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IAgvTypeRepository
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string agvTypeIdentifier)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<AgvType>().AsNoTracking().AnyAsync(a => a.AgvTypeIdentifier == agvTypeIdentifier);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(AgvType agvType)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        DbSet<AgvType> set = dbContext.Set<AgvType>();
        var existing = await set.SingleOrDefaultAsync(a => a.AgvTypeIdentifier == agvType.AgvTypeIdentifier);

        if (existing is null)
        {
            set.Add(agvType);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(agvType with { Id = existing.Id });
        }

        await dbContext.SaveChangesAsync();
    }
}