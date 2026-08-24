using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
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
    /// <param name="graph">The graph to merge in.</param>
    Task AddOrUpdateGraphAsync(Graph graph);
}