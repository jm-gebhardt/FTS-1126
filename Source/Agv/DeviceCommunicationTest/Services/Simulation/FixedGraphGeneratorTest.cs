using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class FixedGraphGeneratorTest
{
    [TestMethod]
    public void Length_DirectEdge_DistanceSourceToTarget()
    {
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(Guid.NewGuid());
        foreach (var directEdge in graph.Edges.Where(e => e.Variant == EdgeType.Direct))
        {
            var sourceNode = graph.Nodes.Single(n => n.Id == directEdge.FromNodeId);
            var targetNode = graph.Nodes.Single(n => n.Id == directEdge.ToNodeId);
            double expectedLength = Mathematics.EuclideanDistance(sourceNode, targetNode);
            directEdge.Length.ShouldBe(expectedLength);
        }
    }
}