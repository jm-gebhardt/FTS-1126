using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Position of a station.
/// </summary>
[PublicAPI]
public class StationPosition
{
    /// <inheritdoc cref="StationPosition"/>
    public StationPosition(double x, double y, double? theta = null)
    {
        X = x;
        Y = y;
        Theta = theta;
    }

    /// <summary>
    /// Orientation of the station. Unit: radians. Range: [-Pi ... Pi]
    /// </summary>
    public double? Theta { get; set; }

    /// <summary>
    /// X coordinate of the station in meters. Range: [float64.min ... float64.max]
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate of the station in meters. Range: [float64.min ... float64.max]
    /// </summary>
    public double Y { get; set; }
}
