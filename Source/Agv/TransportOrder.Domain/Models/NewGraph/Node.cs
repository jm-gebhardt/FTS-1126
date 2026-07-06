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
    public IReadOnlyCollection<Edge> IncomingEdges { get; } = [];

    /// <summary>
    /// Collection of outgoing edges from the node.
    /// </summary>
    public IReadOnlyCollection<Edge> OutgoingEdges { get; } = [];

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string? name, string? description, double x, double y, string? levelId) : base(id, name, description)
    {
        X = x;
        Y = y;
        LevelId = levelId;
    }
}