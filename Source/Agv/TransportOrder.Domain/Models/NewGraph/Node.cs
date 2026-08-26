using CommonNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a node in the graph.
/// </summary>
public class Node : GraphResource
{
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
    public IEnumerable<RoutingNode> RoutingNodes => Children.OfType<RoutingNode>();

    /// <summary>
    /// Collection of routing edges associated with this node.
    /// </summary>
    public IEnumerable<RoutingEdge> RoutingEdges => Children.OfType<RoutingEdge>();

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, double x, double y, string? levelId) : base(id, name, shortName)
    {
        X = x;
        Y = y;
        LevelId = levelId;
    }

    /// <summary>
    /// For deserialization.
    /// </summary>
    public Node() : base(Guid.NewGuid(), null!, null!)
    {
    }

    /// <summary>
    /// Generates a node from a <see cref="CommonNode"/>.
    /// </summary>
    /// <param name="commonNode">The <see cref="CommonNode"/>.</param>
    public static Node FromCommon(CommonNode commonNode)
    {
        return new Node(commonNode.Id, commonNode.Name, commonNode.ShortName, commonNode.X, commonNode.Y, commonNode.LevelId);
    }
}