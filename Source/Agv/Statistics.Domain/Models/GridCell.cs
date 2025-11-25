using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a cell on a grid.
/// </summary>
public class GridCell
{
    /// <summary>
    /// The Id of the grid cell.
    /// </summary>
    public Guid CellId { get; init; }

    /// <summary>
    /// The Id of the grid to which the cell belongs.
    /// </summary>
    public Guid GridId { get; init; }

    /// <summary>
    /// The Grid object.
    /// </summary>
    public Grid? Grid { get; init; }

    /// <summary>
    /// The index of the grid cell in x direction.
    /// </summary>
    public int XIndex { get; init; }

    /// <summary>
    /// The index of the cell in y-direction.
    /// </summary>
    public int YIndex { get; init; }

    /// <summary>
    /// The minimum X-coordinate value of the cell.
    /// </summary>
    public decimal XMin { get; init; }

    /// <summary>
    /// The maximum X-coordinate value of the cell.
    /// </summary>
    public decimal XMax { get; init; }

    /// <summary>
    /// The minimum Y-coordinate value of the cell.
    /// </summary>
    public decimal YMin { get; init; }

    /// <summary>
    /// The maximum Y-coordinate value of the cell.
    /// </summary>
    public decimal YMax { get; init; }

    /// <summary>
    /// The list of AgvMapPositions object.
    /// </summary>
    public ICollection<MapGridCell>? MapGridCell { get; init; }

    /// <summary>
    /// The list of AgvError object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvError>? AgvErrors { get; set; }

    /// <summary>
    /// The list of GridCellStay object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<GridCellStay>? GridCellStays { get; set; }

    /// <summary>
    /// The list of AgvFieldViolation object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvFieldViolation>? AgvFieldViolations { get; set; }

    /// <summary>
    /// The list of Wi-Fi metrics.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvWifiMetrics>? AgvWifiMetrics { get; set; }

    /// <inheritdoc cref="GridCell"/>>
    public GridCell(int xIndex, int yIndex, decimal xMin, decimal xMax, decimal yMin, decimal yMax, Grid grid)
    {
        CellId = Guid.NewGuid();
        XIndex = xIndex;
        YIndex = yIndex;
        XMin = xMin;
        XMax = xMax;
        YMin = yMin;
        YMax = yMax;
        GridId = grid.GridId;
        Grid = grid;
        MapGridCell = new List<MapGridCell>();
        AgvErrors = new List<AgvError>();
        AgvFieldViolations = new List<AgvFieldViolation>();
    }

    /// <inheritdoc cref="GridCell"/>>
    [UsedImplicitly]
    public GridCell()
    {
        CellId = Guid.NewGuid();
    }
}