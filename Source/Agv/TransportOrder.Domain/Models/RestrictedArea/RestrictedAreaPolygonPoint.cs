using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;

/// <summary>
/// A restricted area polygon point.
/// </summary>
public class RestrictedAreaPolygonPoint: PolygonPoint
{
    /// <inheritdoc cref="RestrictedAreaPolygonPoint"/>>
    public RestrictedAreaPolygonPoint(double x, double y, Guid locationId) : base(x, y, locationId) { }

    /// <inheritdoc cref="RestrictedAreaPolygonPoint"/>>
    public RestrictedAreaPolygonPoint()
    {
    }
}