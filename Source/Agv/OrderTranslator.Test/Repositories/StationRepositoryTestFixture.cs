using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.OrderTranslator.Test.Repositories;

public class StationRepositoryTestFixture: SidcarDbTestFixture
{
    public readonly IStationRepository Repository;

    public StationRepositoryTestFixture()
    {
        Repository = new EfStationRepository(ContextFactory);
    }
}