
namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a node in the graph.
/// </summary>
public class Node : GraphResource
{
    private readonly List<Edge> _incomingEdges = [];
    private readonly List<Edge> _outgoingEdges = [];
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
    /// MapId according to VDA5050/LIF.
    /// </summary>
    public string? MapId { get; }
    
    /// <summary>
    /// NodeId in the LIF.
    /// </summary>
    public string LifNodeId { get; }

    /// <summary>
    /// Collection of incoming edges to the node.
    /// </summary>
    public IReadOnlyCollection<Edge> IncomingEdges => _incomingEdges;

    /// <summary>
    /// Collection of outgoing edges from the node.
    /// </summary>
    public IReadOnlyCollection<Edge> OutgoingEdges => _outgoingEdges;

    /// <summary>
    /// Collection of routing nodes associated with this node.
    /// </summary>
    public IReadOnlyCollection<RoutingNode> RoutingNodes => _routingNodes;

    /// <summary>
    /// Collection of routing edges associated with this node.
    /// </summary>
    public IReadOnlyCollection<RoutingEdge> RoutingEdges => _routingEdges;

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, string lifNodeId, double x, double y, string? levelId, string? mapId) : base(id, name, shortName)
    {
        LifNodeId = lifNodeId;
        X = x;
        Y = y;
        LevelId = levelId;
        MapId = mapId;
    }

    internal void AttachIncomingEdge(Edge edge)
    {
        _incomingEdges.Add(edge);
    }

    internal void AttachOutgoingEdge(Edge edge)
    {
        _outgoingEdges.Add(edge);
    }

    internal void AddRoutingNode(RoutingNode routingNode)
    {
        _routingNodes.Add(routingNode);
    }

    internal void AddRoutingEdge(RoutingEdge routingEdge)
    {
        _routingEdges.Add(routingEdge);
    }

    public override string ToString()
    {
        return $"{nameof(Node)} {Name}, X={X:f3}, Y={Y:f3}, LevelId={LevelId}, MapId={MapId}, Id={Id}";
    }
}