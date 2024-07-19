using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfRestrictedAreaRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfRestrictedAreaRepository Repository;

    public EfRestrictedAreaRepositoryTestFixture()
    {
        Repository = new EfRestrictedAreaRepository(ContextFactory);
    }
}