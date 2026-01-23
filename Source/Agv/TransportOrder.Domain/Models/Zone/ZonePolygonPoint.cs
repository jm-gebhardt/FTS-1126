using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

/// <summary>
/// A zone polygon point.
/// </summary>
public class ZonePolygonPoint : PolygonPoint, IEquatable<ZonePolygonPoint>
{
    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    public ZonePolygonPoint(double x, double y, int index, Guid locationId) : base(x, y, index, locationId)
    {
    }

    /// <inheritdoc cref="TriggerPolygonPoint"/>>
    [UsedImplicitly]
    public ZonePolygonPoint()
    {
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public bool Equals(ZonePolygonPoint? other)
    {
        if (!X.Equals(other?.X))
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
        return LocationId.Equals(other.LocationId);
    }
}