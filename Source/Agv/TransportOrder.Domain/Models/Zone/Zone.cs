using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

/// <summary>
/// A zone.
/// </summary>
public class Zone : IEquatable<Zone>
{
    /// <summary>
    /// The correlation id of the zone.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The name of the zone.
    /// </summary>
    public string ZoneName { get; init; }

    /// <summary>
    /// The polygon that defines the zone boundaries.
    /// </summary>
    public ICollection<ZonePolygonPoint> Polygon { get; init; }

    /// <summary>
    /// The capacity of the zone.
    /// </summary>
    public int Capacity { get; init; }

    /// <inheritdoc cref="Zone"/>>
    public Zone(ICollection<ZonePolygonPoint> polygon, string zoneName, Guid correlationId, int capacity)
    {
        Polygon = polygon;
        ZoneName = zoneName;
        Capacity = capacity;
        CorrelationId = correlationId;
    }

    /// <inheritdoc cref="Zone"/>>
    public Zone()
    {
        CorrelationId = Guid.NewGuid();
        Polygon = [];
        ZoneName = string.Empty;
    }

    /// <summary>
    /// Check if the agv position is within a zone.
    /// </summary>
    /// <param name="position">The position of the agv.</param>
    /// <returns>True, if it is inside the zone.</returns>
    [PublicAPI]
    public bool IsAgvInside(AgvPosition position)
    {
        return Polygon.PointIsInPolygon(position.X, position.Y);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public bool Equals(Zone? other)
    {
        if (ZoneName != other?.ZoneName)
        {
            return false;
        }

        if (Capacity != other.Capacity)
        {
            return false;
        }

        return Polygon.OrderBy(p => p.Index).SequenceEqual(other.Polygon.OrderBy(p => p.Index));
    }
}