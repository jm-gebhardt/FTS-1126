// ReSharper disable ConvertToPrimaryConstructor

using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// AGV position information
/// </summary>
[PublicAPI]
public record AgvPosition
{
    /// <summary>
    /// Returns true, when the position is known and valid.
    /// </summary>
    public bool PositionInitialized { get; }

    /// <summary>
    /// Current X-position [m].
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Current Y-position [m].
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Current Z-position [m].
    /// </summary>
    public float Z { get; }
    /// <summary>
    /// Current angle (rotation) [rad].
    /// </summary>
    public float Theta { get; }

    /// <summary>
    /// Deviation from the responded position [m].
    /// </summary>
    public float? DeviationRange { get; }

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
    public override string ToString() => $"[X={X}/Y={Y}/Z={Z}]";
}