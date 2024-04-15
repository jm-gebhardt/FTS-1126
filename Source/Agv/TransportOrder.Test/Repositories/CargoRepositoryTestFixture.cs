using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

public class CargoRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly ICargoRepository Repository;
    public readonly IAgvRepository AgvRepository;

    public CargoRepositoryTestFixture()
    {
        AgvRepository = new AgvRepository(ContextFactory, new InMemoryAgvSettingsProvider());
        Repository = new CargoRepository(ContextFactory, AgvRepository);
    }
}