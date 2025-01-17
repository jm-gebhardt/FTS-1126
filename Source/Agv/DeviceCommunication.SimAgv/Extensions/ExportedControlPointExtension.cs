using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

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

    /// <summary>
    /// Convert to <see cref="EdgeCoefficient"/>
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static EdgeCoefficient ToEdgeCoefficient(this ExportedControlPoint point)
    {
        return new EdgeCoefficient(point.X, -point.Y);
    }
}