using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Trajectory described as a NURBS.
/// </summary>
[PublicAPI]
public record MqttTrajectory
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
    public IList<MqttControlPoint> ControlPoints { get; set; } = [];
}

/// <summary>
/// Control point of a NURBS trajectory.
/// </summary>
[PublicAPI]
public record MqttControlPoint
{
    /// <summary>X coordinate in meters.</summary>
    public float X { get; set; }

    /// <summary>Y coordinate in meters.</summary>
    public float Y { get; set; }

    /// <summary>
    /// The weight with which this control point pulls on the curve.
    /// When not defined, the default will be 1.0.
    /// </summary>
    public float? Weight { get; set; }
}
