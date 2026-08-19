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
        var existingZone = await context.Set<Zone>()
            .Include(z => z.Polygon)
            .AsNoTracking()
            .SingleOrDefaultAsync(z => z.CorrelationId == zone.CorrelationId);
        if (existingZone == null)
        {
            await context.Set<Zone>().AddAsync(zone);
            await context.SaveChangesAsync();
            return;
        }
        if (existingZone.Equals(zone))
        {
            return;
        }
        context.Set<Zone>().Update(zone);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IZoneRepository"/>>
    public async Task<ICollection<Zone>> GetZones(string[] zoneIds, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
        IEnumerable<string> ids = zoneIds;
        return await context.Set<Zone>()
            .Include(z => z.Polygon.OrderBy(p => p.Index))
            .Where(z => ids.Contains(z.ZoneName)).ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="IZoneRepository"/>>
    public async Task<ICollection<Zone>> GetZones(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Zone>()
            .Include(z => z.Polygon.OrderBy(p => p.Index))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}