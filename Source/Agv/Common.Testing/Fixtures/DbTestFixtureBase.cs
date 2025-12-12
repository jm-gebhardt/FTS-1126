using System.Collections;
using Gebhardt.Acc.Common.Testing.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

public class DbTestFixtureBase<TContext> : FixtureBase, IAsyncDisposable where TContext : DbContext
{
    private readonly Guid _id = Guid.NewGuid();

    public IDbContextFactory<TContext> ContextFactory { get; }

    public DbTestFixtureBase()
    {
        AddServices(s => s.AddTestingDb<TContext>(_id));
        // ReSharper disable once VirtualMemberCallInConstructor
        Build();
        ContextFactory = Provider.GetRequiredService<IDbContextFactory<TContext>>().EnsureDbCreated();
    }

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<TEntity>().Add(entity);
        context.SaveChanges();
    }

    public async Task AddAsync<TEntity>(TEntity entity, Action<TContext>? beforeAddAction = null) where TEntity : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        beforeAddAction?.Invoke(context);

        await context.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(params object[] entities)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        if (entities is [IEnumerable enumerable])
        {
            entities = enumerable.Cast<object>().ToArray();
        }
        await context.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        using var context = ContextFactory.CreateDbContext();
        context.Database.EnsureDeleted();
        Provider.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await Provider.DisposeAsync();
    }
}