using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfBackgroundEditorRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : IBackgroundEditorRepository
{
    /// <inheritdoc/>
    public async Task AddOrUpdateBackgroundAsync(Background background)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var existingBackground = context.Set<Background>().FirstOrDefault(b => b.Name == background.Name && b.MapId == background.MapId);
        if (existingBackground != null)
        {
            context.Remove(existingBackground);
        }
        await context.AddAsync(background);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<Background> GetRequiredBackgroundAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var background = await context.Set<Background>().Where(b => b.Id == id).FirstOrDefaultAsync();
        return background ?? throw new BackgroundNotFoundException(id);
    }

    /// <inheritdoc/>
    public async Task DeleteBackgroundAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var background = await context.Set<Background>().Where(b => b.Id == id).FirstOrDefaultAsync();
        if (background == null)
        {
            throw new BackgroundNotFoundException(id);
        }
        context.Remove(background);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<Background>> GetBackgroundsAsync(ICollection<Guid> mapIds, int maxCount = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (mapIds.Count > 0)
        {
            return await context.Set<Background>().Where(b => b.MapId != null && mapIds.Contains(b.MapId!.Value)).Take(maxCount).ToListAsync();
        }
        return await context.Set<Background>().Take(maxCount).ToListAsync();
    }
}