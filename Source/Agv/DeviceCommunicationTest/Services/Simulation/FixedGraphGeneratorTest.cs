using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graphs;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Maps;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

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