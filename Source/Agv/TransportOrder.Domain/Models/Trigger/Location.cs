using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

/// <summary>
/// A location of a trigger.
/// </summary>
[PublicAPI]
public class Location
{
    /// <summary>
    /// The id of the location.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The trigger the location belongs to.
    /// </summary>
    public Guid TriggerId { get; set; }

    /// <summary>
    /// The edge id, if set.
    /// </summary>
    public Guid? EdgeId { get; set; }

    /// <summary>
    /// The node id, if set.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// The polygon, if set.
    /// </summary>
    public ICollection<TriggerPolygonPoint> Polygon { get; set; }

    /// <summary>
    /// The id of the map the location is on.
    /// </summary>
    public Guid? MapId { get; set; } 

    /// <inheritdoc cref="Location"/>>
    public Location()
    {
        Id = Guid.NewGuid();
        Polygon = [];
        MapId =  Guid.NewGuid();
        TriggerId = Guid.NewGuid();
    }

    /// <inheritdoc cref="Location"/>>
    public Location(string nodeId, Guid? mapId, Guid triggerId )
    {
        Id = Guid.NewGuid();
        Polygon = [];
        NodeId = nodeId;
        TriggerId = triggerId;
        MapId =  mapId ?? null;
    }

    /// <inheritdoc cref="Location"/>>
    public Location(Guid edgeId, Guid? mapId, Guid triggerId)
    {
        Id = Guid.NewGuid();
        Polygon = [];
        EdgeId = edgeId;
        MapId =  mapId ?? null;
        TriggerId = triggerId;
    }

    /// <inheritdoc cref="Location"/>>
    public Location(ICollection<PolygonPoint> polygon, Guid? mapId, Guid triggerId)
    {
        Id = Guid.NewGuid();
        Polygon = polygon.Select(p => new TriggerPolygonPoint(p.X, p.Y, Id)).ToList();
        MapId =  mapId ?? null;
        TriggerId = triggerId;
    }

    /// <summary>
    /// True, if a position is inside a polygon location.
    /// </summary>
    /// <param name="position">The agv position.</param>
    /// <returns>True, if a position is inside a polygon location.</returns>
    public bool IsPositionAtLocation(AgvPosition position)
    {
        return Polygon.PointIsInPolygon(position.X, position.Y);
    }
}
