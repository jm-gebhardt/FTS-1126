using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfAgvDataRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IAgvDataRepository
{
    /// <inheritdoc/>
    public async Task<AgvData?> GetAgvDataAsync(Guid correlationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvData>().AsNoTracking().SingleOrDefaultAsync(q => q.AgvCorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(AgvData agvData)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<AgvData> dbSet = context.Set<AgvData>();

        if (dbSet.Any(a => a.AgvCorrelationId == agvData.AgvCorrelationId))
        {
            dbSet.Update(agvData);
        }
        else
        {
            dbSet.Add(agvData);
        }

        await context.SaveChangesAsync();
    }
}