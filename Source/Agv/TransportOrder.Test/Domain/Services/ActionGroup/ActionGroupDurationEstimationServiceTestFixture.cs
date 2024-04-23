using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

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