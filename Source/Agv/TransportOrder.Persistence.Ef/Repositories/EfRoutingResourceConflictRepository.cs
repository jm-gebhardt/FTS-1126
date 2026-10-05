using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IRoutingResourceConflictRepository"/>
public class EfRoutingResourceConflictRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<RoutingResourceConflict, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory),
    IRoutingResourceConflictRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResourceConflict>> GetAllAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var conflicts = await context.Set<RoutingResourceConflict>().AsNoTracking().ToListAsync();

        return conflicts.Select(c => new ResourceConflict(c.ResourceId, c.ConflictingResourceId)).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResourceConflict>> GetByResourceAsync(Guid resourceId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var conflicts = await context.Set<RoutingResourceConflict>()
            .AsNoTracking()
            .Where(c => c.ResourceId == resourceId || c.ConflictingResourceId == resourceId)
            .ToListAsync();

        return conflicts.Select(c => new ResourceConflict(c.ResourceId, c.ConflictingResourceId)).ToList();
    }

    /// <inheritdoc/>
    public async Task SetConflictsAsync(IReadOnlyCollection<ResourceConflict> conflicts)
    {
        var rows = conflicts
            .Distinct()
            .Select(c => new RoutingResourceConflict { ResourceId = c.ResourceId, ConflictingResourceId = c.ConflictingResourceId })
            .ToList();

        await using var context = await ContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await context.Set<RoutingResourceConflict>().ExecuteDeleteAsync();
        context.Set<RoutingResourceConflict>().AddRange(rows);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}