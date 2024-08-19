using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;

/// <summary>
/// The restricted area entity.
/// </summary>
public class RestrictedArea
{
    /// <inheritdoc cref="RestrictedArea"/>>
    public RestrictedArea(ICollection<PolygonPoint> area)
    {
        Id = Guid.NewGuid();
        Area = area.Select(p => new RestrictedAreaPolygonPoint(p.X, p.Y, Id)).ToList();
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
            var point = new RestrictedAreaPolygonPoint(newPoint.X, newPoint.Y, Id){Id = newPoint.Id};
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
}