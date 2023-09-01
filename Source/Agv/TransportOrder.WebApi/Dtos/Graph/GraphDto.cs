using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Model for drawing a graph in the UI
/// </summary>
/// <param name="Nodes">The list of nodes in the graph</param>
/// <param name="Edges">The list of edges in the graph.</param>
/// <param name="CorrelationId">CorrelationId</param>
[JetBrains.Annotations.PublicAPI]
public record GraphDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges, Guid CorrelationId) : CorrelatedBy<Guid>;