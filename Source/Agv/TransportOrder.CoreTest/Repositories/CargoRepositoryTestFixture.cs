using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class CargoRepositoryTestFixture : DbTestFixtureBase<TransportOrderDbContext>
{
    public readonly ICargoRepository Repository;
    public readonly IAgvRepository AgvRepository;

    public CargoRepositoryTestFixture()
    {
        AgvRepository = new AgvRepository(ContextFactory, new InMemoryAgvSettingsProvider());
        Repository = new CargoRepository(ContextFactory, AgvRepository);
    }
}