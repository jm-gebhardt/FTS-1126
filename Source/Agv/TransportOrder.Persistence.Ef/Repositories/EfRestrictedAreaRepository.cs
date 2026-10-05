using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IRestrictedAreaRepository" />
public class EfRestrictedAreaRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IRestrictedAreaRepository
{
    /// <inheritdoc />
    public async Task<bool> IsPointInRestrictedArea(float x, float y)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var restrictedAreas = await context.Set<RestrictedArea>()
            .Include(a => a.Area.OrderBy(p => p.Index))
            .AsNoTracking()
            .ToListAsync();
        return restrictedAreas.Any(a => a.IsPositionInside(x, y));
    }

    /// <inheritdoc />
    public async Task AddRestrictedArea(RestrictedArea restrictedArea)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<RestrictedArea>().Add(restrictedArea);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateRestrictedArea(RestrictedArea restrictedArea)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var restrictedAreaToUpdate = await context.Set<RestrictedArea>()
            .Include(a => a.Area.OrderBy(p => p.Index))
            .SingleOrDefaultAsync(a => a.Id == restrictedArea.Id);
        if (restrictedAreaToUpdate == null)
        {
            throw new RestrictedAreaNotFoundException(restrictedArea.Id);
        }
        if (!restrictedAreaToUpdate.Area.Equals(restrictedArea.Area))
        {
            var oldArePoints = restrictedAreaToUpdate.Area.Select(a => a.Id).ToList();
            var newArePoints = restrictedArea.Area.Select(a => a.Id).ToList();

            var areaPointsToAdd = restrictedArea.Area.Where(a => !oldArePoints.Contains(a.Id)).ToList();
            var areaPointsToRemove = restrictedAreaToUpdate.Area.Where(a => !newArePoints.Contains(a.Id)).ToList();
            context.Set<RestrictedAreaPolygonPoint>().RemoveRange(areaPointsToRemove);
            await context.Set<RestrictedAreaPolygonPoint>().AddRangeAsync(areaPointsToAdd);
        }
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<RestrictedArea> GetRestrictedArea(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var restrictedArea = await context.Set<RestrictedArea>()
            .Include(a => a.Area.OrderBy(p => p.Index))
            .SingleOrDefaultAsync(a => a.Id == id);

        return restrictedArea ?? throw new RestrictedAreaNotFoundException(id);
    }

    /// <inheritdoc />
    public async Task<ICollection<RestrictedArea>> GetRestrictedAreas(Guid[] ids)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var restrictedAreas = context.Set<RestrictedArea>()
            .Include(a => a.Area.OrderBy(p => p.Index));
        if (ids.IsNullOrEmpty())
        {
            return await restrictedAreas.ToListAsync();
        }

        // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
        IEnumerable<Guid> areaIds = ids;
        var foundRestrictedAreas = await restrictedAreas.Where(r => areaIds.Contains(r.Id)).ToListAsync();
        var notFoundRestrictedAreas = ids.Except(foundRestrictedAreas.Select(r => r.Id).ToList()).ToList();
        if (notFoundRestrictedAreas.Count > 0)
        {
            throw new RestrictedAreaNotFoundException(notFoundRestrictedAreas);
        }
        return foundRestrictedAreas;
    }

    /// <inheritdoc />
    public async Task DeleteRestrictedArea(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var restrictedArea = await context.Set<RestrictedArea>()
            .Include(a => a.Area)
            .SingleOrDefaultAsync(a => a.Id == id);
        if (restrictedArea == null)
        {
            throw new RestrictedAreaNotFoundException(id);
        }

        context.Set<RestrictedArea>().Remove(restrictedArea);
        await context.SaveChangesAsync();
    }
}