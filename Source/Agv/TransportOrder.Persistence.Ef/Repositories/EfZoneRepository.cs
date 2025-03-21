using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IZoneRepository"/>>
public class EfZoneRepository(IDbContextFactory<TransportOrderDbContext> contextFactory): IZoneRepository
{
    /// <inheritdoc cref="IZoneRepository"/>>
    public async Task AddOrUpdateZone(Zone zone)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (await context.Set<Zone>().Include(z => z.Polygon).AsSplitQuery().AnyAsync(z => z.CorrelationId == zone.CorrelationId))
        {
            context.Set<Zone>().Update(zone);
            await context.SaveChangesAsync();
            return;
        }
        await context.Set<Zone>().AddAsync(zone);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IZoneRepository"/>>
    public async Task<ICollection<Zone>> GetZones(string[] zoneIds)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Set<Zone>()
            .Include(z => z.Polygon.OrderBy(p => p.Index))
            .Where(z => zoneIds.Contains(z.ZoneId)).ToListAsync();
    }
}