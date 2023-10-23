using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class AgvDataRepositoryTestFixture : DbTestFixtureBase<TransportOrderDbContext>
{
    public readonly IAgvDataRegistry Repository;

    public AgvDataRepositoryTestFixture()
    {
        Repository = new EfAgvDataRegistry(ContextFactory);
    }
}