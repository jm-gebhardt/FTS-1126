namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a grid with a cell width and height. The GridCells are then the actual cells of it.
/// </summary>
public class Grid
{
    /// <summary>
    /// The uid of the grid.
    /// </summary>
    public Guid GridId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The display name of the grid.
    /// </summary>
    public string GridName { get; init; }

    /// <summary>
    /// The grid cell width in x-direction.
    /// </summary>
    public decimal XStep { get; init; }

    /// <summary>
    /// The grid cell width in y-direction.
    /// </summary>
    public decimal YStep { get; init; }

    /// <summary>
    /// The list of GridCells related to this grid.
    /// </summary>
    public List<GridCell> GridCells { get; init; } = [];

    /// <summary>
    /// The list of Maps related to this grid.
    /// </summary>
    public List<Map>? Maps { get; init; } = [];

    /// <inheritdoc cref="Grid"/>
    public Grid(string gridName, decimal xStep, decimal yStep)
    {
        GridName = gridName;
        XStep = xStep;
        YStep = yStep;
    }
}