using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="WaitingPointAction"/>>
[CanBeCaptiveDependency]
public class EfWaitingPointActionRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory) :
    EfRepository<WaitingPointAction, KaeserSidecarDbContext, IDbContextFactory<KaeserSidecarDbContext>>(contextFactory), IWaitingPointActionRepository
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