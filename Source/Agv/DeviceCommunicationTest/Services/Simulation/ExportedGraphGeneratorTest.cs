using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class ExportedGraphGeneratorTest
{
    [TestMethod]
    public void Generate_RealData_CreatesGraph()
    {
        var fixture = new ExportedGraphGeneratorTestFixture();
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        var exportedGraph = fixture.GetRealGraph();

        graph.Edges.Count.ShouldBeGreaterThanOrEqualTo(exportedGraph.Edges.Count + exportedGraph.Stations.Count*2);
        graph.Nodes.Count.ShouldBe(exportedGraph.Nodes.Count + exportedGraph.Stations.Count);
        graph.Edges.Select(e => e.Id).Distinct().Count().ShouldBe(graph.Edges.Count);
    }
}