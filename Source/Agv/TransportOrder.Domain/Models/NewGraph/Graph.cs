using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a graph consisting of nodes, edges, and stations.
/// </summary>
[PublicAPI]
public class Graph
{
    [UsedImplicitly]
    private readonly List<Node> _nodes = [];

    [UsedImplicitly]
    private readonly List<Edge> _edges = [];
    
    [UsedImplicitly]
    private readonly List<Station> _stations = [];

    /// <summary>
    /// Unique identifier of the graph.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Collection of nodes in the graph.
    /// </summary>
    public IReadOnlyCollection<Node> Nodes => _nodes;

    /// <summary>
    /// Collection of edges in the graph.
    /// </summary>
    public IReadOnlyCollection<Edge> Edges => _edges;

    /// <summary>
    /// Collection of stations in the graph.
    /// </summary>
    public IReadOnlyCollection<Station> Stations => _stations;
}