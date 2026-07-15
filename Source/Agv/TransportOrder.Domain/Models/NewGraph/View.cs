using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// 
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
    public IReadOnlyCollection<Node> Nodes { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyCollection<Edge> Edges { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyCollection<Station> Stations { get; } = [];
}