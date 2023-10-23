using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfNodeStateRegistryTestFixture
{
    private ServiceProvider Provider { get; }

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public readonly EfNodeStateRegistry Registry;

    public EfNodeStateRegistryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfNodeStateRegistry>>();

        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
            );
        Provider = collection.BuildServiceProvider(true);
        ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
        Registry = new EfNodeStateRegistry(mockLogger.Object, ContextFactory);
    }

    public void Add(NodeLock nodeLock)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Add(nodeLock);
        context.SaveChanges();
    }

    public NodeLock? Get(string nodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        return context.Set<NodeLock>().AsNoTracking().FirstOrDefault(n => n.NodeId == nodeId);
    }
}