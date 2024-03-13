using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class ActionGroupDurationEstimationServiceTestFixture
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)> _deviationCache = new DeviationCacheProvider();

    public ActionGroupDurationEstimationService Service { get; }

    public ActionGroupDurationEstimationServiceTestFixture()
    {
        Service = new ActionGroupDurationEstimationService(_deviationCache ,_actionGroupRepository.Object);
    }

    public ActionGroupDurationEstimationServiceTestFixture WithDeviation((double value, DateTime lastUpdated) deviation, Guid agvId, string[] targets, bool existsInMemory)
    {
        if (existsInMemory)
        {
            string key = ActionGroupDurationEstimationService.GetKey(agvId, targets);
            _deviationCache.ConcurrentDict.AddOrUpdate(key, deviation, (_,_)=> deviation);
        }
        _actionGroupRepository.Setup(x => x.GetAverageDurationEstimateDeviationAsync(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deviation.value);
        return this;
    }

    public bool ValueExistsInMemory(Guid agvId, string[] targets, out (double value, DateTime lastUpdated) actual)
    {
        string key = ActionGroupDurationEstimationService.GetKey(agvId, targets);
        return _deviationCache.ConcurrentDict.TryGetValue(key, out actual);
    }
}