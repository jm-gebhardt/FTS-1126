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
        Area = area.Select(p => new PolygonPoint(p.X, p.Y, Id)).ToList();
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
    public ICollection<PolygonPoint> Area { get; init; }

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