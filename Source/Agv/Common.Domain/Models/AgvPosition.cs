// ReSharper disable ConvertToPrimaryConstructor

using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Domain.Models;

/// <summary>
/// AGV position information
/// </summary>
[PublicAPI]
public record AgvPosition
{
    /// <summary>
    /// Returns true, when the position is known and valid.
    /// </summary>
    public bool PositionInitialized { get; init; }

    /// <summary>
    /// Current X-position [m].
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Current Y-position [m].
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Current Z-position [m].
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Current angle (rotation) [rad].
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// Deviation from the responded position [m].
    /// </summary>
    public float? DeviationRange { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="AgvPosition"/>
    /// </summary>
    public AgvPosition(bool positionInitialized, float x, float y, float z, float theta, float? deviationRange = null)
    {
        PositionInitialized = positionInitialized;
        X = x;
        Y = y;
        Z = z;
        Theta = theta;
        DeviationRange = deviationRange;
    }

    /// <summary>
    /// Simple string representation (e.g. for logging)
    /// </summary>
    public override string ToString()
    {
        return $"[X={X:F3}/Y={Y:F3}/Theta={Theta:F3}]";
    }
}