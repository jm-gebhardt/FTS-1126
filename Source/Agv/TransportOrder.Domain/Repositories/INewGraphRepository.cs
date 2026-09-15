using Gebhardt.Acc.Common.Domain.Models.NewGraph;
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
    /// <returns>The graph, empty if nothing is stored.</returns>
    Task<Graph> GetGraphAsync();

    /// <summary>
    /// Merges the given graph into the stored one, adding unknown resources and updating known ones.
    /// </summary>
    /// <param name="graphSegment">The graph segment to merge in.</param>
    Task AddOrUpdateGraphAsync(GraphSegment graphSegment);

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
}