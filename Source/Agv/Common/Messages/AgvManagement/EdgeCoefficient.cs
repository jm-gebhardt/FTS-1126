using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// Coefficient for a <see cref="GraphEdge"/>.
/// </summary>
public class EdgeCoefficient : IEquatable<EdgeCoefficient>
{
    /// <summary>
    /// Scalar value.
    /// </summary>
    [UsedImplicitly]
    public double? Scalar { get; }

    /// <summary>
    /// X coordinate.
    /// </summary>
    [UsedImplicitly]
    public double? X { get; }

    /// <summary>
    /// Y coordinate.
    /// </summary>
    [UsedImplicitly]
    public double? Y { get; }

    /// <summary>
    /// Initializes a point <see cref="EdgeCoefficient"/> instance.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public EdgeCoefficient(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a scalar <see cref="EdgeCoefficient"/> instance.
    /// </summary>
    /// <param name="scalar"></param>
    public EdgeCoefficient(double scalar)
    {
        Scalar = scalar;
    }

    /// <summary>
    /// Checks if this <see cref="EdgeCoefficient"/> is a scalar.
    /// </summary>
    [UsedImplicitly]
    public bool IsScalar => Scalar != null;
    
    /// <summary>
    /// Checks if this <see cref="EdgeCoefficient"/> is a point.
    /// </summary>
    public bool IsPoint => X != null && Y != null;

    /// <summary>
    /// Equality check.
    /// </summary>
    public bool Equals(EdgeCoefficient? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Nullable.Equals(Scalar, other.Scalar) && Nullable.Equals(X, other.X) && Nullable.Equals(Y, other.Y);
    }

    /// <summary>
    /// Equality check.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != this.GetType())
        {
            return false;
        }
        return Equals((EdgeCoefficient)obj);
    }

    /// <summary>
    /// Get hashcode.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Scalar, X, Y);
    }

    /// <summary>
    /// Equality check.
    /// </summary>
    public static bool operator ==(EdgeCoefficient? left, EdgeCoefficient? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality check.
    /// </summary>
    public static bool operator !=(EdgeCoefficient? left, EdgeCoefficient? right)
    {
        return !Equals(left, right);
    }
}

