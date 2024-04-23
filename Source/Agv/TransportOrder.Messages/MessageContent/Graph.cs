using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a graph 
/// </summary>
[UsedImplicitly]
public record Graph : CorrelatedBy<Guid>
{
    /// <summary>The list of nodes in the graph.</summary>
    public List<GraphNode> Nodes { get; init; }

    /// <summary>The list of edges in the graph.</summary>
    public List<GraphEdge> Edges { get; init; }

    /// <summary>CorrelationId</summary>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Graph"/>
    /// <param name="nodes">The list of nodes in the graph.</param>
    /// <param name="edges">The list of edges in the graph.</param>
    /// <param name="correlationId">CorrelationId</param>
    public Graph(List<GraphNode> nodes, List<GraphEdge> edges, Guid correlationId)
    {
        Nodes = nodes;
        Edges = edges;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private Graph()
    {
        Nodes = null!;
        Edges = null!;
    }
}