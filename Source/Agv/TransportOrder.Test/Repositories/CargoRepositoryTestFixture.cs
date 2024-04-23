using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

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