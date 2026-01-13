using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Test.Helper;

public static class GridGenerator
{
    public static Grid GenerateGrid(string gridName = "a grid", decimal xStep = 2, decimal yStep = 2) =>
        new(gridName, xStep, yStep);
}