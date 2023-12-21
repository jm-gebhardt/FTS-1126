using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;

/// <summary>
/// The join entity for Map and Agv
/// </summary>
public record MapAgv
{
    /// <summary>
    /// The CorrelationId of the Map
    /// </summary>
    public Guid MapId { get; init; }

    /// <summary>
    /// The Map
    /// </summary>
    public Map? Map { get; set; }

    /// <summary>
    /// The CorrelationId of the Agv
    /// </summary>
    public Guid AgvId { get; init; }

    /// <summary>
    /// The Agv
    /// </summary>
    public Agv.Agv? Agv { get; set; }

    /// <summary>
    /// EF
    /// </summary>
    [UsedImplicitly]
    private MapAgv()
    {
        Map = null!;
        Agv = null!;
    }

    /// <inheritdoc cref="MapAgv"/>
    /// <param name="mapId"></param>
    /// <param name="map"></param>
    /// <param name="agvId"></param>
    /// <param name="agv"></param>
    public MapAgv(Guid mapId, Map? map, Guid agvId, Agv.Agv? agv)
    {
        MapId = mapId;
        Map = map;
        AgvId = agvId;
        Agv = agv;
    }
}