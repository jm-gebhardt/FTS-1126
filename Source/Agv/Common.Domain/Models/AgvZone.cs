using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// The zone of an agv.
/// </summary>
[PublicAPI]
public record AgvZone
{
    /// <summary>
    /// The polygon that defines the zone boundaries.
    /// </summary>
    public ICollection<PolygonPoint> Polygon { get; init; }

    /// <summary>
    /// The id of the zone.
    /// </summary>
    public string ZoneId { get; set; }

    /// <summary>
    /// The capacity of the zone.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// The agvs currently occupying the zone.
    /// </summary>
    public ICollection<string> Occupancy { get; set; }

    /// <inheritdoc cref="AgvZone"/>>
    public AgvZone(string zoneId, int capacity, ICollection<PolygonPoint> polygon, ICollection<string> occupancy)
    {
        ZoneId = zoneId;
        Capacity = capacity;
        Polygon = polygon;
        Occupancy = occupancy;
    }
}