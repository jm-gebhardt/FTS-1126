namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a node in the graph.
/// </summary>
public record Node : GraphResource
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

    /// <inheritdoc cref="Node"/>
    public Node(Guid id, string name, string shortName, double x, double y, string? levelId) : base(id, name, shortName)
    {
        X = x;
        Y = y;
        LevelId = levelId;
    }
}