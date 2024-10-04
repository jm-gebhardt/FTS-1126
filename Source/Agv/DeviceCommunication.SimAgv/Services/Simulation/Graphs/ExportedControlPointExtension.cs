using System.Numerics;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Provides extension methods for <see cref="ExportedControlPoint"/>
/// </summary>
public static class ExportedControlPointExtension
{
    /// <summary>
    /// Invert the y-coordinate of the control point.
    /// </summary>
    /// <param name="point">The control point.</param>
    /// <returns></returns>
    public static ExportedControlPoint InvertY(this ExportedControlPoint point)
    {
        return point with { Y = -point.Y };
    }

    /// <summary>
    /// Get the <see cref="Vector2"/> representation of a control point.
    /// </summary>
    /// <param name="point">The control point</param>
    /// <returns>Vector to using x, y coordinates of the control point.</returns>
    public static Vector2 AsVector2(this ExportedControlPoint point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }
}