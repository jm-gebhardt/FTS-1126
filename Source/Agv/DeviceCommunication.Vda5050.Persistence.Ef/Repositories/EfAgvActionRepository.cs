using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfAgvActionRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IAgvActionRepository
{
    /// <inheritdoc/>
    public async Task AddOrUpdateActionsAsync(ICollection<AgvAction> actions, string agvSerialNumber, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DbSet<AgvAction> dbSet = context.Set<AgvAction>();
        List<AgvAction> existingActionsForAgv = await dbSet.Where(a => a.AgvSerialNumber == agvSerialNumber).ToListAsync(cancellationToken);

        Dictionary<string, AgvAction> pendingActionsByType = actions
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
        await context.AddRangeAsync(newActions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvAction>> GetActionsForScopeAsync(string agvSerialNumber, ActionScope scope, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<AgvAction> actions = await context.Set<AgvAction>().AsNoTracking().Where(a => a.AgvSerialNumber == agvSerialNumber).ToListAsync(cancellationToken);
        return actions.Where(a => a.ActionScopes.Contains(scope)).ToList();
    }
}