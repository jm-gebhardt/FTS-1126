using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for a location.
/// </summary>
[PublicAPI]
public record LocationDto
{
    /// <summary>
    /// The node id, if set.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// The edge id, if set.
    /// </summary>
    public Guid? EdgeId { get; set; }

    /// <summary>
    /// The polygon, if set.
    /// </summary>
    public ICollection<PolygonPointDto>? Polygon { get; set; }

    /// <summary>
    /// The optional configuration parameters at the location.
    /// </summary>
    public ICollection<ConfigurationParameterDto>? ConfigurationParameters { get; set; }

    /// <summary>
    /// The id of the map the location is on.
    /// </summary>
    public Guid? MapId { get; set; }

    /// <inheritdoc cref="TriggerLocationDto"/>>
    public LocationDto()
    {
        Polygon = [];
    }
}