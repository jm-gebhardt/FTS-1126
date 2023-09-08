using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for the AgvRegistry
/// </summary>
[JetBrains.Annotations.PublicAPI]
public interface IAgvGraphRegistry
{
    /// <summary>
    /// Gets the graph from the GraphRegistry for this Agv
    /// </summary>
    Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId);

    /// <summary>
    /// Gets all AgvGraph from registry.
    /// </summary>
    /// <returns></returns>
    Task<List<AgvGraph>> GetAllAgvGraphsAsync();

    /// <summary>
    /// Get nodes of this Agv, but only which are included in the predicate filter
    /// </summary>
    /// <param name="agvCorrelationId">The AgvCorrelationId</param>
    /// <param name="predicate">Optional Linq predicate. If null, there will be no filtering</param>
    Task<IEnumerable<GraphNode>> GetNodesAsync(Guid agvCorrelationId, Expression<Func<GraphNode, bool>>? predicate = null);

    /// <summary>
    /// Get node of this Agv by NodeId
    /// </summary>
    /// <param name="id">The Id of the node to get</param>
    /// <param name="agvCorrelationId">The AgvCorrelationId</param>
    Task<GraphNode> GetNodeAsync(string id, Guid agvCorrelationId);

    /// <summary>
    /// Update the whole graph from <see cref="AgvGraph"/>
    /// </summary>
    /// <param name="agvGraph">The Agv graph to add or replace</param>
    Task AddOrUpdateAsync(AgvGraph agvGraph);

    /// <summary>
    /// Remove the graph for the given Agv
    /// </summary>
    /// <param name="agvCorrelationId">The Agv CorrelationId</param>
    /// <returns></returns>
    Task RemoveAsync(Guid agvCorrelationId);

    /// <summary>
    /// Check if a graph for the given Agv exists
    /// </summary>
    /// <param name="agvCorrelationId">The Agv CorrelationId</param>
    /// <returns>True if the AgvGraph for given Agv exists</returns>
    Task<bool> ExistsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Check if a node exists in the registry regardless of an agv
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    Task<bool> NodeExistsAsync(string nodeId);
}