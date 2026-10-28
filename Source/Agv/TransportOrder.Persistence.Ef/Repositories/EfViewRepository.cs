using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc/>
[UsedImplicitly]
public class EfViewRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : IViewRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<View>> GetViewsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Set<View>().AsNoTracking().OrderBy(v => v.Name).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<View?> FindViewAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Set<View>().AsNoTracking().SingleOrDefaultAsync(v => v.Id == id);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateViewAsync(View view, byte[]? backgroundImage = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        if (view.IsDefault)
        {
            await context.Set<View>()
                .Where(v => v.IsDefault && v.Id != view.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsDefault, false));
        }

        if (view.Background is null || backgroundImage is not null)
        {
            await context.Set<ViewBackgroundImage>().Where(i => i.ViewId == view.Id).ExecuteDeleteAsync();
        }

        if (await context.Set<View>().AnyAsync(v => v.Id == view.Id))
        {
            context.Update(view);
        }
        else
        {
            context.Add(view);
        }

        if (view.Background is not null && backgroundImage is not null)
        {
            context.Add(new ViewBackgroundImage(view.Id, backgroundImage));
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteViewAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.Set<View>().Where(v => v.Id == id).ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetBackgroundImageAsync(Guid viewId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Set<ViewBackgroundImage>()
            .Where(i => i.ViewId == viewId)
            .Select(i => i.Image)
            .SingleOrDefaultAsync();
    }
}