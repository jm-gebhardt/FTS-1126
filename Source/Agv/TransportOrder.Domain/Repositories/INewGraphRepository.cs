using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for the graph.
/// </summary>
[PublicAPI]
public interface INewGraphRepository
{
    /// <summary>
    /// Loads the graph with its nodes, edges and stations, each one's routing resources, and conflicting resources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The graph, empty if nothing is stored.</returns>
    Task<Graph> GetGraphAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges the given graph into the stored one, adding unknown resources and updating known ones.
    /// </summary>
    /// <param name="graphSegment">The graph segment to merge in.</param>
    Task AddOrUpdateGraphSegmentAsync(GraphSegment graphSegment);

    /// <summary>
    /// Deletes all nodes, edges and stations that belong to the graph segment with the given id.
    /// </summary>
    /// <param name="graphSegmentId">The id of the graph segment to delete.</param>
    /// <returns>The deleted graph segment</returns>
    Task<bool> DeleteGraphSegmentAsync(Guid graphSegmentId);

    /// <summary>
    /// Indicates whether a routing node with the given id is persisted.
    /// </summary>
    /// <param name="id">Id of the routing node.</param>
    Task<bool> RoutingNodeExistsAsync(Guid id);

    /// <summary>
    /// Indicates whether a routing edge with the given id is persisted.
    /// </summary>
    /// <param name="id">Id of the routing edge.</param>
    Task<bool> RoutingEdgeExistsAsync(Guid id);

    /// <summary>
    /// Returns the area covered by the nodes and stations of each level. A station belongs to the level of its
    /// first interaction node.
    /// </summary>
    /// <returns>One area per level, empty if there is no node.</returns>
    Task<IReadOnlyList<LevelArea>> GetLevelAreasAsync();
}