using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public class MaintenanceServiceTestFixture
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public MaintenanceServiceTestFixture()
    {
        _agvDb.AddAgvData((AgvData?)null);
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