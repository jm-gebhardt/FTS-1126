using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents an edge in a graph, connecting two nodes.
/// </summary>
[PublicAPI]
public record Edge : GraphResource
{
    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public Guid StartNodeId { get; }

    /// <summary>
    /// End node of the edge.
    /// </summary>
    public Guid EndNodeId { get; }

    /// <summary>
    /// List of routing edges associated with this edge.
    /// </summary>
    public List<RoutingEdge> RoutingEdges { get; init; } = [];

    /// <inheritdoc cref="Edge"/>
    public Edge(Guid id, string name, string shortName, Guid startNodeId, Guid endNodeId) : base(id, name, shortName)
    {
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
    }
}