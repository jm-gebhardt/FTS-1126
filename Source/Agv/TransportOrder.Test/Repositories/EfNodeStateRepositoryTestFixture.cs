using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

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
        return context.Set<NodeLock>().AsNoTracking().SingleOrDefault(n => n.NodeId == nodeId);
    }
}