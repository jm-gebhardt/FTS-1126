using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITriggerRepository"/>>
public class EfTriggerRepository : ITriggerRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IConcurrentDictionaryProvider<string, List<Guid>> _nodeTriggersDictionaryProvider;
    private readonly ILogger<EfTriggerRepository> _logger;

    /// <summary>
    /// Initializes a new <see cref="EfTriggerRepository"/> instance.
    /// </summary>
    /// <param name="dbContextFactory">Database context factory.</param>
    /// <param name="concurrentDictionaryProvider">The provider of the node to trigger in memory dictionary.</param>
    /// <param name="logger">The logger.</param>
    public EfTriggerRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<string, List<Guid>> concurrentDictionaryProvider,
        ILogger<EfTriggerRepository> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _nodeTriggersDictionaryProvider = concurrentDictionaryProvider;
        _logger = logger;
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task Add(Trigger trigger)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(trigger);
        await context.SaveChangesAsync();
        if (trigger.Locations.Count > 0)
        {
            foreach (var location in trigger.Locations)
            {
                AddTriggerToLocationDictionary(trigger, location);
            }
        }
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<ICollection<Trigger>> GetTriggers()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon.OrderBy(p => p.Index))
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<Trigger> GetById(Guid triggerId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var trigger = await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon.OrderBy(p => p.Index))
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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var triggerToUpdate = await context.Set<Trigger>()
            .Include(t => t.ConfigurationParameters)
            .Include(t => t.Locations)
            .ThenInclude(t => t.Polygon.OrderBy(p => p.Index))
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
                        context.Set<TriggerPolygonPoint>().RemoveRange(location.Polygon);
                    }
                    RemoveTriggerFromLocationRepository(trigger, location);
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
                        await context.Set<TriggerPolygonPoint>().AddRangeAsync(location.Polygon);
                    }
                    AddTriggerToLocationDictionary(trigger, location);
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

    private void RemoveTriggerFromLocationRepository(Trigger trigger, Location location)
    {
        if (location.NodeId != null)
        {
            if (_nodeTriggersDictionaryProvider.ConcurrentDict.TryGetValue(location.NodeId, out List<Guid>? triggerIds))
            {
                triggerIds.Remove(location.TriggerId);
                _logger.LogDebug("{Method}: Removed trigger {TriggerId} from node {NodeId}", nameof(RemoveTriggerFromLocationRepository), trigger.Id, location.NodeId);
            }
        }
    }

    private void AddTriggerToLocationDictionary(Trigger trigger, Location location)
    {
        if (location.NodeId != null)
        {
            _nodeTriggersDictionaryProvider.ConcurrentDict.AddOrUpdate(location.NodeId,
                addValueFactory: (_) => [location.TriggerId],
                updateValueFactory: (_, oldValue) =>
                {
                    oldValue.Add(location.TriggerId);
                    return oldValue;
                });
            _logger.LogDebug("{Method}: Added trigger {TriggerId} to node {NodeId}", nameof(AddTriggerToLocationDictionary), trigger.Id, location.NodeId);
        }
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public ICollection<Guid> GetTriggersAssignedToNode(string nodeId)
    {
        return _nodeTriggersDictionaryProvider.ConcurrentDict.GetValueOrDefault(nodeId, []);
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task LoadTriggerToLocationDictionary()
    {
        _nodeTriggersDictionaryProvider.ConcurrentDict.Clear();
        ICollection<Trigger> triggers = await GetTriggers();
        foreach (var trigger in triggers)
        {
            if (trigger.Locations.Count <= 0)
            {
                continue;
            }
            foreach (var location in trigger.Locations)
            {
                if (location.NodeId != "")
                {
                    _nodeTriggersDictionaryProvider.ConcurrentDict.AddOrUpdate(location.NodeId!,
                        addValueFactory: (_) => [location.TriggerId],
                        updateValueFactory: (_, oldValue) =>
                        {
                            oldValue.Add(location.TriggerId);
                            return oldValue;
                        });
                }
            }
        }
    }
}