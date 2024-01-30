using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;

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
    /// <param name="agvData">The agv data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The Cost if a route was found, otherwise null</returns>
    Task<double?> GetCostsAsync(Models.Agv.Agv agv, string destinationNodeId, AgvData? agvData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if given AGV has route for given ActionGroup
    /// firstAction contains the first action of the ActionGroup or null if no route is found
    /// firstActionCost contains the costs for the first action of the ActionGroup or float.MaxValue if no route is found
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="actionGroup"></param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [PublicAPI]
    Task<double?> GetCostsForFirstActionAsync(Models.Agv.Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true once it found the first Agv which can handle the whole ActionGroup (based on it's current node)
    /// </summary>
    /// <param name="actionGroup">The ActionGroup containing one or more Actions of which the target node Id will be considered</param>
    /// <param name="agvs">Agvs to consider</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> HasAnyAgvRouteAsync(Models.ActionGroup.ActionGroup actionGroup, IEnumerable<Models.Agv.Agv> agvs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine route for Agv to go to destination
    /// </summary>
    /// <param name="agv">The agv to calculate the route for.</param>
    /// <param name="destinationNodeId">The destination node Id.</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The found route</returns>
    /// <exception cref="RouteNotFoundException"></exception>
    Task<Route> GetRouteAsync(Models.Agv.Agv agv, string destinationNodeId, string? startNodeId = null, AgvData? agvData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine route for Agv from its last node to the destination nodes one after the other
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destinationNodeIds">The list of destination node which should be approached one by one</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <param name="agvData">The agv data.</param>
    /// <returns>The found route</returns>
    /// <exception cref="RouteNotFoundException"></exception>
    [PublicAPI]
    Task<Route> GetRouteAsync(Models.Agv.Agv agv, IList<string> destinationNodeIds, string? startNodeId, AgvData? agvData);

    /// <summary>
    /// Returns the node from availableNodes with the lowest cost route from agv last node
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="availableNodes"></param>
    /// <param name="agvData">The agv data.</param>>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <returns>The found destination node with lowest cost route</returns>
    /// <exception cref="RouteNotFoundException" />
    [PublicAPI]
    Task<GraphNode> GetNodeWithLowestCostsAsync(Models.Agv.Agv agv, IList<GraphNode> availableNodes, AgvData? agvData, string? startNodeId = null);
}