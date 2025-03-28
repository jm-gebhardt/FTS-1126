using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfZoneRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly IZoneRepository Repository;

    public EfZoneRepositoryTestFixture()
    {
        Repository = new EfZoneRepository(ContextFactory);
    }
}