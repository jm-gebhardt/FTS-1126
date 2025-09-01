using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class BezierGraphGeneratorTest
{
    private static string[] GenerateNodeIds(string suffix, int count)
    {
        return Enumerable.Range(0, count).Select(idx => $"{suffix}{idx}").ToArray();
    }

    private const float South = (float)Math.PI;
    private const float North = 0f;

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(0, 0, 1)]
    [DataRow(0, 1, 1)]
    [DataRow(1, 0, 1)]
    [DataRow(1, 1, 1)]
    [DataRow(3, 1, 1)]
    [DataRow(1, 3, 1)]
    [DataRow(3, 3, 1)]
    [DataRow(2, 2, 2)]
    [DataRow(10, 10, 10)]
    public void Generate_TestGridOfDifferentSizes_AllSuccessful(
        int numberOfSourceNodes,
        int numberOfDestinationNodes,
        int gridRows)
    {
        string[] sources = GenerateNodeIds("S", numberOfSourceNodes);
        string[] destinations = GenerateNodeIds("D", numberOfDestinationNodes);
        var correlationId = Guid.NewGuid();

        var generator = new BezierGraphGenerator(sources, destinations, new GeneratedMapGenerator(), gridRows);
        var graph = generator.Generate(correlationId);

        int gridWidth = Math.Max(sources.Length, destinations.Length);

        graph.CorrelationId.ShouldBe(correlationId);
        graph.Nodes.Count.ShouldBe(sources.Length +
                                   destinations.Length +
                                   gridWidth * gridRows);

        int expectedNumberOfEdges = (
            sources.Length +
            destinations.Length +
            (gridWidth - 1) * gridRows + // horizontal
            gridWidth * (gridRows - 1) // vertical
        ) * 2;

        if (numberOfSourceNodes == 0 && numberOfDestinationNodes == 0)
        {
            expectedNumberOfEdges = 0;
        }

        graph.Edges.Count.ShouldBe(expectedNumberOfEdges);

        foreach (string s in sources)
        {
            graph.Nodes.Any(n => n.Id == s).ShouldBeTrue();
            graph.Nodes.First(n => n.Id == s).Theta.ShouldBe(North);
        }
        foreach (string d in destinations)
        {
            graph.Nodes.Any(n => n.Id == d).ShouldBeTrue();
            graph.Nodes.First(n => n.Id == d).Theta.ShouldBe(South);
        }
    }

    [TestMethod]
    public void Generate_TwoAgvs_DoNotRegenerate()
    {
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var serviceNodeTypes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        var generator = new BezierGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 1, serviceNodeTypes);
       
        var firstGraph = generator.Generate(Guid.NewGuid());
        var secondGraph = generator.Generate(Guid.NewGuid());
       
        firstGraph.Edges.ShouldBeEquivalentTo(secondGraph.Edges);
        firstGraph.Nodes.ShouldBeEquivalentTo(secondGraph.Nodes);
        firstGraph.CorrelationId.ShouldNotBe(secondGraph.CorrelationId);
    }

    [TestMethod]
    public void Generate_TwoServiceNode_AllEdgesAdded()
    {
        var serviceNodes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new BezierGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 3, serviceNodes);
        var graph = generator.Generate(Guid.NewGuid());

        string[][] expectedEdges =
        {
            // Grid
            new[] { "N000-000", "N001-000" },
            new[] { "N000-000", "N000-001" },
            new[] { "N001-000", "N001-001" },
            new[] { "N000-001", "N001-001" },
            new[] { "N000-001", "N000-002" },
            new[] { "N001-001", "N001-002" },
            new[] { "N000-002", "N001-002" },
            // Source
            new[] { "S0", "N000-000" },
            new[] { "S1", "N001-000" },
            // Destination
            new[] { "D0", "N000-002" },
            new[] { "D1", "N001-002" },
            // Service
            new[] { "Charge-0", "N000-000" },
            new[] { "Park-1", "N001-001" }
        };

        string[] graphEdges = graph.Edges.Select(e => $"{e.FromNodeId}-{e.ToNodeId}").ToArray();

        // Check for edges including bidirectional
        foreach (string[] edge in expectedEdges.Concat(expectedEdges.Reverse()))
        {
            string edgeName = $"{edge[0]}-{edge[1]}";
            graphEdges.FirstOrDefault(n => n == edgeName).ShouldBe(edgeName);
        }

        // 2*Expect for bidirectional
        graph.Edges.Count.ShouldBe(expectedEdges.Length * 2);
        graph.Edges.All(e => e.Variant == EdgeType.Bezier5).ShouldBeTrue();
    }

    [TestMethod]
    public void Generate_BidirectionalEdgesToStations_EdgesMirrored()
    {
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new BezierGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 3, []);
        var graph = generator.Generate(Guid.NewGuid());

        foreach (string station in (string[])[..sources, ..destinations])
        {
            var fromEdge = graph.Edges.Single(e => e.FromNodeId == station);
            var toEdge = graph.Edges.Single(e => e.ToNodeId == station);
            fromEdge.VariantParameters.ShouldBeEquivalentTo(toEdge.VariantParameters.Reverse().ToArray());
            fromEdge.Length.ShouldBe(toEdge.Length);
        }
    }

    [TestMethod]
    public void Generate_AllBidirectional_EdgesMirrored()
    {
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new BezierGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 3, []);
        var graph = generator.Generate(Guid.NewGuid());

        foreach (var node in graph.Nodes)
        {
            foreach(var fromEdge in graph.Edges.Where(e => e.FromNodeId == node.Id))
            {
                var toEdge = graph.Edges.SingleOrDefault(e => e.ToNodeId == fromEdge.FromNodeId && e.FromNodeId == fromEdge.ToNodeId);
                if (toEdge != null)
                {
                    fromEdge.VariantParameters.ShouldBeEquivalentTo(toEdge.VariantParameters.Reverse().ToArray());
                    fromEdge.Length.ShouldBe(toEdge.Length);
                }
            }
        }
    }
}