using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// 
/// </summary>
[UsedImplicitly]
public class Graph
{
    /// <summary>
    /// 
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

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