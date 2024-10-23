using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfQueueRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfQueueRepository Repository;

    public EfQueueRepositoryTestFixture()
    {
        Repository = new EfQueueRepository(ContextFactory);
    }
}