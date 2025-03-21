using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

public class StationRepositoryTestFixture: DbTestFixtureBase<KaeserSidecarDbContext>
{
    public readonly EfStationRepository Repository;

    public StationRepositoryTestFixture()
    {
        Repository = new EfStationRepository(ContextFactory, OrderGenerator.KaeserConfiguration);
    }
}