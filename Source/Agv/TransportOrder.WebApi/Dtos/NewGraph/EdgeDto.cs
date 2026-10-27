using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// An edge of the graph.
/// </summary>
/// <param name="Id">The id of the edge.</param>
/// <param name="Name">The name of the edge.</param>
/// <param name="ShortName">The short name of the edge.</param>
/// <param name="StartNodeId">The id of the node the edge starts at.</param>
/// <param name="EndNodeId">The id of the node the edge ends at.</param>
/// <param name="AgvStateIndices">The positions in <see cref="GraphDto.AgvStates"/> of the AGV states the edge supports.</param>
/// <param name="Trajectories">The distinct trajectories of the edge, each with the AGV states driving it.</param>
[PublicAPI]
public record EdgeDto(Guid Id, string Name, string ShortName, Guid StartNodeId, Guid EndNodeId, IReadOnlyList<int> AgvStateIndices, IReadOnlyList<TrajectoryDto> Trajectories);