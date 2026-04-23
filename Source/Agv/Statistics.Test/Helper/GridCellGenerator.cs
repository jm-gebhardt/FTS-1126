using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Test.Helper;

public static class GridCellGenerator
{
    public static GridCell GenerateGridCell(
        int xIndex = 1,
        int yIndex = 1,
        decimal xMin = 1,
        decimal xMax = 2,
        decimal yMin = 1,
        decimal yMax = 2,
        Grid? grid = null) =>
        new(xIndex, yIndex, xMin, xMax, yMin, yMax, grid ?? GridGenerator.GenerateGrid());
}