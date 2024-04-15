using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Services.Metrics;

public class PrometheusMetricCollectionServiceTestFixture
{
    private readonly Mock<IAgvDb> _agvDbMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    
    public PrometheusMetricCollectionService Service { get; }

    public PrometheusMetricCollectionServiceTestFixture()
    {
        Service = new PrometheusMetricCollectionService(_agvDbMock.Object, _actionGroupRepositoryMock.Object);
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
}