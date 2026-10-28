using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A node of the graph.
/// </summary>
/// <param name="Id">The id of the node.</param>
/// <param name="Name">The name of the node.</param>
/// <param name="ShortName">The short name of the node.</param>
/// <param name="X">The x position in [m] in reference to the global origin.</param>
/// <param name="Y">The y position in [m] in reference to the global origin.</param>
/// <param name="LevelId">The id of the level the node is on.</param>
/// <param name="AgvStateIndices">The positions in <see cref="GraphDto.AgvStates"/> of the AGV states the node supports.</param>
/// <param name="Capabilities">The actions AGVs can run at the node.</param>
/// <param name="Status">The runtime state of the node.</param>
[PublicAPI]
public record NodeDto(Guid Id, string Name, string ShortName, double X, double Y, string? LevelId, IReadOnlyList<int> AgvStateIndices, IReadOnlyList<RoutingActionType> Capabilities, NodeStatusDto Status);