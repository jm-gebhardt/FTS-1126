using JetBrains.Annotations;
using System.Numerics;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// A polygon point.
/// </summary>
[PublicAPI]
public record PolygonPoint
{
    private const double Tolerance = 1e-9;

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
    /// Add the Vector2D to the point
    /// </summary>
    public static PolygonPoint operator +(PolygonPoint point, Vector2D vector)
        => new(point.X + vector.X, point.Y + vector.Y);
    
    /// <summary>
    /// Calculates the difference vector between two <see cref="PolygonPoint"/>s.
    /// </summary>
    public static Vector2D operator -(PolygonPoint point1, PolygonPoint point2)
        => new(point1.X - point2.X, point1.Y - point2.Y);

    /// <summary>
    /// Converts this <see cref="PolygonPoint"/> to a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 ToVector2()
    {
        return new Vector2((float)X, (float)Y);
    }

    /// <summary>
    /// Determines whether this <see cref="PolygonPoint"/> is equal to <paramref name="other"/>, allowing for a
    /// small floating point tolerance.
    /// </summary>
    /// <param name="other">The other <see cref="PolygonPoint"/> to compare against.</param>
    /// <returns><see langword="true"/> if both points are equal within <see cref="Tolerance"/>; otherwise <see langword="false"/>.</returns>
    public virtual bool Equals(PolygonPoint? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Math.Abs(X - other.X) <= Tolerance && Math.Abs(Y - other.Y) <= Tolerance;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}