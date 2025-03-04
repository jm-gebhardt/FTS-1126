using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class CargoRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly ICargoRepository Repository;
    public readonly IAgvRepository AgvRepository;

    private readonly IConcurrentDictionaryProvider<Guid, List<Cargo>> _cargoCache = new AgvCargoDictionaryProvider();

    public CargoRepositoryTestFixture()
    {
        AgvRepository = new AgvRepository(ContextFactory, new InMemoryAgvSettingsProvider());
        Repository = new CargoRepository(ContextFactory, AgvRepository, _cargoCache);
    }

    public CargoRepositoryTestFixture WithCachedCargo(Guid agvCorrelationId, List<Cargo> cargos)
    {
        _cargoCache.ConcurrentDict.TryAdd(agvCorrelationId, cargos);
        return this;
    }

    public List<Cargo> GetCachedCargo(Guid agvCorrelationId)
    {
        return _cargoCache.ConcurrentDict[agvCorrelationId];
    }
}