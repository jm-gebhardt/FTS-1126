using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using GraphEdge = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.GraphEdge;
using GraphNode = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

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
        var edges = new List<GraphNode>
        {
            new()  { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new()  { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new()  { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };
        var agvGraph = new Graph(edges,
            new List<GraphEdge>
            {
                new(edges.Single( e=> e.Id == "S1"), edges.Single( e=> e.Id == "D1")),
                new(edges.Single( e=> e.Id == "S2"), edges.Single( e=> e.Id == "D2")) 
            }, Guid.NewGuid());

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
        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.From.Id && e.To.Id == x.To.Id));
    }
}