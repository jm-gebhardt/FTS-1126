using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class AgvGraphProfileTest
{
    private const double FloatComparisonThreshold = 0.0001;

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvGraphIsMappedToGraph()
    {
        const string mapName = "Map#1";
        var agvGraph = new AgvGraph(Guid.NewGuid(),
            new[]
            {
                new GraphNode("S1") {Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f},
                new GraphNode("D1") {Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f},
                new GraphNode("S2") {Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f},
                new GraphNode("D2") {Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f}
            },
            new[]
            {
                new GraphEdge("S1", "D1", 8.944f), // Just take the distance
                new GraphEdge("S2", "D2", 10.77f) // as metric.
            },
            mapName.ToGuid(),
            mapName);

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.CorrelationId.ShouldBe(agvGraph.CorrelationId);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n => 
                                                             n.Id == x.Id &&
                                                             n.Type.ToString() == x.Type.ToString() &&
                                                             Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
                                                             Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
                                                             Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));
        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.FromNodeId == x.From.Id && e.ToNodeId == x.To.Id));
    }
}