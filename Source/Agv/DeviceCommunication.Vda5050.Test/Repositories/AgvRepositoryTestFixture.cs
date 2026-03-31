using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

public class AgvRepositoryTestFixture : DeviceCommunicationVda5050DbFixture
{
    public readonly IAgvCacheProvider Cache = new AgvCache();
    public readonly IAgvRepository Repository;

    public AgvRepositoryTestFixture()
    {
        Repository = new EfAgvRepository(ContextFactory, Cache);
    }
}
