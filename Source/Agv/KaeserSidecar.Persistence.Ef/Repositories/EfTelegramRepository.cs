using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <summary>
/// Provides a repository for telegram related queries.
/// </summary>
public class EfTelegramRepository<TTelegramType>(IDbContextFactory<KaeserSidecarDbContext> contextFactory) : ITelegramRepository
    where TTelegramType : Telegram
{
    /// <inheritdoc cref="ITelegramRepository"/>>
    public async Task<ICollection<Telegram>> GetAll()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        List<TTelegramType> telegrams = await context.Set<TTelegramType>().ToListAsync();
        return telegrams.Select(t => (Telegram)t).ToList();
    }

    /// <inheritdoc cref="ITelegramRepository"/>>
    public async Task<Telegram> GetByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Set<TTelegramType>().Where(t => t.Id == id).SingleAsync();
    }

    /// <inheritdoc cref="ITelegramRepository"/>>
    public async Task RemoveAsync(Telegram aggregate)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var telegram = await context.Set<TTelegramType>().Where(t => t.Id == aggregate.Id).SingleAsync();
        context.Remove(telegram);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="ITelegramRepository"/>>
    public async Task AddAsync(Telegram aggregate)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Set<TTelegramType>().Add((TTelegramType)aggregate);
        await context.SaveChangesAsync();
    }
}