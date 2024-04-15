using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;

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