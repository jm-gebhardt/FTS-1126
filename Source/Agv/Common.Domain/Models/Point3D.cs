namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// A 3D point with an angle (rotation).
/// </summary>
public record Point3D
{
    /// <summary>
    /// X-position [m]
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Y-position [m]
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Z-position [m]
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Angle (rotation) [rad]
    /// </summary>
    public float Theta { get; init; }

    /// <inheritdoc cref="Point3D"/>
    /// <param name="x">X-position [m]</param>
    /// <param name="y">Y-position [m]</param>
    /// <param name="z">Z-position [m]</param>
    /// <param name="theta">Angle (rotation) [rad]</param>
    public Point3D(float x, float y, float z, float theta)
    {
        X = x;
        Y = y;
        Z = z;
        Theta = theta;
    }
}