using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class MaintenanceServiceTestFixture
{
    private readonly Mock<IAgvGraphRepository> _graphRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public MaintenanceServiceTestFixture()
    {
        MaintenanceService = new MaintenanceService(_graphRepository.Object, _nodeStateRepository.Object, RouteService.Object, new Mock<ILogger<MaintenanceService>>().Object, new Mock<IAgvDataRepository>().Object);
    }

    public Mock<IRouteService> RouteService { get; } = new();

    public MaintenanceService MaintenanceService { get; private set; }

    public MaintenanceServiceTestFixture WithAvailableNodes(TransportOrder.Domain.Models.Agv.Agv agv, List<GraphNode> availableNodes)
    {
        _graphRepository.Setup(x => x.GetNodesAsync(agv.CorrelationId, It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => availableNodes.AsQueryable().Where(predicate)));
        foreach (var availableNode in availableNodes)
        {
            _graphRepository.Setup(x => x.GetNodeAsync(availableNode.Id, agv.CorrelationId))
                .ReturnsAsync(availableNodes.First(n => n.Id == availableNode.Id));
        }

        _nodeStateRepository.Setup(x => x.FilterFreeUnlockedNodes(availableNodes)).Returns(availableNodes);
        return this;
    }

    public MaintenanceServiceTestFixture WithLowestCostNode(TransportOrder.Domain.Models.Agv.Agv agv, IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>())).ReturnsAsync(node);
        return this;
    }
}