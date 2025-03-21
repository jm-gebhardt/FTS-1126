using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

/// <summary>
/// A zone polygon point.
/// </summary>
public class ZonePolygonPoint: PolygonPoint
{
    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    public ZonePolygonPoint(double x, double y, int index, Guid locationId) : base(x, y, index, locationId) { }

    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    public ZonePolygonPoint()
    {
    }
}