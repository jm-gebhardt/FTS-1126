using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs;

public class EfNodeStateRegistryTestFixture
{
    private ServiceProvider Provider { get; }
    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public readonly EfNodeStateRegistry Registry;

    public EfNodeStateRegistryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfNodeStateRegistry>>();
      
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                }
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