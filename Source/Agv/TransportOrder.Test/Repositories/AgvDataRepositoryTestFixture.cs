using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class AgvDataRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly IAgvDataRepository Repository;

    public AgvDataRepositoryTestFixture()
    {
        Repository = new EfAgvDataRepository(ContextFactory);
    }
}