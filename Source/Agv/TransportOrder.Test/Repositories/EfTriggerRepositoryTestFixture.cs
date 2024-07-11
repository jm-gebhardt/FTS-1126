using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfTriggerRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfTriggerRepository Repository;

    public EfTriggerRepositoryTestFixture()
    {
        Repository = new EfTriggerRepository(ContextFactory);
    }
}