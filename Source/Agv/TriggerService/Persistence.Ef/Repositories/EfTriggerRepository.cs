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
    private readonly ConcurrentDictionary<Guid, IFfcTrigger> _triggers = new();

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<bool> TryAdd(Guid id, IFfcTrigger ffcTrigger)
    {
        if (!await LoadTriggerFromDb(id, ffcTrigger))
        {
            await AddOrUpdateInDb(id, ffcTrigger);
        }

        return _triggers.TryAdd(id, ffcTrigger);
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task<IFfcTrigger> GetById(Guid id)
    {
        if (!_triggers.TryGetValue(id, out var trigger))
        {
            throw new TriggerNotFoundException(id.ToString());
        }
        await LoadTriggerFromDb(id, trigger);
        
        return trigger;
    }

    /// <inheritdoc cref="ITriggerRepository"/>>
    public async Task UpdateTrigger(Guid id, IFfcTrigger ffcTrigger)
    {
        if (id != ffcTrigger.GetProperties().Id)
        {
            throw new TriggerNotFoundException(id.ToString());
        }

        await AddOrUpdateInDb(id, ffcTrigger);

        if (!_triggers.TryGetValue(id, out var oldTrigger))
        {
            _triggers.TryAdd(id, ffcTrigger);
        }
        _triggers.TryUpdate(id, ffcTrigger, oldTrigger?? ffcTrigger);
    }

    private async Task<bool> LoadTriggerFromDb(Guid id, IFfcTrigger ffcTrigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var storedTrigger = await context.Set<Trigger>().SingleOrDefaultAsync(t => t.Id == id);
        if (storedTrigger != null)
        {
            foreach (var parameter in storedTrigger.ConfigurationParameters)
            {
                ffcTrigger.UpdateDefaultConfigurationParameter(parameter.Name, parameter.Value);
            }
            return true;
        }
        return false;
    }

    private async Task AddOrUpdateInDb(Guid id, IFfcTrigger ffcTrigger)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var storedTrigger = await context.Set<Trigger>().SingleOrDefaultAsync(t => t.Id == id);
        if (storedTrigger != null)
        {
            storedTrigger.UpdateParameters(ffcTrigger.GetDefaultConfigurationParameters().Values);
            context.Set<Trigger>().Update(storedTrigger);
        }
        else
        {
            storedTrigger = new Trigger(id, ffcTrigger.GetDefaultConfigurationParameters().Values);
            context.Set<Trigger>().Add(storedTrigger);
        }
        await context.SaveChangesAsync();
    }
}