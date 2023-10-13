using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a graph edge.
/// </summary>
/// <param name="From">The from node.</param>
/// <param name="To">The to node.</param>
[UsedImplicitly]
public record GraphEdge(GraphNode From, GraphNode To);