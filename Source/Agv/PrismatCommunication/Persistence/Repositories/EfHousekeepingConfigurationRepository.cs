using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc/>
public class EfHousekeepingConfigurationRepository(IDbContextFactory<PrismatCommunicationDbContext> contextFactory) : IHousekeepingConfigurationRepository
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
