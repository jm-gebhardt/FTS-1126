using Gebhardt.Acc.Statistics.Domain.Extensions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Extensions;

[TestClass]
public class GridExtensionsTest
{
    [TestMethod]
    [DataRow(0, 0, 0, 0)]
    [DataRow(0.5, 0.5, 0, 0)]
    [DataRow(0.99, 0.99, 0, 0)]
    [DataRow(1, 0.99, 1, 0)]
    [DataRow(1, 1, 1, 1)]
    [DataRow(-1, 1, -1, 1)]
    [DataRow(-1, -1, -1, -1)]
    public void GetGridCellForPosition_DeterminesProperGridCell(double x, double y, int expectedXIndex, int expectedYIndex)
    {
        const decimal step = 1;
        const string name = "grid";
        var grid = new Grid(name, step, step);
        grid.GridName.ShouldBe(name);
        grid.XStep.ShouldBe(step);
        grid.YStep.ShouldBe(step);

        var gridCell = grid.GetGridCellForPosition((decimal) x, (decimal) y);

        gridCell.XIndex.ShouldBe(expectedXIndex);
        gridCell.XMin.ShouldBe(expectedXIndex);
        gridCell.XMax.ShouldBe(expectedXIndex + step);

        gridCell.YIndex.ShouldBe(expectedYIndex);
        gridCell.YMin.ShouldBe(expectedYIndex);
        gridCell.YMax.ShouldBe(expectedYIndex + step);
    }
}