using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a GridCell related to a map.
/// </summary>
public record MapGridCell
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
    public Guid GridCellId { get; init; }

    /// <summary>
    /// The Agv grid position object.
    /// </summary>
    public GridCell? GridCell { get; init; }

    /// <summary>
    /// EF.
    /// </summary>
    [UsedImplicitly]
    private MapGridCell()
    {
        Map = null!;
        GridCell = null!;
    }

    /// <inheritdoc cref="MapGridCell"/>
    /// <param name="map">The Map object.</param>
    /// <param name="gridCell">The Agv grid position object.</param>
    public MapGridCell(Map map, GridCell gridCell)
    {
        MapId = map.MapId;
        Map = map;
        GridCellId = gridCell.CellId;
        GridCell = gridCell;
    }
}