using System.Collections;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class DbTestFixtureBase<TContext> : IAsyncDisposable where TContext : DbContext
{
    protected IDbContextFactory<TContext> ContextFactory { get; }

    protected DbTestFixtureBase()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        ContextFactory = provider.GetRequiredService<IDbContextFactory<TContext>>();
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

    public async ValueTask DisposeAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        await context.Database.EnsureDeletedAsync();
    }
}