using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

/// <summary>
/// A trigger polygon point
/// </summary>
public class TriggerPolygonPoint : PolygonPoint
{
    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    public TriggerPolygonPoint(double x, double y, Guid locationId) : base(x, y, locationId) { }

    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    public TriggerPolygonPoint()
    {
    }
}