using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class ActionGroupRetrievalServiceTestFixture : IAsyncDisposable
{

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        ContextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory);
    }

    public async Task AddAsync<T>(T entity) where T : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        await context.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(params object[] entities)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        await context.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        
        await context.Database.EnsureDeletedAsync();
    }
}