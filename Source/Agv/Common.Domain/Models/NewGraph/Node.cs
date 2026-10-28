using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a node in the graph.
/// </summary>
[PublicAPI]
public record Node : GraphResource
{
    /// <summary>
    /// X position on the layout in reference to the global origin.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y position on the layout in reference to the global origin.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Identifier for the level the node is on.
    /// </summary>
    public string? LevelId { get; init; }
    
    /// <summary>
    /// MapId according to VDA5050/LIF.
    /// </summary>
    public string? MapId { get; init; }
    
    /// <summary>
    /// NodeId in the LIF.
    /// </summary>
    public string LifNodeId { get; init; }

    /// <summary>
    /// List of routing nodes associated with this node.
    /// </summary>
    public List<RoutingNode> RoutingNodes { get; init; } = [];

    /// <summary>
    /// List of routing edges associated with this node.
    /// </summary>
    public List<RoutingEdge> RoutingEdges { get; init; } = [];

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, string lifNodeId, double x, double y, string? levelId, string? mapId) : base(id, name, shortName)
    {
        LifNodeId = lifNodeId;
        X = x;
        Y = y;
        LevelId = levelId;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public Node()
    {
        LifNodeId = null!;
    }
}