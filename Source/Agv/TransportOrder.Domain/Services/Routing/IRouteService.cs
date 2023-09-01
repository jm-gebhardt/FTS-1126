using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using TransportOrder.Routing;
using TransportOrder.Routing.Exceptions;

namespace TransportOrder.Domain.Services.Routing;

/// <summary>
/// Service for computation of Route for Agv
/// </summary>
public interface IRouteService
{
    /// <summary>
    /// Computes if Agv has route for last node to destination node to check if Agv can handle route
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destinationNodeId">The destination node Id</param>
    /// <param name="costs">The costs for the found route. Null if the method returns false</param>
    bool HasRoute(IAgv agv, string destinationNodeId, out float? costs);

    /// <summary>
    /// Computes if Agv has route for last node to Check if the vehicle can drive from its last node to the destination nodes one after the other
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destinationNodeIds">The list of destination node which should be approached one by one</param>
    /// <param name="costs">The costs for the found route. Null if the method returns false</param>
    [PublicAPI]
    bool HasRoute(IAgv agv, IList<string> destinationNodeIds, out float? costs);

    /// <summary>
    /// Determine route for Agv to go to destination
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destinationNodeId">The destination node Id</param>
    /// <returns>The found route</returns>
    /// <exception cref="RouteNotFoundException"></exception>
    Task<IGraphAlgorithm.Route> GetRouteAsync(IAgv agv, string destinationNodeId);

    /// <summary>
    /// Determine route for Agv from its last node to the destination nodes one after the other
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destinationNodeIds">The list of destination node which should be approached one by one</param>
    /// <returns>The found route</returns>
    /// <exception cref="RouteNotFoundException"></exception>
    [PublicAPI]
    Task<IGraphAlgorithm.Route> GetRouteAsync(IAgv agv, IList<string> destinationNodeIds);

    /// <summary>
    /// Returns the node from availableNodes with the lowest cost route from agv last node
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="availableNodes"></param>
    /// <returns>The found destination node with lowest cost route</returns>
    /// <exception cref="RouteNotFoundException" />
    [PublicAPI]
    Task<GraphNode> GetNodeWithLowestCostsAsync(IAgv agv, IList<GraphNode> availableNodes);
}