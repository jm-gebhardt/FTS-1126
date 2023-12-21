using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfNodeStateRepositoryTestFixture
{
    private ServiceProvider Provider { get; }

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public readonly EfNodeStateRepository Repository;

    public EfNodeStateRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfNodeStateRepository>>();

        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
            );
        Provider = collection.BuildServiceProvider(true);
        ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
        Repository = new EfNodeStateRepository(ContextFactory, new SemaphoreProvider<INodeStateDbSemaphore>(), mockLogger.Object);
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