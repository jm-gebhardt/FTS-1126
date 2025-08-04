using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Represents the map message content.
/// </summary>
[UsedImplicitly]
public record Map
{
    /// <summary>
    /// Gets the Correlation Id of the map.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the Map Id.
    /// </summary>
    public string MapId { get; init; }

    /// <summary>
    /// Background metadata of the map.
    /// </summary>
    public BackgroundInfo BackgroundInfo { get; init; }

    /// <summary>
    /// Gets the list of AGVs (Automated Guided Vehicles) using this map.
    /// </summary>
    public Guid[] RelatedAgvs { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private Map()
    {
        BackgroundInfo = null!;
        MapId = null!;
        RelatedAgvs = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> record with specified properties.
    /// </summary>
    /// <param name="correlationId">The Correlation Id of the map.</param>
    /// <param name="mapId">The Map Id.</param>
    /// <param name="backgroundInfo">The background metadata of the map.</param>
    /// <param name="relatedAgvs">The list of AGVs using this map.</param>
    public Map(Guid correlationId, string mapId, BackgroundInfo backgroundInfo, Guid[] relatedAgvs)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        BackgroundInfo = backgroundInfo;
        RelatedAgvs = relatedAgvs;
    }
}