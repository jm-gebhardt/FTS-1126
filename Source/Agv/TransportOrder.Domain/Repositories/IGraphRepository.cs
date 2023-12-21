using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for the graph repository.
/// </summary>
[PublicAPI]
public interface IGraphRepository
{
    /// <summary>
    /// Gets the graph from the GraphRepository for this agv.
    /// </summary>
    Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId);

    /// <summary>
    /// Gets correlation ids of all agvs on a map.
    /// </summary>
    /// <param name="mapId">The correlation id of the map.</param>
    Task<ICollection<Guid>> GetAgvsRelatedToMap(Guid mapId);

    /// <summary>
    /// Gets the graph of selected agvs.
    /// </summary>
    /// <param name="correlationIds">The agv correlation ids,.</param>
    /// <returns></returns>
    Task<ICollection<AgvGraph>> GetSelectedAgvGraphsAsync(ICollection<Guid> correlationIds);

    /// <summary>
    /// Get nodes of this agv, but only which are included in the predicate filter.
    /// </summary>
    /// <param name="agvCorrelationId">The AgvCorrelationId</param>
    /// <param name="predicate">Optional Linq predicate. If null, there will be no filtering</param>
    Task<IEnumerable<GraphNode>> GetNodesAsync(Guid agvCorrelationId, Expression<Func<GraphNode, bool>>? predicate = null);

    /// <summary>
    /// Get node of this agv by NodeId.
    /// </summary>
    /// <param name="id">The Id of the node to get</param>
    Task<GraphNode> GetNodeAsync(string id);

    /// <summary>
    /// Gets the graph for the given map.
    /// </summary>
    /// <param name="mapCorrelationId">The correlation id of the map.</param>
    /// <returns></returns>
    Task<IGraph> GetGraphForMap(Guid mapCorrelationId);

    /// <summary>
    /// Update the relation to the nodes/edges and Agvs. If the AgvGraph does not exist, it will be created.
    /// </summary>
    /// <param name="agvGraph">The Agv graph to add or replace</param>
    /// <returns>True if the AgvGraph was added or updated. False if e.g. not all nodes/edges were existing.</returns>
    Task<bool> AddOrUpdateAgvGraphAsync(AgvGraph agvGraph);

    /// <summary>
    /// Remove the graph for the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The Agv CorrelationId</param>
    /// <returns></returns>
    Task RemoveAsync(Guid agvCorrelationId);

    /// <summary>
    /// Check if a graph for the given agv exists.
    /// </summary>
    /// <param name="agvCorrelationId">The Agv CorrelationId</param>
    /// <returns>True if the AgvGraph for given Agv exists</returns>
    Task<bool> ExistsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Check if a (global) graph exists.
    /// </summary>
    /// <returns>True if any Nodes/Edges for the (global) graph exists.</returns>
    Task<bool> ExistsAsync();

    /// <summary>
    /// Check if a node exists in the repository.
    /// </summary>
    /// <param name="nodeId">The Id of the node</param>
    /// <returns></returns>
    Task<bool> NodeExistsAsync(string nodeId);

    /// <summary>
    /// Gets the (global) Graph. If there is none it will create an empty one.
    /// </summary>
    Task<Graph> GetGraphAsync();

    /// <summary>
    /// Get nodes , but only which are included in the predicate filter.
    /// </summary>
    /// <param name="predicate">Optional Linq predicate. If null, there will be no filtering</param>
    Task<IEnumerable<GraphNode>> GetNodesAsync(Expression<Func<EfNode, bool>>? predicate = null);

    /// <summary>
    /// Updates the Graph.
    /// </summary>
    /// <param name="graph">The new Graph.</param>
    Task UpdateGraphAsync(Graph graph);

    /// <summary>
    /// Get the CorrelationIds of the Agv which have at least one node in the given list.
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    Task<ICollection<Guid>> GetAgvIdsRelatedToNodesAsync(ICollection<GraphNode> nodes);
}