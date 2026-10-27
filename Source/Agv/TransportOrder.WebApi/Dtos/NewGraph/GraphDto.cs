using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The global graph with its nodes, edges and stations.
/// </summary>
/// <param name="Nodes">Nodes of the graph.</param>
/// <param name="Edges">Edges of the graph.</param>
/// <param name="Stations">Stations of the graph.</param>
/// <param name="AgvStates">All distinct AGV states.</param>
[PublicAPI]
public record GraphDto(IReadOnlyList<NodeDto> Nodes, IReadOnlyList<EdgeDto> Edges, IReadOnlyList<StationDto> Stations, IReadOnlyList<AgvTypeStateDto> AgvStates);