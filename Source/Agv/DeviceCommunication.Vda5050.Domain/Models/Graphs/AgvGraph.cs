using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// Graph for an agv, contains collections
/// of <see cref="Node"/> and <see cref="Edge"/> the agv can access.
/// </summary>
[PublicAPI]
public record AgvGraph
{
    /// <summary>
    /// The correlationId of the Agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The nodes the agv can access.
    /// </summary>
    public ICollection<Node> Nodes { get; init; }

    /// <summary>
    /// The nodes the agv can access.
    /// </summary>
    public ICollection<Edge> Edges { get; init; }

    /// <inheritdoc cref="AgvGraph"/>
    public AgvGraph(Guid agvCorrelationId, ICollection<Node> nodes, ICollection<Edge> edges)
    {
        AgvCorrelationId = agvCorrelationId;
        Nodes = nodes;
        Edges = edges;
    }
}