using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Models;

/// <summary>
/// Represents a AgvPosition related to a map.
/// </summary>
public record MapAgvPositions
{
    /// <summary>
    /// The CorrelationId of the Map.
    /// </summary>
    public Guid MapId { get; init; }

    /// <summary>
    /// The Map object.
    /// </summary>
    public Map? Map { get; init; }

    /// <summary>
    /// The PositionId of the Agv grid position.
    /// </summary>
    public Guid PositionId { get; init; }

    /// <summary>
    /// The Agv grid position object.
    /// </summary>
    public GridAgvPosition? AgvGridPosition { get; init; }

    /// <summary>
    /// EF.
    /// </summary>
    [UsedImplicitly]
    private MapAgvPositions()
    {
        Map = null!;
        AgvGridPosition = null!;
    }

    /// <inheritdoc cref="MapAgvPositions"/>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="map">The Map object.</param>
    /// <param name="positionId">The PositionId of the Agv grid position.</param>
    /// <param name="agvGridPosition">The Agv grid position object.</param>
    public MapAgvPositions(Guid mapId, Map? map, Guid positionId, GridAgvPosition? agvGridPosition)
    {
        MapId = mapId;
        Map = map;
        PositionId = positionId;
        AgvGridPosition = agvGridPosition;
    }
}