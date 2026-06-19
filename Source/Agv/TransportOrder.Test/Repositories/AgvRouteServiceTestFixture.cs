using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class AgvRouteServiceTestFixture : TransportOrderDbFixture
{
    private readonly Mock<IAgvRepository> _agvRepositoryMock = new();
    private readonly Mock<ILogger<AgvRouteRetrievalService>> _loggerMock = new();
    private readonly List<ActionGroup> _actionsGroups = new();
    private readonly List<TransportOrder.Domain.Models.Agv.Agv> _agvs = new();

    public AgvRoutePlanCache RoutePlanCache { get; } = new();

    public AgvRouteRetrievalService AgvRouteRetrievalService { get; }

    public AgvRouteServiceTestFixture()
    {
        AgvRouteRetrievalService = new AgvRouteRetrievalService(ContextFactory, _agvRepositoryMock.Object, RoutePlanCache, _loggerMock.Object);
    }

    private void SetupAgvs()
    {
        foreach (var agv in _agvs)
        {
            _agvRepositoryMock.Setup(x => x.GetLastNodeIdAsync(agv.CorrelationId)).ReturnsAsync(agv.AgvStatus!.LastNodeId);
        }

        _agvRepositoryMock.Setup(x => x.GetLastNodeIdAsync(It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync((ICollection<Guid> agvCorrelationIds) =>
            {
                var result = new Dictionary<Guid, Guid?>();

                if (agvCorrelationIds.Count == 0)
                {
                    // if no agvCorrelationIds are given we want to use all agvs
                    agvCorrelationIds = _agvs.Select(a => a.CorrelationId).ToList();
                }

                foreach (var id in agvCorrelationIds)
                {
                    var matchingAgv = _agvs.FirstOrDefault(agv => agv.CorrelationId == id);
                    if (matchingAgv != null)
                    {
                        result[id] = matchingAgv.AgvStatus?.LastNodeId;
                    }
                }
                return result;
            });
    }

    public AgvRouteServiceTestFixture WithAgv(params TransportOrder.Domain.Models.Agv.Agv[] agv)
    {
        _agvs.AddRange(agv);
        return this;
    }

    public AgvRouteServiceTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionsGroups.Add(actionGroup);
        return this;
    }

    private new void Build()
    {
        base.Build();
    }

    public new async Task<AgvRouteServiceTestFixture> BuildAsync()
    {
        SetupAgvs();
        AddInitTask(AddRangeAsync(_actionsGroups));

        await base.BuildAsync();
        Build();
        return this;
    }
}