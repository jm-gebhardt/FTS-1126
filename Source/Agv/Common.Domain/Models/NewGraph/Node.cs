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
    /// List of routing nodes associated with this node.
    /// </summary>
    public List<RoutingNode> RoutingNodes { get; init; } = [];

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, double x, double y, string? levelId) : base(id, name, shortName)
    {
        X = x;
        Y = y;
        LevelId = levelId;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public Node()
    {
    }
}