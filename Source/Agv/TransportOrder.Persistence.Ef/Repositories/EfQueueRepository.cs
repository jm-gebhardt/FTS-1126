using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IQueueRepository"/>>
public class EfQueueRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IQueueRepository
{
    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task AddQueue(Queue queue)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(queue);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IQueueRepository"/>>
    public async Task<ICollection<Queue>> GetQueues()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<Queue>()
            .AsSplitQuery().ToListAsync();
    }
}