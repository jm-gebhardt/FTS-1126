using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfQueueRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfQueueRepository Repository;

    public EfQueueRepositoryTestFixture()
    {
        Repository = new EfQueueRepository(ContextFactory, new Mock<ILogger<EfQueueRepository>>().Object);
    }
}