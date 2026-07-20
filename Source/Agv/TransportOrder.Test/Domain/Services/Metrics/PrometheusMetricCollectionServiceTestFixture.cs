using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Metrics;

public class PrometheusMetricCollectionServiceTestFixture
{
    private readonly Mock<IAgvDb> _agvDbMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepositoryMock = new();

    public FakeTimeProvider TimeProvider { get; } = new();

    public PrometheusMetricCollectionService Service { get; }

    public PrometheusMetricCollectionServiceTestFixture()
    {
        Service = new PrometheusMetricCollectionService(_agvDbMock.Object, _actionGroupRepositoryMock.Object, _stationRepositoryMock.Object, TimeProvider);
    }

    public PrometheusMetricCollectionServiceTestFixture AddGraph(AgvGraph graph)
    {
        _agvDbMock.Setup(x => x.Graphs.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        return this;
    }

    public PrometheusMetricCollectionServiceTestFixture AddActionGroups(List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups)
    {
        _actionGroupRepositoryMock.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(actionGroups);
        return this;
    }

    public PrometheusMetricCollectionServiceTestFixture WithStation(Station? station)
    {
        _stationRepositoryMock.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<Guid>())).ReturnsAsync(station);
        return this;
    }
}