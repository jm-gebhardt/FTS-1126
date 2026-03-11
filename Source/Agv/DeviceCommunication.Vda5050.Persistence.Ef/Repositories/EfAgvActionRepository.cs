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
        DbSet<AgvAction> dbSet = context.Set<AgvAction>();
        List<AgvAction> existingActionsForAgv = await dbSet.Where(a => a.AgvSerialNumber == agvSerialNumber).ToListAsync();
        List<AgvAction> handledActions = [];
        foreach (var existingAction in existingActionsForAgv)
        {
            var action = actions.SingleOrDefault(a => a.ActionType == existingAction.ActionType && a.AgvSerialNumber == existingAction.AgvSerialNumber);
            if (action == null)
            {
                context.Remove(existingAction);
                continue;
            }
            context.Update(action);
            handledActions.Add(action);
        }

        IEnumerable<AgvAction> newActions = actions.Except(handledActions);
        await context.AddRangeAsync(newActions);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvAction>> GetActionsForScopeAsync(string agvSerialNumber, ActionScope scope)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvAction>().AsNoTracking().Where(a => a.AgvSerialNumber == agvSerialNumber && a.ActionScopes.Contains(scope)).ToListAsync();
    }
}