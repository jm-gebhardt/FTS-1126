using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class ExportedGraphGeneratorTest
{
    [TestMethod]
    public void Generate_ValidData_CreatesGraph()
    {
        var exportedGraph = ExportedGraphHelper.GenerateDefaultGraph();
        var fixture = new ExportedGraphGeneratorTestFixture()
            .WithExportedGraph(exportedGraph);
        var graph = fixture.Generator.Generate(Guid.NewGuid());

        int stations = graph.Nodes.Count(n => n.Type != AgvGraph.NodeType.WayPoint);
        graph.Edges.Count.ShouldBe(exportedGraph.Segments.Count + 2*stations);
        graph.Nodes.Count.ShouldBe(exportedGraph.Nodes.Count);
    }

    [TestMethod]
    public void Generate_RealData_CreatesGraph()
    {
        var fixture = new ExportedGraphGeneratorTestFixture(useRealData: true);
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        var exportedGraph = fixture.GetRealGraph();

        graph.Edges.Count.ShouldBeGreaterThanOrEqualTo(exportedGraph.Segments.Count);
        graph.Nodes.Count.ShouldBe(exportedGraph.Nodes.Count);
    }

    [TestMethod]
    public void Generate_RealDataConfigFromYaml_CreatesGraph()
    {
        var config = new KarisExportConfiguration();
        config.LoadFromYaml();
        var fixture = new ExportedGraphGeneratorTestFixture(useRealData: true, configuration:config);
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        var exportedGraph = fixture.GetRealGraph();

        graph.Edges.Count.ShouldBeGreaterThanOrEqualTo(exportedGraph.Segments.Count);
        graph.Nodes.Count.ShouldBe(exportedGraph.Nodes.Count);
    }

    [TestMethod]
    public void Generate_RenameWaypoints_WaypointsRenamed()
    {
        var config = new KarisExportConfiguration
        {
            RenameWaypoints = true
        };
        var fixture = new ExportedGraphGeneratorTestFixture(useRealData: true, configuration: config);
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        var exportedGraph = fixture.GetRealGraph();

        exportedGraph.Nodes.Any(n => n.Name.StartsWith("WP")).ShouldBeFalse();
        graph.Nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint).All(n => n.Id.StartsWith("WP")).ShouldBeTrue();
    }

    [TestMethod]
    public void Generate_DoNotRenameWaypoints_WaypointsNotRenamed()
    {
        var config = new KarisExportConfiguration
        {
            RenameWaypoints = false
        };
        var fixture = new ExportedGraphGeneratorTestFixture(useRealData: true, configuration: config);
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        var exportedGraph = fixture.GetRealGraph();

        exportedGraph.Nodes.Any(n => n.Name.StartsWith("WP")).ShouldBeFalse();
        graph.Nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint).Any(n => n.Id.StartsWith("WP")).ShouldBeFalse();
    }

    [TestMethod]
    public void Generate_UseRegex_NodeTypeIdentified()
    {
        var exportedGraph = ExportedGraphHelper.GenerateDefaultGraph(waypointStart:"wayPoint");
        var config = new KarisExportConfiguration
        {
            RenameWaypoints = false,
            WayPointRegexPattern = "^(wayPoint)"
        };
        var fixture = new ExportedGraphGeneratorTestFixture(configuration: config).WithExportedGraph(exportedGraph);
        var graph = fixture.Generator.Generate(Guid.NewGuid());
        graph.Nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint).All(n => n.Id.StartsWith("wayPoint")).ShouldBeTrue();
    }
}