using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Control point of a NURBS trajectory.
/// </summary>
[PublicAPI]
public record ControlPoint
{
    /// <summary>
    /// The weight with which this control point pulls on the curve. When not defined, the default is 1.0. Range: [0.0 ... float64.max]
    /// </summary>
    public double? Weight { get; init; }

    /// <summary>
    /// X coordinate of the control point in meters.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y coordinate of the control point in meters.
    /// </summary>
    public double Y { get; init; }

    /// <inheritdoc cref="ControlPoint"/>
    public ControlPoint(double x, double y, double? weight = null)
    {
        X = x;
        Y = y;
        Weight = weight;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public ControlPoint()
    {
    }
}