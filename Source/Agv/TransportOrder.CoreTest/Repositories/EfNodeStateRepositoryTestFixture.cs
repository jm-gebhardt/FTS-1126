using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfNodeStateRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfNodeStateRepository Repository;

    public EfNodeStateRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfNodeStateRepository>>();

        Repository = new EfNodeStateRepository(ContextFactory, new SemaphoreProvider<INodeStateDbSemaphore>(), mockLogger.Object);
    }

    public NodeLock? Get(string nodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        return context.Set<NodeLock>().AsNoTracking().FirstOrDefault(n => n.NodeId == nodeId);
    }
}