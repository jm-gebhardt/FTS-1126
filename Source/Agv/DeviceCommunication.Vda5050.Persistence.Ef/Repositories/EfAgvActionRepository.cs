using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfAgvActionRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IAgvActionRepository
{
    /// <inheritdoc/>
    public async Task AddOrUpdateActionsAsync(ICollection<AgvAction> actions, string agvSerialNumber)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var dbSet = context.Set<AgvAction>();
        var existingActionsForAgv = await dbSet.Where(a => a.AgvSerialNumber == agvSerialNumber).ToListAsync();

        var pendingActionsByType = actions
            .GroupBy(action => action.ActionType)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var existingAction in existingActionsForAgv)
        {
            if (!pendingActionsByType.Remove(existingAction.ActionType, out var action))
            {
                context.Remove(existingAction);
                continue;
            }

            context.Entry(existingAction).CurrentValues.SetValues(action);
        }

        IEnumerable<AgvAction> newActions = pendingActionsByType.Values;
        await context.AddRangeAsync(newActions);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvAction>> GetActionsForScopeAsync(string agvSerialNumber, ActionScope scope)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var actions = await context.Set<AgvAction>().AsNoTracking().Where(a => a.AgvSerialNumber == agvSerialNumber).ToListAsync();
        return actions.Where(a => a.ActionScopes.Contains(scope)).ToList();
    }
}