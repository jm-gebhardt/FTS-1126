using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a graph 
/// </summary>
/// <param name="Nodes">The list of nodes in the graph.</param>
/// <param name="Edges">The list of edges in the graph.</param>
/// <param name="CorrelationId">CorrelationId</param>
[UsedImplicitly]
public record Graph(List<GraphNode> Nodes, List<GraphEdge> Edges, Guid CorrelationId) : CorrelatedBy<Guid>;