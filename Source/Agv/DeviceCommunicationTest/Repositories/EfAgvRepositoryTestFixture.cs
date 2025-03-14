using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

namespace Gebhardt.Acc.DeviceCommunicationTest.Repositories;

public class EfAgvRepositoryTestFixture: DeviceCommunicationDbTestFixture
{
    public readonly IAgvRepository Repository;

    public EfAgvRepositoryTestFixture()
    {
        Repository = new EfAgvRepository(ContextFactory.CreateDbContext(), new AgvCache());
    }
}