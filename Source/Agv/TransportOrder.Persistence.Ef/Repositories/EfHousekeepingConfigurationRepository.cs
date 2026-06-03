using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfHousekeepingConfigurationRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : IHousekeepingConfigurationRepository
{
    /// <inheritdoc/>
    public async Task<HousekeepingConfiguration?> GetConfigurationAsync(HousekeepingEntityType entityType)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Set<HousekeepingConfiguration>()
            .FirstOrDefaultAsync(c => c.EntityType == entityType);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(HousekeepingConfiguration configuration)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var existing = await context.Set<HousekeepingConfiguration>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EntityType == configuration.EntityType);

        if (existing != null)
        {
            context.Remove(existing);
        }

        await context.AddAsync(configuration);
        await context.SaveChangesAsync();
    }
}
