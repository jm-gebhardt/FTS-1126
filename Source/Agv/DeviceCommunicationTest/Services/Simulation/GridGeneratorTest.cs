using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class GridGeneratorTest
{
    private const float East = (float) Math.PI / 2f;

    private const float West = 3f * (float) Math.PI / 2f;
    private const float South = (float) Math.PI;
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
    public void GenerateGrid_TestGridOfDifferentSizes_AllSuccessful(
        int numberOfSourceNodes,
        int numberOfDestinationNodes,
        int gridRows)
    {
        string[] sources = GenerateNodeIds("S", numberOfSourceNodes);
        string[] destinations = GenerateNodeIds("D", numberOfDestinationNodes);
        var correlationId = Guid.NewGuid();

        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), gridRows);
        var graph = generator.Generate(correlationId);

        int gridWidth = Math.Max(sources.Length, destinations.Length);

        graph.AgvCorrelationId.ShouldBe(correlationId);
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
            graph.Nodes.Any(n => n.Name == s).ShouldBeTrue();
            graph.Nodes.First(n => n.Name == s).Theta.ShouldBe(North);
        }
        foreach (string d in destinations)
        {
            graph.Nodes.Any(n => n.Name == d).ShouldBeTrue();
            graph.Nodes.First(n => n.Name == d).Theta.ShouldBe(South);
        }
    }

    /// <summary>
    /// Test adding service nodes
    /// </summary>
    [TestMethod]
    public void GenerateGrid_TwoServiceNode_Successful()
    {
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var serviceNodeTypes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 1, serviceNodeTypes);
        var graph = generator.Generate(Guid.NewGuid());

        graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Charge).ShouldBe(1);
        graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Park).ShouldBe(1);

        graph.Nodes.First(n => (AgvGraph.NodeType) n.Type == serviceNodeTypes.First()).Theta.ShouldBe(West);
        graph.Nodes.First(n => (AgvGraph.NodeType) n.Type == serviceNodeTypes.Last()).Theta.ShouldBe(East);
    }

    /// <summary>
    /// Test adding service nodes
    /// </summary>
    [TestMethod]
    public void GenerateGrid_FourServiceNode_Successful()
    {
        var serviceNodes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 2, serviceNodes);
        var graph = generator.Generate(Guid.NewGuid());

        graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Charge).ShouldBe(2);
        graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Park).ShouldBe(2);
    }

    /// <summary>
    /// Test adding service nodes
    /// </summary>
    [TestMethod]
    public void GenerateGrid_FourServiceNode_AllNodeIds_Unique()
    {
        var serviceNodes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 2, serviceNodes);
        var graph = generator.Generate(Guid.NewGuid());
        graph.Nodes.Count.ShouldBe(new HashSet<string>(graph.Nodes.Select(n => n.Name)).Count);
    }

    /// <summary>
    /// Test adding service nodes
    /// </summary>
    [TestMethod]
    public void GenerateGrid_FourServiceNode_AllEdges_Unique()
    {
        var serviceNodes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 2, serviceNodes);
        var graph = generator.Generate(Guid.NewGuid());
        graph.Edges.Count.ShouldBe(new HashSet<string>(graph.Edges.Select(n => $"{n.FromNodeId}.{n.ToNodeId}")).Count);
    }

    [TestMethod]
    public void GenerateGrid_TwoServiceNode_AllEdgesAdded()
    {
        var serviceNodes = new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park };
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 3, serviceNodes);
        var graph = generator.Generate(Guid.NewGuid());

        // 2*Expect for bidirectional
        graph.Edges.Count.ShouldBe(13 * 2);
    }

    [TestMethod]
    public void GenerateGrid_WithThreeServiceNode_GridLengthOne_ExpectIndexOutOfRangeException()
    {
        string[] sources = GenerateNodeIds("S", 2);
        string[] destinations = GenerateNodeIds("D", 2);
        var generator = new GridGraphGenerator(sources, destinations, new GeneratedMapGenerator(), 1,
            new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Charge }
        );
        Should.Throw<IndexOutOfRangeException>(() => generator.Generate(Guid.NewGuid()));
    }

    private static string[] GenerateNodeIds(string suffix, int count)
    {
        return Enumerable.Range(0, count).Select(idx => $"{suffix}{idx}").ToArray();
    }
}