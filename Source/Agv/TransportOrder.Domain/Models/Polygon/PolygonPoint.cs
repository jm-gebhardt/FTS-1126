using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

/// <summary>
/// A point defining the polygon.
/// </summary>
[PublicAPI]
public class PolygonPoint
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
    /// The y coordinate of the point.
    /// </summary>
    public double Y { get; set; }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint(double x, double y, Guid locationId)
    {
        Id = Guid.NewGuid();
        LocationId = locationId;
        X = x;
        Y = y;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint(double x, double y)
    {
        Id = Guid.NewGuid();
        LocationId = Guid.Empty;
        X = x;
        Y = y;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint()
    {
        Id = Guid.NewGuid();
        LocationId = Guid.NewGuid();
    }
}