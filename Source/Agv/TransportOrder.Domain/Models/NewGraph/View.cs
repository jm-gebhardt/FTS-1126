using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a view of a graph.
/// </summary>
[UsedImplicitly]
public class View
{
    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public string? Name { get; }

    //Area

    //IsDefault or convention that first is default?

    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyCollection<Node> Nodes { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyCollection<Edge> Edges { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyCollection<Station> Stations { get; } = [];
}