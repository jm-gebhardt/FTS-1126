using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A NURBS trajectory of an edge. Without control points it defines a straight line between the edge's nodes.
/// </summary>
/// <param name="AgvStateIndices">The positions in <see cref="GraphDto.AgvStates"/> of the AGV states driving this trajectory.</param>
/// <param name="ControlPoints">The control points of the NURBS.</param>
/// <param name="Degree">The degree of the NURBS.</param>
/// <param name="KnotVector">The knot vector of the NURBS.</param>
[PublicAPI]
public record TrajectoryDto(IReadOnlyList<int> AgvStateIndices, IReadOnlyList<ControlPointDto> ControlPoints, int? Degree, IReadOnlyList<double> KnotVector);