using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IDecisionPointActionRepository"/>>
public class EfDecisionPointActionRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory) :
    EfRepository<DecisionPointAction, KaeserSidecarDbContext, IDbContextFactory<KaeserSidecarDbContext>>(contextFactory), IDecisionPointActionRepository
{
    /// <inheritdoc cref="IDecisionPointActionRepository"/>>
    public async Task RemoveById(Guid actionCorrelationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var action = await context.Set<DecisionPointAction>().SingleOrDefaultAsync(a => a.CorrelationId == actionCorrelationId);
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