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
    private readonly Mock<IAgvGraphRegistry> _graphRegistry = new();
    private readonly Mock<INodeStateRegistry> _nodeStateRegistry = new();

    public MaintenanceServiceTestFixture()
    {
        MaintenanceService = new MaintenanceService(_graphRegistry.Object, _nodeStateRegistry.Object, RouteService.Object, new Mock<ILogger<MaintenanceService>>().Object);
    }

    public Mock<IRouteService> RouteService { get; } = new();
    public MaintenanceService MaintenanceService { get; private set; }
    
    public MaintenanceServiceTestFixture WithAvailableNodes(IAgv agv, List<GraphNode> availableNodes)
    {
        _graphRegistry.Setup(x => x.GetNodesAsync(agv.CorrelationId, It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => availableNodes.AsQueryable().Where(predicate)));
        foreach (var availableNode in availableNodes)
        {
            _graphRegistry.Setup(x => x.GetNodeAsync(availableNode.Id, agv.CorrelationId))
                .ReturnsAsync(availableNodes.First(n => n.Id == availableNode.Id));
        }
       
        _nodeStateRegistry.Setup(x => x.FilterFreeUnlockedNodes(availableNodes)).Returns(availableNodes);
        return this;
    }

    public MaintenanceServiceTestFixture WithLowestCostNode(IAgv agv, IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes)).ReturnsAsync(node);
        return this;
    }
}