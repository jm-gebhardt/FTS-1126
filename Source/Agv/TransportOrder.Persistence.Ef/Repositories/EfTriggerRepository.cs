using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
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
    private readonly IConcurrentDictionaryProvider<Guid, Location> _triggerLocationDictionaryProvider;
    private readonly ILogger<EfTriggerRepository> _logger;

    /// <summary>
    /// Initializes a new <see cref="EfTriggerRepository"/> instance.
    /// </summary>
    /// <param name="dbContextFactory">Database context factory.</param>
    /// <param name="triggerLocationDictionaryProvider">The provider of the location to polygon in memory dictionary.</param>
    /// <param name="logger">The logger.</param>
    public EfTriggerRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<Guid, Location> triggerLocationDictionaryProvider, ILogger<EfTriggerRepository> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _triggerLocationDictionaryProvider = triggerLocationDictionaryProvider;
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
                    RemoveTriggerFromLocationDictionary(location);
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
            } else if (triggerToUpdate.Locations.Count > 0 && triggerToUpdate.Locations.Count == trigger.Locations.Count)
            {
                _logger.LogInformation("Update(trigger):  Location count {LocCount}.", trigger.Locations.Count);
                ICollection<Location> locationsToUpdate = triggerToUpdate.Locations;
                ICollection<Location> newLocations = trigger.Locations;
                bool updateLocations = !locationsToUpdate.SequenceEqual(newLocations);

                if (updateLocations)
                {
                    context.Set<Location>().RemoveRange(triggerToUpdate.Locations);
                    await context.Set<Location>().AddRangeAsync(trigger.Locations);
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

    private void RemoveTriggerFromLocationDictionary(Location location)
    {
        _ = _triggerLocationDictionaryProvider.ConcurrentDict.TryRemove(location.Id, out _);
    }

    private void AddTriggerToLocationDictionary(Trigger trigger, Location location)
    {
        _triggerLocationDictionaryProvider.ConcurrentDict.AddOrUpdate(location.Id, location, (_, _) => location);
        _logger.LogDebug("{Method}: Added trigger {TriggerId} to location {LocationId} (NodeId: {NodeId}, EdgeId: {EdgeId}, Polygon: {IsPolygon}, Number of AGVs: {AgvCount})", 
            nameof(AddTriggerToLocationDictionary), trigger.Id, location.Id, location.NodeId, location.EdgeId, location.Polygon.Count > 0, location.Agvs.Count);
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public ICollection<Location> GetNodeTriggerLocations(string nodeId)
    {
        return _triggerLocationDictionaryProvider.ConcurrentDict.Values.Where(l => l.NodeId == nodeId).ToList();
    }

    /// <inheritdoc  cref="ITriggerRepository"/>>
    public async Task<ICollection<Location>> GetPolygonTriggerLocations(Guid agvId, AgvPosition agvPosition)
    {
        ICollection<Location> locationsWithPolygon = _triggerLocationDictionaryProvider.ConcurrentDict.Values.Where(l => l.Polygon.Count > 0).ToList();
        List<Location> locationsWithTriggerToCall = [];

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        foreach (var location in locationsWithPolygon)
        {
            bool pointIsInPolygon = location.Polygon.PointIsInPolygon(agvPosition.X, agvPosition.Y);
            if (pointIsInPolygon)
            {
                if (!location.Agvs.Contains(agvId))
                {
                    location.Agvs.Add(agvId);
                    locationsWithTriggerToCall.Add(location);
                    _logger.LogDebug("{Method}: AGV {AgvId} entered polygon", nameof(GetPolygonTriggerLocations), agvId);

                    var locationRecord = await context.Set<Location>().Where(l => l.Id == location.Id).FirstOrDefaultAsync();
                    if (locationRecord != null)
                    {
                        locationRecord.Agvs.Add(agvId);
                    }

                    int count = await context.SaveChangesAsync();
                    _logger.LogTrace("{Method}: locationRecord.Agvs: {Agvs}. Saved {Count} records!", nameof(GetPolygonTriggerLocations), locationRecord?.Agvs.Count, count);
                }
                else
                {
                    _logger.LogTrace("{Method}: AGV {AgvId} is within polygon but already registered", nameof(GetPolygonTriggerLocations), agvId);
                }
            }
            else
            {
                if (location.Agvs.Contains(agvId))
                {
                    location.Agvs.Remove(agvId);
                    _logger.LogDebug("{Method}: AGV {AgvId} left polygon.", nameof(GetPolygonTriggerLocations), agvId);

                    var locationRecord = await context.Set<Location>().Where(l => l.Id == location.Id).FirstOrDefaultAsync();
                    if (locationRecord != null)
                    {
                        locationRecord.Agvs.Remove(agvId);
                        _logger.LogTrace("{Method}: After remove agvGuidString is {AgvGuidString}", nameof(GetPolygonTriggerLocations), string.Join(',', locationRecord.Agvs));
                    }
                    int count =  await context.SaveChangesAsync();
                   _logger.LogTrace("{Method}: Saved {Count} records!", nameof(GetPolygonTriggerLocations), count);
                }
            }
        }
        return locationsWithTriggerToCall;
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task LoadTriggerToLocationDictionary()
    {
        _triggerLocationDictionaryProvider.ConcurrentDict.Clear();

        ICollection<Trigger> triggers = await GetTriggers();
        foreach (var trigger in triggers)
        {
            if (trigger.Locations.Count <= 0)
            {
                continue;
            }
            foreach (var location in trigger.Locations)
            {
                AddTriggerToLocationDictionary(trigger, location);
            }
        }
    }
}