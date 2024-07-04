using Gebhardt.Acc.Common.TriggerDefinition;
using Gebhardt.Acc.TriggerService.Domain.Repositories;
using Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Gebhardt.Acc.TriggerService.Domain.Models;
using Gebhardt.Acc.TriggerService.Domain.Exceptions;

namespace Gebhardt.Acc.TriggerService.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITriggerRepository"/>>
public class EfTriggerRepository(IDbContextFactory<TriggerServiceDbContext> dbContextFactory) : ITriggerRepository
{
    private readonly ConcurrentDictionary<Guid, ITrigger> _triggers = new();

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<bool> TryAdd(Guid id, ITrigger trigger)
    {
        if (!await LoadTriggerFromDb(id, trigger))
        {
            await AddOrUpdateInDb(id, trigger);
        }

        return _triggers.TryAdd(id, trigger);
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<ITrigger> GetById(Guid id)
    {
        if (!_triggers.TryGetValue(id, out var trigger))
        {
            throw new TriggerNotFoundException(id.ToString());
        }
        await LoadTriggerFromDb(id, trigger);
        
        return trigger;
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task UpdateTrigger(Guid id, ITrigger trigger)
    {
        await AddOrUpdateInDb(id, trigger);

        if (!_triggers.TryGetValue(id, out var oldTrigger))
        {
            _triggers.TryAdd(id, trigger);
        }
        _triggers.TryUpdate(id, trigger, oldTrigger?? trigger);
    }

    private async Task<bool> LoadTriggerFromDb(Guid id, ITrigger trigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var storedTrigger = await context.Set<Trigger>().SingleOrDefaultAsync(t => t.Id == id);
        if (storedTrigger != null)
        {
            foreach (var parameter in storedTrigger.ConfigurationParameters)
            {
                trigger.UpdateConfigurationParameter(parameter.Name, parameter.Value);
            }
            return true;
        }
        return false;
    }

    private async Task AddOrUpdateInDb(Guid id, ITrigger trigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var storedTrigger = await context.Set<Trigger>().SingleOrDefaultAsync(t => t.Id == id);
        if (storedTrigger != null)
        {
            storedTrigger.UpdateParameters(trigger.GetConfigurationParameters().Values);
            context.Set<Trigger>().Update(storedTrigger);
        }
        else
        {
            storedTrigger = new Trigger(id, trigger.GetConfigurationParameters().Values);
            context.Set<Trigger>().Add(storedTrigger);
        }
        await context.SaveChangesAsync();
    }
}