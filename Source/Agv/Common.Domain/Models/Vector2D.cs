using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// A two-dimensional vector using double precision coordinates.
/// </summary>
[PublicAPI]
public record Vector2D
{
    private const double Tolerance = 1e-9;

    /// <summary>
    /// X-component of the vector.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y-component of the vector.
    /// </summary>
    public double Y { get; init; }

    /// <inheritdoc cref="Vector2D"/>
    /// <param name="x">X-component of the vector.</param>
    /// <param name="y">Y-component of the vector.</param>
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc cref="Vector2D"/>
    public Vector2D()
    {
    }

    /// <summary>
    /// Length (magnitude) of the vector.
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Angle of the vector to the X axis.
    /// </summary>
    public double Angle => Math.Atan2(Y, X);

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    public static Vector2D operator +(Vector2D left, Vector2D right)
        => new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Subtracts <paramref name="right"/> from <paramref name="left"/> component-wise.
    /// </summary>
    public static Vector2D operator -(Vector2D left, Vector2D right)
        => new(left.X - right.X, left.Y - right.Y);

    /// <summary>
    /// Negates the vector.
    /// </summary>
    public static Vector2D operator -(Vector2D vector)
        => new(-vector.X, -vector.Y);

    /// <summary>
    /// Scales the vector by <paramref name="scalar"/>.
    /// </summary>
    public static Vector2D operator *(Vector2D vector, double scalar)
        => new(vector.X * scalar, vector.Y * scalar);

    /// <summary>
    /// Scales the vector by <paramref name="scalar"/>.
    /// </summary>
    public static Vector2D operator *(double scalar, Vector2D vector)
        => vector * scalar;

    /// <summary>
    /// Divides the vector by <paramref name="scalar"/>.
    /// </summary>
    public static Vector2D operator /(Vector2D vector, double scalar)
        => new(vector.X / scalar, vector.Y / scalar);

    /// <summary>
    /// Returns a normalized (unit length) copy of this vector.
    /// </summary>
    /// <returns>A vector with the same direction and length 1, or a zero vector if this vector has zero length.</returns>
    public Vector2D Normalize()
    {
        double length = Length;

        return length <= Tolerance ? new Vector2D(0.0, 0.0) : new Vector2D(X / length, Y / length);
    }
    
    /// <summary>
    /// Determines whether this <see cref="Vector2D"/> is equal to <paramref name="other"/>, allowing for a
    /// small floating point tolerance.
    /// </summary>
    /// <param name="other">The other <see cref="Vector2D"/> to compare against.</param>
    /// <returns><see langword="true"/> if both vectors are equal within tolerance; otherwise <see langword="false"/>.</returns>
    public virtual bool Equals(Vector2D? other)
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
