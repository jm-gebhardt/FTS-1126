using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;

/// <summary>
/// The restricted area entity.
/// </summary>
public class RestrictedArea: IEquatable<RestrictedArea>
{
    /// <inheritdoc cref="RestrictedArea"/>>
    public RestrictedArea(ICollection<PolygonPoint> area)
    {
        Id = Guid.NewGuid();
        Area = area.Select(p => new RestrictedAreaPolygonPoint(p.X, p.Y, p.Index, Id)).ToList();
    }

    /// <inheritdoc cref="RestrictedArea"/>>
    public RestrictedArea()
    {
        Id = Guid.NewGuid();
        Area = [];
    }

    /// <summary>
    /// The id of the restricted area.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The (restricted) area.
    /// </summary>
    public ICollection<RestrictedAreaPolygonPoint> Area { get; init; }

    /// <summary>
    /// Update the area.
    /// </summary>
    /// <param name="newArea">The area.</param>
    public void UpdateArea(ICollection<PolygonPoint> newArea)
    {
        List<Guid> oldAreaPoints = Area.Select(a => a.Id).ToList();
        List<Guid> newAreaPoints = newArea.Select(a => a.Id).ToList();

        List<PolygonPoint> newPoints = newArea.Where(a => !oldAreaPoints.Contains(a.Id)).ToList();
        foreach (var newPoint in newPoints)
        {
            var point = new RestrictedAreaPolygonPoint(newPoint.X, newPoint.Y, newPoint.Index, Id){Id = newPoint.Id};
            Area.Add(point);
        }

        List<RestrictedAreaPolygonPoint> pointsToRemove= Area.Where(a => !newAreaPoints.Contains(a.Id)).ToList();
        foreach (var point in pointsToRemove)
        {
            Area.Remove(point);
        }
    }

    /// <summary>
    /// True, if a point is inside a restricted area.
    /// </summary>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    /// <returns>True, if a position is inside a polygon location.</returns>
    public bool IsPositionInside(float x, float y)
    {
        return Area.PointIsInPolygon(x, y);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public bool Equals(RestrictedArea? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        foreach (var a in Area)
        {
            var otherArea = other.Area.SingleOrDefault(ar => ar.Index == a.Index);
            if (!a.Equals(otherArea))
            {
                return false;
            }
        }
        return Id.Equals(other.Id);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public override bool Equals(object? obj)
    {
        if (obj is null)
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
        return Equals((RestrictedArea)obj);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}