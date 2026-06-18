using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Control point of a NURBS trajectory.
/// </summary>
[PublicAPI]
public class ControlPoint
{
    /// <inheritdoc cref="ControlPoint"/>
    public ControlPoint(double x, double y, double? weight = null)
    {
        X = x;
        Y = y;
        Weight = weight;
    }

    /// <summary>
    /// The weight with which this control point pulls on the curve. When not defined, the default is 1.0. Range: [0.0 ... float64.max]
    /// </summary>
    public double? Weight { get; set; }

    /// <summary>
    /// X coordinate of the control point in meters.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate of the control point in meters.
    /// </summary>
    public double Y { get; set; }
}
