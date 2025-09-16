using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Routing;

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
    Task<double?> GetCostsAsync(Agv agv, Guid destinationNodeId, AgvData? agvData, CancellationToken cancellationToken = default);

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
    Task<double?> GetCostsForFirstActionAsync(Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if given AGV has route for given ActionGroup
    /// firstAction contains the first action of the ActionGroup or null if no route is found
    /// firstActionCost contains the costs for the first action of the ActionGroup or float.MaxValue if no route is found
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="actionGroup"></param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="nodesToAvoid">The nodes to avoid.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [PublicAPI]
    Task<double?> GetCostsForFirstActionNotPassingNodesAsync(Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, ICollection<Guid> nodesToAvoid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true once it found the first Agv which can handle the whole ActionGroup (based on its current node)
    /// </summary>
    /// <param name="actionGroup">The ActionGroup containing one or more Actions of which the target node Id will be considered</param>
    /// <param name="agvs">Agvs to consider</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> HasAnyAgvRouteAsync(Models.ActionGroup.ActionGroup actionGroup, IEnumerable<Agv> agvs, CancellationToken cancellationToken = default);

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
    Task<Route> GetRouteAsync(Agv agv, Guid destinationNodeId, Guid? startNodeId = null, AgvData? agvData = null, CancellationToken cancellationToken = default);

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
    Task<Route> GetRouteAsync(Agv agv, IList<Guid> destinationNodeIds, Guid? startNodeId, AgvData? agvData);

    /// <summary>
    /// Returns the node from availableNodes with the lowest cost route from agv last node
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="availableNodes"></param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// >
    /// <returns>The found destination node with the lowest route cost </returns>
    /// <exception cref="RouteNotFoundException" />
    [PublicAPI]
    Task<GraphNode> GetNodeWithLowestCostsAsync(Agv agv, IList<GraphNode> availableNodes, AgvData? agvData, Guid? startNodeId = null);

    /// <summary>
    /// Returns the correlation id and route of the agv closest to the given target node, that can reach the node.
    /// </summary>
    /// <param name="agvs">Candidate agvs.</param>
    /// <param name="targetNodeId">Target node id.</param>
    /// <param name="skipAgvsStartingAtTarget">If true, do not consider agvs that start from the target.</param>
    /// <returns>Correlation id of the agv closest to the node.</returns>
    Task<(Guid? agv, Route? route)> GetAgvRouteClosestToTarget(ICollection<(Agv agv, AgvData? agvData)> agvs, Guid targetNodeId, bool skipAgvsStartingAtTarget = false);
}