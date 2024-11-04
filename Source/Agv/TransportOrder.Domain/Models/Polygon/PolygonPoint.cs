using JetBrains.Annotations;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

/// <summary>
/// A point defining the polygon.
/// </summary>
[PublicAPI]
public class PolygonPoint : IEquatable<PolygonPoint>
{
    /// <summary>
    /// The id of the polygon point.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The location the polygon belongs to.
    /// </summary>
    public Guid LocationId { get; set; }

    /// <summary>
    /// The x coordinate of the point.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The index of the polygon point in the polygon.
    /// </summary>

    public int Index { get; set; }

    /// <summary>
    /// The y coordinate of the point.
    /// </summary>
    public double Y { get; set; }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint(double x, double y, int index, Guid locationId)
    {
        Id = Guid.NewGuid();
        LocationId = locationId;
        X = x;
        Y = y;
        Index = index;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint(double x, double y, int index)
    {
        Id = Guid.NewGuid();
        LocationId = Guid.Empty;
        X = x;
        Y = y;
        Index = index;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint()
    {
        Id = Guid.NewGuid();
        LocationId = Guid.NewGuid();
    }

    /// <inheritdoc />
    public bool Equals(PolygonPoint? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (Id != other.Id)
        {
            return false;
        }
        if (LocationId != other.LocationId)
        {
            return false;
        }
        if (!X.Equals(other.X))
        {
            return false;
        }
        if (!Y.Equals(other.Y))
        {
            return false;
        }
        if (!Index.Equals(other.Index))
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc />
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
        if (obj.GetType() != GetType())
        {
            return false;
        }
        return Equals((PolygonPoint)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, LocationId, X, Y, Index);
    }
}