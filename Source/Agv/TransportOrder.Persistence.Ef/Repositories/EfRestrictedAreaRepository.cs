using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class EfRestrictedAreaRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IRestrictedAreaRepository
{
    /// <inheritdoc />
    public async Task<bool> IsPointInRestrictedArea(float x, float y)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        List<RestrictedArea> restrictedAreas = await context.Set<RestrictedArea>()
            .Include(a => a.Area)
            .AsNoTracking()
            .ToListAsync();
        return restrictedAreas.Any(a => a.IsPositionInside(x, y));
    }
}