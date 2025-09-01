using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Extensions;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Extensions;

[TestClass]
public class NodesExtensionTest
{
    [TestMethod]
    public void ToGrid_1x1_ReturnsGrid()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        const float step = 1;
        const string name = "grid";
        var grid = graph.Nodes.ToGrid(xStep: step, yStep: step, name);
        grid.GridName.ShouldBe(name);
        grid.XStep.ShouldBe(step);
        grid.YStep.ShouldBe(step);
        grid.XStart.ShouldBe(graph.Nodes.Min(n => n.X));
        grid.YStart.ShouldBe(graph.Nodes.Min(n => n.Y));
        grid.XSize.ShouldBe(5);
        grid.YSize.ShouldBe(6);
    }
}