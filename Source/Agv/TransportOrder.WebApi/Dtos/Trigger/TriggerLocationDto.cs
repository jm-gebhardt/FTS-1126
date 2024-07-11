using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for the location of a trigger.
/// </summary>
[PublicAPI]
public record TriggerLocationDto
{
    /// <summary>
    /// The id of the location.
    /// </summary>
    public Guid Id { get; set; }

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
    public ICollection<PolygonPointDto> Polygon { get; set; }

    /// <summary>
    /// The id of the map the location is on.
    /// </summary>
    public Guid MapId { get; set; }

    /// <inheritdoc cref="TriggerLocationDto"/>>
    public TriggerLocationDto()
    {
        Id = Guid.NewGuid();
        Polygon = [];
        MapId = Guid.NewGuid();
    }
}