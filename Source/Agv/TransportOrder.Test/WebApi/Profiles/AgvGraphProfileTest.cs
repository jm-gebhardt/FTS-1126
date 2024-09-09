using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Graph = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Graph;
using GraphEdge = Gebhardt.Acc.TransportOrder.Messages.MessageContent.GraphEdge;
using GraphNode = Gebhardt.Acc.TransportOrder.Messages.MessageContent.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvGraphProfileTest
{
    private const double FloatComparisonThreshold = 0.0001;

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly for direct edge type.
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvGraphIsMappedToGraph()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new() { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new() { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };

        var agvGraph = new Graph(nodes,
            new List<GraphEdge>
            {
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S1"), nodes.Single(e => e.Id == "D1"), EdgeType.Direct,
                    new List<EdgeCoefficient>(), 8.9443),
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S2"), nodes.Single(e => e.Id == "D2"), EdgeType.Direct,
                    new List<EdgeCoefficient>(), 8.9443)
            }, Guid.NewGuid());

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n =>
            n.Id == x.Id &&
            n.Type.ToString() == x.Type.ToString() &&
            Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
            Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
            Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));

        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.FromNodeId &&
                                                             e.To.Id == x.ToNodeId &&
                                                             Math.Abs(e.Length - x.Length) < FloatComparisonThreshold));
    }

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly for slanted edge type.
    /// </summary>
    [TestMethod]
    public void MapAgvGraphToGraphDto_Slanted_AllPropertiesMapped()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new() { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new() { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };

        var edge1 = Common.Domain.Models.Graphs.GraphEdge.CreateSlanted("S1", "D1", 1f, 90, 15d);
        var edge2 = Common.Domain.Models.Graphs.GraphEdge.CreateSlanted("S2", "D2", 1f, 45, 10d);

        var agvGraph = new Graph(nodes,
            new List<GraphEdge>
            {
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S1"), nodes.Single(e => e.Id == "D1"), edge1.Variant,
                    edge1.VariantParameters, 8.9443),
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S2"), nodes.Single(e => e.Id == "D2"), edge2.Variant,
                    edge2.VariantParameters, 8.9443)
            }, Guid.NewGuid());

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n =>
            n.Id == x.Id &&
            n.Type.ToString() == x.Type.ToString() &&
            Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
            Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
            Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));

        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.FromNodeId &&
                                                             e.To.Id == x.ToNodeId &&
                                                             e.Variant == x.Variant &&
                                                             e.VariantParameters == x.VariantParameters &&
                                                             Math.Abs(e.Length - x.Length) < FloatComparisonThreshold));
    }

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly for Bezier2 edge type.
    /// </summary>
    [TestMethod]
    public void MapAgvGraphToGraphDto_Bezier2_AllPropertiesMapped()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new() { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new() { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };

        var edge1 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier2("S1", "D1", 1f, 20, 15);
        var edge2 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier2("S2", "D2", 1f, 45, 10);

        var agvGraph = new Graph(nodes,
            new List<GraphEdge>
            {
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S1"), nodes.Single(e => e.Id == "D1"), edge1.Variant,
                    edge1.VariantParameters, 8.9443),
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S2"), nodes.Single(e => e.Id == "D2"), edge2.Variant,
                    edge2.VariantParameters, 8.9443)
            }, Guid.NewGuid());

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n =>
            n.Id == x.Id &&
            n.Type.ToString() == x.Type.ToString() &&
            Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
            Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
            Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));

        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.FromNodeId &&
                                                             e.To.Id == x.ToNodeId &&
                                                             e.Variant == x.Variant &&
                                                             e.VariantParameters == x.VariantParameters &&
                                                             Math.Abs(e.Length - x.Length) < FloatComparisonThreshold));
    }

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly for Bezier3 edge type.
    /// </summary>
    [TestMethod]
    public void MapAgvGraphToGraphDto_Bezier3_AllPropertiesMapped()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new() { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new() { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };

        var edge1 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier3("S1", "D1", 1f, 20, 15, 30, 25);
        var edge2 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier3("S2", "D2", 1f, 45, 10, 55, 20);
       
        var agvGraph = new Graph(nodes,
            new List<GraphEdge>
            {
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S1"), nodes.Single(e => e.Id == "D1"), edge1.Variant,
                    edge1.VariantParameters, 8.9443),
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S2"), nodes.Single(e => e.Id == "D2"), edge2.Variant,
                    edge2.VariantParameters, 8.9443)
            }, Guid.NewGuid());

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n =>
            n.Id == x.Id &&
            n.Type.ToString() == x.Type.ToString() &&
            Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
            Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
            Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));

        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.FromNodeId &&
                                                             e.To.Id == x.ToNodeId &&
                                                             e.Variant == x.Variant &&
                                                             e.VariantParameters == x.VariantParameters &&
                                                             Math.Abs(e.Length - x.Length) < FloatComparisonThreshold));
    }

    /// <summary>
    /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly for Bezier5 edge type.
    /// </summary>
    [TestMethod]
    public void MapAgvGraphToGraphDto_Bezier5_AllPropertiesMapped()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "S1", Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
            new() { Id = "D1", Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
            new() { Id = "S2", Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
            new() { Id = "D2", Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
        };

        var edge1 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier5("S1", "D1", 1f, [new EdgeCoefficient(20, 15), new EdgeCoefficient(30, 25), new EdgeCoefficient(40, 35), new EdgeCoefficient(50, 45)]);
        var edge2 = Common.Domain.Models.Graphs.GraphEdge.CreateBezier5("S1", "D1", 1f, [new EdgeCoefficient(45, 10), new EdgeCoefficient(55, 20), new EdgeCoefficient(65, 30), new EdgeCoefficient(75, 40)]);

        var agvGraph = new Graph(nodes,
            new List<GraphEdge>
            {
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S1"), nodes.Single(e => e.Id == "D1"), edge1.Variant,
                    edge1.VariantParameters, 8.9443),
                new(Guid.NewGuid(), nodes.Single(e => e.Id == "S2"), nodes.Single(e => e.Id == "D2"), edge2.Variant,
                    edge2.VariantParameters, 8.9443)
            }, Guid.NewGuid());

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());
        var graph = mapper.Map<GraphDto>(agvGraph);

        graph.ShouldNotBeNull();

        graph.Nodes.ShouldNotBeEmpty();
        graph.Edges.ShouldNotBeEmpty();

        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n =>
            n.Id == x.Id &&
            n.Type.ToString() == x.Type.ToString() &&
            Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
            Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
            Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));

        graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From.Id == x.FromNodeId &&
                                                             e.To.Id == x.ToNodeId &&
                                                             e.Variant == x.Variant &&
                                                             e.VariantParameters == x.VariantParameters &&
                                                             Math.Abs(e.Length - x.Length) < FloatComparisonThreshold));
    }
}