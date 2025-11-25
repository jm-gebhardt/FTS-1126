using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="Grid"/> class.
/// </summary>
public static class GridExtensions
{
    /// <summary>
    /// Determines the grid cell corresponding to a specific position on the grid.
    /// </summary>
    /// <param name="grid">The grid definition.</param>
    /// <param name="x">The x-coordinate of the position.</param>
    /// <param name="y">The y-coordinate of the position.</param>
    /// <returns>The grid cell where the specified position is inside.</returns>
    public static GridCell GetGridCellForPosition(this Grid grid, decimal x, decimal y)
    {
        int xIndex = (int) Math.Floor(x / grid.XStep);
        int yIndex = (int) Math.Floor(y / grid.YStep);
        decimal xMin = xIndex * grid.XStep;
        decimal xMax = xMin + grid.XStep;
        decimal yMin = yIndex * grid.YStep;
        decimal yMax = yMin + grid.YStep;
        var gridCell = new GridCell(xIndex, yIndex, xMin, xMax, yMin, yMax, grid);
        return gridCell;
    }
}