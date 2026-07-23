using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a graph consisting of nodes, edges, and stations.
/// </summary>
[UsedImplicitly]
public class Graph
{
    /// <summary>
    /// Unique identifier of the graph.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Collection of nodes in the graph.
    /// </summary>
    public IReadOnlyCollection<Node> Nodes { get; } = [];

    /// <summary>
    /// Collection of edges in the graph.
    /// </summary>
    public IReadOnlyCollection<Edge> Edges { get; } = [];

    /// <summary>
    /// Collection of stations in the graph.
    /// </summary>
    public IReadOnlyCollection<Station> Stations { get; } = [];
}