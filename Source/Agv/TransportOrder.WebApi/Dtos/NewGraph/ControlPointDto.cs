using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A control point of a NURBS trajectory.
/// </summary>
/// <param name="X">The x position in [m] in reference to the global origin.</param>
/// <param name="Y">The y position in [m] in reference to the global origin.</param>
/// <param name="Weight">The weight of the control point. Null means 1.</param>
[PublicAPI]
public record ControlPointDto(double X, double Y, double? Weight);