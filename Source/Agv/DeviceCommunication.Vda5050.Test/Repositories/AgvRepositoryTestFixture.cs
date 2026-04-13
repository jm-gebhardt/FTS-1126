using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

public class AgvRepositoryTestFixture : DeviceCommunicationVda5050DbFixture
{
    public readonly IAgvRepository Repository;
    private readonly IAgvCacheProvider _cache = new AgvCache();

    public AgvRepositoryTestFixture()
    {
        Repository = new EfAgvRepository(ContextFactory, _cache);
    }

    public AgvRepositoryTestFixture WithCachedAgv(Agv agv) 
    {
        _cache.ConcurrentDict.AddOrUpdate(agv.CorrelationId, agv, (_, _) => agv);
        return this;
    }
}