using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// A polygon point.
/// </summary>
[PublicAPI]
public record PolygonPoint
{
    /// <summary>
    /// X-position [m]
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y-position [m]
    /// </summary>
    public double Y { get; init; }

    /// <inheritdoc cref="PolygonPoint"/>
    /// <param name="x">X-position [m]</param>
    /// <param name="y">Y-position [m]</param>
    public PolygonPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint()
    {
    }
}