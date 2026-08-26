using JetBrains.Annotations;
using CommonEdge = Gebhardt.Acc.Common.Domain.Models.NewGraph.Edge;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents an edge in a graph, connecting two nodes.
/// </summary>
[PublicAPI]
public class Edge : GraphResource
{
    /// <summary>
    /// Id of the node the edge starts at.
    /// </summary>
    public Guid StartNodeId { get; }

    /// <summary>
    /// Id of the node the edge ends at.
    /// </summary>
    public Guid EndNodeId { get; }

    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public Node StartNode { get; }

    /// <summary>
    /// End node of the edge.
    /// </summary>
    public Node EndNode { get; }

    /// <inheritdoc cref="Edge"/>
    public Edge(Guid id, string name, string shortName, Node startNode, Node endNode) : base(id, name, shortName)
    {
        StartNode = startNode;
        EndNode = endNode;
        StartNodeId = startNode.Id;
        EndNodeId = endNode.Id;
    }

    /// <summary>
    /// For deserialization.
    /// </summary>
    public Edge() : base(Guid.Empty, null!, null!)
    {
        StartNode = null!;
        EndNode = null!;
    }

    /// <summary>
    /// Generates an Edge from a <see cref="CommonEdge"/>
    /// </summary>
    /// <param name="commonEdge">The <see cref="CommonEdge"/></param>
    /// <param name="nodes">The nodes of the same graph, keyed by their id.</param>
    public static Edge FromCommon(CommonEdge commonEdge, IReadOnlyDictionary<Guid, Node> nodes)
    {
        return new Edge(commonEdge.Id, commonEdge.Name, commonEdge.ShortName, nodes[commonEdge.StartNodeId], nodes[commonEdge.EndNodeId]);
    }
}