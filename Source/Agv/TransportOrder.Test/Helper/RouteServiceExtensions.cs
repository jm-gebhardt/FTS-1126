using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class RouteServiceExtensions
{
    public static void WithRoute(this Mock<IRouteService> routeServiceMock, double costs, string[] nodes)
    {
        routeServiceMock.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(costs, nodes));
    }

    public static void WithRouteCostsFirstAction(this Mock<IRouteService> routeServiceMock, TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup, float? routeCosts = null)
    {
        routeServiceMock.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(routeCosts);
    }

    public static void WithRouteLowestCostNode(this Mock<IRouteService> routeServiceMock, TransportOrder.Domain.Models.Agv.Agv agv, IList<GraphNode> availableNodes, GraphNode node)
    {
        routeServiceMock.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null)).ReturnsAsync(node);
    }
}