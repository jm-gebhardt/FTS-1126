using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Model for drawing a graph in the UI
/// </summary>
/// <param name="Nodes">The list of nodes in the graph</param>
/// <param name="Edges">The list of edges in the graph.</param>
[PublicAPI]
public record GraphDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);