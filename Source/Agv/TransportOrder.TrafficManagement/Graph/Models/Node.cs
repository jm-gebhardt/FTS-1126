using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a node in the graph.
/// </summary>
[PublicAPI]
public class Node : GraphResource
{
    private readonly List<RoutingNode> _routingNodes = [];
    private readonly List<RoutingEdge> _routingEdges = [];

    /// <summary>
    /// X position on the layout in reference to the global origin.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y position on the layout in reference to the global origin.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Identifier for the level the node is on.
    /// </summary>
    public string? LevelId { get; }

    /// <summary>
    /// Collection of incoming edges to the node.
    /// </summary>
    public List<Edge> IncomingEdges { get; } = [];

    /// <summary>
    /// Collection of outgoing edges from the node.
    /// </summary>
    public List<Edge> OutgoingEdges { get; } = [];

    /// <summary>
    /// Collection of routing nodes associated with this node.
    /// </summary>
    public IReadOnlyCollection<RoutingNode> RoutingNodes => _routingNodes;

    /// <summary>
    /// Collection of routing edges associated with this node.
    /// </summary>
    public IReadOnlyCollection<RoutingEdge> RoutingEdges => _routingEdges;

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, double x, double y, string? levelId) : base(id, name, shortName)
    {
        X = x;
        Y = y;
        LevelId = levelId;
    }

    internal void AddRoutingNode(RoutingNode routingNode)
    {
        _routingNodes.Add(routingNode);
    }

    internal void AddRoutingEdge(RoutingEdge routingEdge)
    {
        _routingEdges.Add(routingEdge);
    }
}