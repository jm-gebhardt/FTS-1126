namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents an edge in a graph, connecting two nodes.
/// </summary>
public class Edge : GraphResource
{
    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public Node StartNode { get; }

    /// <summary>
    /// End node of the edge.
    /// </summary>
    public Node EndNode { get; }

    /// <inheritdoc cref="Edge"/>
    public Edge(Guid id, string? name, string? description, Node startNode, Node endNode) : base(id, name, description)
    {
        StartNode = startNode;
        EndNode = endNode;
    }
}