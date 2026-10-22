using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents an edge in a graph, connecting two nodes.
/// </summary>
[PublicAPI]
public record Edge : GraphResource
{
    /// <summary>
    /// EdgeId in the LIF.
    /// </summary>
    public string LifEdgeId { get; init; }
    
    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public Guid StartNodeId { get; init; }

    /// <summary>
    /// End node of the edge.
    /// </summary>
    public Guid EndNodeId { get; init; }

    /// <summary>
    /// List of routing edges associated with this edge.
    /// </summary>
    public List<RoutingEdge> RoutingEdges { get; init; } = [];

    /// <inheritdoc cref="Edge"/>
    public Edge(Guid id, string name, string shortName, string lifEdgeId, Guid startNodeId, Guid endNodeId) : base(id, name, shortName)
    {
        LifEdgeId = lifEdgeId;
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public Edge()
    {
        LifEdgeId = null!;
    }
}