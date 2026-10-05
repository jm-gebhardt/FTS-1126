using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class RouteServiceExtensions
{
    /// <summary>
    /// Extension block for <see cref="Mock{IRouteService}"/>.
    /// </summary>
    extension(Mock<IRouteService> routeServiceMock)
    {
        /// <summary>
        /// Configures the mock route service to return a <see cref="Route"/> with specified costs, nodes, and edges.
        /// </summary>
        /// <param name="costs">The cost of the route.</param>
        /// <param name="nodes">The node names in the route.</param>
        /// <param name="edges">Optional edge IDs in the route.</param>
        public void WithRoute(double costs, string[] nodes, Guid[]? edges = null)
        {
            var routeEdges = edges ?? [];
            var graphNodes = nodes.Select(n => n.ToGraphNode()).ToArray();
            routeServiceMock.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(costs, graphNodes, routeEdges));
            routeServiceMock.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<IList<Guid>>(), It.IsAny<Guid?>(), It.IsAny<AgvData>())).ReturnsAsync(new Route(costs, graphNodes, routeEdges));
        }

        /// <summary>
        /// Configures the mock route service to return route costs for the first action in an <see cref="ActionGroup"/>.
        /// </summary>
        /// <param name="agv">The <see cref="TransportOrder.Domain.Models.Agv.Agv"/> for which to calculate costs.</param>
        /// <param name="actionGroup">The <see cref="ActionGroup"/> containing the action.</param>
        /// <param name="routeCosts">The route costs to return.</param>
        public void WithRouteCostsFirstAction(TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup, float? routeCosts = null)
        {
            routeServiceMock.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(routeCosts);
        }

        /// <summary>
        /// Configures the mock route service to return the <see cref="GraphNode"/> with the lowest cost from available nodes.
        /// </summary>
        /// <param name="agv">The <see cref="TransportOrder.Domain.Models.Agv.Agv"/> for which to find the lowest cost node.</param>
        /// <param name="availableNodes">The available <see cref="GraphNode"/>s to choose from.</param>
        /// <param name="node">The <see cref="GraphNode"/> to return as having the lowest cost.</param>
        public void WithRouteLowestCostNode(TransportOrder.Domain.Models.Agv.Agv agv, IList<GraphNode> availableNodes, GraphNode node)
        {
            routeServiceMock.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null)).ReturnsAsync(node);
        }
    }
}