using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="WaitingPointAction"/>>
[CanBeCaptiveDependency]
public class EfWaitingPointActionRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<WaitingPointAction, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory), IWaitingPointActionRepository
{
    /// <inheritdoc cref="WaitingPointAction"/>>
    public async Task RemoveById(Guid actionCorrelationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var action = await context.Set<WaitingPointAction>().SingleOrDefaultAsync(a => a.CorrelationId == actionCorrelationId);
        if (action != null)
        {
            context.Remove(action);
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc cref="WaitingPointAction"/>>
    public async Task UpdateAsync(WaitingPointAction action)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        context.Update(action);
        await context.SaveChangesAsync();
    }
}