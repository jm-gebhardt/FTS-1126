using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IDecisionPointActionRepository"/>>
[CanBeCaptiveDependency]
public class EfDecisionPointActionRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<DecisionPointAction, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory), IDecisionPointActionRepository
{
    /// <inheritdoc cref="IDecisionPointActionRepository"/>>
    public async Task RemoveById(Guid actionCorrelationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var action = await context.Set<DecisionPointAction>().SingleOrDefaultAsync(a => a.AgvActionCorrelationId == actionCorrelationId);
        if (action != null)
        {
            context.Remove(action);
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc cref="IDecisionPointActionRepository"/>>
    public async Task UpdateAsync(DecisionPointAction action)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        context.Update(action);
        await context.SaveChangesAsync();
    }
}