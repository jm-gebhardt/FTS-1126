using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Trajectory described as a NURBS.
/// </summary>
[PublicAPI]
public record Trajectory
{
    /// <summary>
    /// Defines the number of control points that influence any given point on the curve.
    /// </summary>
    public int Degree { get; set; }

    /// <summary>
    /// Sequence of parameter values that determine where and how the control points affect the NURBS curve.
    /// </summary>
    public IList<float> KnotVector { get; set; } = [];

    /// <summary>
    /// List of control points of the NURBS, which includes the beginning and end point.
    /// </summary>
    public IList<ControlPoint> ControlPoints { get; set; } = [];
}

/// <summary>
/// Control point of a NURBS trajectory.
/// </summary>
[PublicAPI]
public record ControlPoint
{
    /// <summary>
    /// The weight with which this control point pulls on the curve.
    /// When not defined, the default will be 1.0.
    /// </summary>
    public double? Weight { get; set; }

    /// <summary>X coordinate in meters.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; set; }
}