using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

public class AgvDataRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly IAgvDataRepository Repository;

    public AgvDataRepositoryTestFixture()
    {
        Repository = new EfAgvDataRepository(ContextFactory);
    }
}