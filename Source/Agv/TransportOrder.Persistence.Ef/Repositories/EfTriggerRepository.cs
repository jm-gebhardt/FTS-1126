using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITriggerRepository"/>>
public class EfTriggerRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : ITriggerRepository
{
    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task Add(Trigger trigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(trigger);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<ICollection<Trigger>> GetTriggers()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<Trigger> GetById(Guid triggerId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var trigger = await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon)
            .AsSplitQuery()
            .SingleOrDefaultAsync(t => t.Id == triggerId);
        if (trigger == null)
        {
            throw new TriggerNotFoundException(triggerId);
        }
        return trigger;
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task Update(Trigger trigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var triggerToUpdate = await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon)
            .AsSplitQuery()
            .SingleOrDefaultAsync(t => t.Id == trigger.Id);
        if (triggerToUpdate == null)
        {
            throw new TriggerNotFoundException(trigger.Id);
        }

        if (!triggerToUpdate.Locations.Equals(trigger.Locations))
        {
            if (triggerToUpdate.Locations.Count > trigger.Locations.Count)
            {
                List<Guid> newLocations = trigger.Locations.Select(l => l.Id).ToList();
                List<Location> locationToRemove = triggerToUpdate.Locations.Where(l => !newLocations.Contains(l.Id)).ToList(); 
                foreach (var location in locationToRemove)
                {
                    if (location.Polygon.Count > 0)
                    {
                        context.Set<PolygonPoint>().RemoveRange(location.Polygon);
                    }
                }
                context.Set<Location>().RemoveRange(locationToRemove);
            }
            if (triggerToUpdate.Locations.Count < trigger.Locations.Count)
            {
                List<Guid> oldLocations = triggerToUpdate.Locations.Select(l => l.Id).ToList();
                List<Location> locationToAdd = trigger.Locations.Where(l => !oldLocations.Contains(l.Id)).ToList();
                foreach (var location in locationToAdd)
                {
                    if (location.Polygon.Count > 0)
                    {
                        await context.Set<PolygonPoint>().AddRangeAsync(location.Polygon);
                    }
                    await context.Set<Location>().AddAsync(location);
                }
            }
        }

        if (!triggerToUpdate.ConfigurationParameters.Equals(trigger.ConfigurationParameters))
        {
            var parameter = triggerToUpdate.ConfigurationParameters.SingleOrDefault(t => t.Value != trigger.ConfigurationParameters.Single(tt => tt.Name == t.Name).Value);
            if (parameter != null)
            {
                parameter.Value = trigger.ConfigurationParameters.Single(tt => tt.Name == parameter.Name).Value;
            }
        }

        await context.SaveChangesAsync();
    }
}