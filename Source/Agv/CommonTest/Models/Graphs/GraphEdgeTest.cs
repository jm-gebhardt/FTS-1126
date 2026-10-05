using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;
using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

[TestClass]
public class GraphEdgeTest
{
    [TestMethod]
    public void GetSections_DirectEdge_NoSectionsAndVirtualNodes()
    {
        var source = NodeGenerator.GenerateNode(nodeName: "source");
        var target = NodeGenerator.GenerateNode(nodeName: "target");
        var edge = GraphEdge.CreateDirect(source.Id, target.Id);
        var (sections, virtualNodes) = edge.GetSections(source, target);
        virtualNodes.ShouldBeEmpty();
        sections.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetSections_BezierEdge_NoSectionsAndVirtualNodes()
    {
        var source = NodeGenerator.GenerateNode(nodeName: "source");
        var target = NodeGenerator.GenerateNode(nodeName: "target");
        var bezier = new Bezier(new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1)
        });
        var edge = GraphEdge.CreateBezier2(source.Id, target.Id, (float) bezier.GetLength(100), 1, 0);
        var (sections, virtualNodes) = edge.GetSections(source, target);
        virtualNodes.ShouldBeEmpty();
        sections.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetSections_SlantedEdge_SectionsAndVirtualNodesCalculated()
    {
        const float west = 3f * (float) Math.PI / 2f;
        var source = NodeGenerator.GenerateNode(nodeName: "source", x: 0, y: 0, z: 0);
        var target = NodeGenerator.GenerateNode(nodeName: "target", x: 1, y: 1, z: 0);

        var edge = AgvGraphGenerator.CreateSlantedEdge(source.Name, target.Name, west, 1.0, [source, target]);
        var (sections, virtualNodes) = edge.GetSections(source, target);
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
        var source = NodeGenerator.GenerateNode(nodeName: "source", x: 0, y: 0, z: 0);
        var target = NodeGenerator.GenerateNode(nodeName: "target", x: 1, y: 1, z: 0);
        var edge = AgvGraphGenerator.CreatePolypathEdge(source.Name, target.Name,
            [
                new EdgeCoefficient(0, 1)
            ],
            [source, target]);
        var (sections, virtualNodes) = edge.GetSections(source, target);
        sections.Count.ShouldBe(2);
        sections.All(s => s.Variant == EdgeType.Direct).ShouldBeTrue();
        sections.Single(s => s.FromNodeId == source.Id).Length.ShouldBe(1);
        sections.Single(s => s.ToNodeId == target.Id).Length.ShouldBe(1);
        virtualNodes.Count.ShouldBe(1);
        virtualNodes.First().X.ShouldBe(0);
        virtualNodes.First().Y.ShouldBe(1);
    }

    [TestMethod]
    public void CreateBezier5_ValidData_AllPropertiesSet()
    {
        var source = NodeGenerator.GenerateNode(nodeName: "source");
        var target = NodeGenerator.GenerateNode(nodeName: "target");
        var controlPoints = new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(2, 2),
            new(3, 3),
            new(4, 4)
        };
        var edgeCoefficients = controlPoints.Skip(1).Take(4).Select(p => new EdgeCoefficient(p.X, p.Y)).ToArray();
        var edgeId = Guid.NewGuid();
        var bezier = new Bezier(controlPoints);
        float edgeLength = (float) bezier.GetLength(100);
        var edge = GraphEdge.CreateBezier5(source.Id, target.Id, edgeLength, edgeCoefficients, edgeId);
        edge.FromNodeId.ShouldBe(source.Id);
        edge.ToNodeId.ShouldBe(target.Id);
        edge.Id.ShouldBe(edgeId);
        edge.Variant.ShouldBe(EdgeType.Bezier5);
        edge.Length.ShouldBe(edgeLength);
        edge.VariantParameters.ShouldBeEquivalentTo(edgeCoefficients);
    }

    [TestMethod]
    public void CreateBezier5_Not4ControlPoints_ThrowsException()
    {
        var edgeCoefficients = Enumerable.Range(0, 2).Select(_ => new EdgeCoefficient(1, 1)).ToArray();
        Should.Throw<Exception>(() => GraphEdge.CreateBezier5("source".ToGuid(), "target".ToGuid(), 12.0f, edgeCoefficients, Guid.NewGuid()));
    }

    [TestMethod]
    public void CreateBezier2_ValidData_AllPropertiesSet()
    {
        var source = NodeGenerator.GenerateNode(nodeName: "source");
        var target = NodeGenerator.GenerateNode(nodeName: "target");
        var controlPoints = new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(4, 4)
        };
        var edgeCoefficients = controlPoints.Skip(1).Take(1).Select(p => new EdgeCoefficient(p.X, p.Y)).ToList();
        var edgeId = Guid.NewGuid();
        var bezier = new Bezier(controlPoints);
        float edgeLength = (float) bezier.GetLength(100);
        var edge = GraphEdge.CreateBezier2(source.Id, target.Id, edgeLength, edgeCoefficients.First().X!.Value, edgeCoefficients.First().Y!.Value, edgeId);
        edge.FromNodeId.ShouldBe(source.Id);
        edge.ToNodeId.ShouldBe(target.Id);
        edge.Id.ShouldBe(edgeId);
        edge.Variant.ShouldBe(EdgeType.Bezier2);
        edge.Length.ShouldBe(edgeLength);
        edge.VariantParameters.ShouldBeEquivalentTo(edgeCoefficients);
    }

    [TestMethod]
    public void CreateBezier3_ValidData_AllPropertiesSet()
    {
        var source = NodeGenerator.GenerateNode(nodeName: "source");
        var target = NodeGenerator.GenerateNode(nodeName: "target");
        var controlPoints = new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(2, 2),
            new(4, 4)
        };
        var edgeCoefficients = controlPoints.Skip(1).Take(2).Select(p => new EdgeCoefficient(p.X, p.Y)).ToList();
        var edgeId = Guid.NewGuid();
        var bezier = new Bezier(controlPoints);
        float edgeLength = (float) bezier.GetLength(100);
        var edge = GraphEdge.CreateBezier3(source.Id,
            target.Id,
            edgeLength,
            edgeCoefficients.First()
                .X!.Value,
            edgeCoefficients.First()
                .Y!.Value,
            edgeCoefficients.Last()
                .X!.Value,
            edgeCoefficients.Last()
                .Y!.Value,
            edgeId);
        edge.FromNodeId.ShouldBe(source.Id);
        edge.ToNodeId.ShouldBe(target.Id);
        edge.Id.ShouldBe(edgeId);
        edge.Variant.ShouldBe(EdgeType.Bezier3);
        edge.Length.ShouldBe(edgeLength);
        edge.VariantParameters.ShouldBeEquivalentTo(edgeCoefficients);
    }
}