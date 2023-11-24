using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfNodeRepositoryTestFixture: IDisposable, IAsyncDisposable
{
    public EfNodeRepository Repository { get; }

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    private ServiceProvider Provider { get; }

    public EfNodeRepositoryTestFixture()
    {
        var collection = new ServiceCollection()
       .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
        );
        Provider = collection.BuildServiceProvider(true);
        ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
        Repository = new EfNodeRepository(ContextFactory);
    }

    public void AddGraph(EfGraph graph)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<EfGraph>().Add(graph);
        context.SaveChanges();
    }

    public void AddNodeLocks(List<NodeLock> nodeLocks)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<NodeLock>().AddRange(nodeLocks);
        context.SaveChanges();
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