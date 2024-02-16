using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class MaintenanceServiceTestFixture
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public MaintenanceServiceTestFixture()
    {
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync((AgvData?)null);
        MaintenanceService = new MaintenanceService(_agvDb.Object, _nodeStateRepository.Object, RouteService.Object, new Mock<ILogger<MaintenanceService>>().Object);
    }

    public Mock<IRouteService> RouteService { get; } = new();

    public MaintenanceService MaintenanceService { get; private set; }

    public MaintenanceServiceTestFixture WithAvailableNodes(TransportOrder.Domain.Models.Agv.Agv agv, List<GraphNode> availableNodes)
    {
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(agv.CorrelationId, It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => availableNodes.AsQueryable().Where(predicate)));
        foreach (var availableNode in availableNodes)
        {
            _agvDb.Setup(x => x.Graphs.GetNodeAsync(availableNode.Id))
                .ReturnsAsync(availableNodes.First(n => n.Id == availableNode.Id));
        }

        _nodeStateRepository.Setup(x => x.FilterFreeUnlockedNodes(availableNodes)).Returns(availableNodes);
        return this;
    }

    public MaintenanceServiceTestFixture WithLowestCostNode(TransportOrder.Domain.Models.Agv.Agv agv, IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null)).ReturnsAsync(node);
        return this;
    }
}