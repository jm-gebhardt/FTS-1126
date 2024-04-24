using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

[TestClass]
public class GraphEdgeTest
{
    [TestMethod]
    public void GetSections_DirectEdge_NoSectionsAndVirtualNodes()
    {
        var source = NodeGenerator.GenerateNode(nodeId:"source");
        var target = NodeGenerator.GenerateNode(nodeId:"target");
        var edge = GraphEdge.CreateDirect(source.Id, target.Id);
        (List<GraphEdge> sections, List<GraphNode> virtualNodes) = edge.GetSections(source, target);
        virtualNodes.ShouldBeEmpty();
        sections.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetSections_BezierEdge_NoSectionsAndVirtualNodes()
    {
        var source = NodeGenerator.GenerateNode(nodeId:"source");
        var target = NodeGenerator.GenerateNode(nodeId:"target");
        var bezier = new Bezier(new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1)
        });
        var edge = GraphEdge.CreateBezier1(source.Id, target.Id, (float)bezier.GetLength(100), 1, 0);
        (List<GraphEdge> sections, List<GraphNode> virtualNodes) = edge.GetSections(source, target);
        virtualNodes.ShouldBeEmpty();
        sections.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetSections_SlantedEdge_SectionsAndVirtualNodesCalculated()
    {
        const float west = 3f * (float)Math.PI / 2f;
        var source = NodeGenerator.GenerateNode(nodeId:"source", x:0, y:0, z:0);
        var target = NodeGenerator.GenerateNode(nodeId:"target", x:1, y:1, z:0);

        var edge = AgvGraphGenerator.CreateSlantedEdge(source.Id, target.Id, west, 1.0, [source, target]);
        (List<GraphEdge> sections, List<GraphNode> virtualNodes) = edge.GetSections(source, target);
        sections.Count.ShouldBe(3);
        sections.All(s => s.Variant == EdgeType.Direct).ShouldBeTrue();
        sections.Single(s => s.FromNodeId == source.Id).Length.ShouldBe(1);
        sections.Single(s => s.ToNodeId == target.Id).Length.ShouldBe(1);
        sections.Single(s => s.FromNodeId != source.Id && s.ToNodeId != target.Id).Length.ShouldBe(Math.Sqrt(2), tolerance: 0.001);
        virtualNodes.Count.ShouldBe(2);
        virtualNodes.First().X.ShouldBe(1);
        virtualNodes.First().Y.ShouldBe(0, tolerance: 0.001);
        virtualNodes.Last().X.ShouldBe(0, tolerance: 0.001);
        virtualNodes.Last().Y.ShouldBe(1);
    }

    [TestMethod]
    public void GetSections_PolypathEdge_SectionsAndVirtualNodesCalculated()
    {
        var source = NodeGenerator.GenerateNode(nodeId:"source", x:0, y:0, z:0);
        var target = NodeGenerator.GenerateNode(nodeId:"target", x:1, y:1, z:0);
        var edge = AgvGraphGenerator.CreatePolypathEdge(source.Id, target.Id,
            [
                new EdgeCoefficient(0, 1)
            ],
            [source, target]);
        (List<GraphEdge> sections, List<GraphNode> virtualNodes) = edge.GetSections(source, target);
        sections.Count.ShouldBe(2);
        sections.All(s => s.Variant == EdgeType.Direct).ShouldBeTrue();
        sections.Single(s => s.FromNodeId == source.Id).Length.ShouldBe(1);
        sections.Single(s => s.ToNodeId == target.Id).Length.ShouldBe(1);
        virtualNodes.Count.ShouldBe(1);
        virtualNodes.First().X.ShouldBe(0);
        virtualNodes.First().Y.ShouldBe(1);
    }
}