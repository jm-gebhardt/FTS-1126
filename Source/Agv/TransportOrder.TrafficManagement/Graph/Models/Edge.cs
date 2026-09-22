using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents an edge in a graph, connecting two nodes.
/// </summary>
[PublicAPI]
public class Edge : GraphResource
{
    private readonly List<RoutingEdge> _routingEdges = [];

    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public Node StartNode { get; }

    /// <summary>
    /// End node of the edge.
    /// </summary>
    public Node EndNode { get; }

    /// <summary>
    /// Collection of routing edges associated with this edge.
    /// </summary>
    public IReadOnlyCollection<RoutingEdge> RoutingEdges => _routingEdges;

    /// <inheritdoc cref="Edge"/>
    public Edge(Guid id, string name, string shortName, Node startNode, Node endNode) : base(id, name, shortName)
    {
        StartNode = startNode;
        EndNode = endNode;
    }

    internal void AddRoutingEdge(RoutingEdge routingEdge)
    {
        _routingEdges.Add(routingEdge);
    }
}