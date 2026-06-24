using JetBrains.Annotations;
using System.Numerics;

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

    /// <summary>
    /// Translates a <see cref="PolygonPoint"/> by the given <paramref name="vector"/>.
    /// </summary>
    public static PolygonPoint operator +(PolygonPoint point, Vector2 vector)
        => new(point.X + vector.X, point.Y + vector.Y);

    /// <summary>
    /// Converts this <see cref="PolygonPoint"/> to a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 ToVector2()
    {
        return new Vector2((float)X, (float)Y);
    }
}