using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class BoundingBoxConflictDetectorTests
{
    [TestMethod]
    public void Detect_TwoOverlappingBoxes_ReturnsOneConflict()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(aId, ResourceType.Node, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(bId, ResourceType.Node, 1, 0, 1.0))
            .Detector.Detect();

        result.Count.ShouldBe(1);
        result[0].ResourceA.ResourceId.ShouldBe(aId);
        result[0].ResourceB.ResourceId.ShouldBe(bId);
    }

    [TestMethod]
    public void Detect_TwoNonOverlappingBoxes_ReturnsEmpty()
    {
        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 5, 5, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_AdjacentNodeAndEdge_ExcludesConflict()
    {
        var fromNode = RoutingGraphGenerator.GenerateNode(0, 0);
        var toNode = RoutingGraphGenerator.GenerateNode(2, 0);
        var edge = RoutingGraphGenerator.GenerateEdge(fromNode, toNode);
        var graph = RoutingGraphGenerator.GenerateGraph([fromNode, toNode], [edge]);

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingNode(fromNode).Id, ResourceType.Node, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edge).Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_TwoEdgesSharingANode_ExcludesConflict()
    {
        var nodeA = RoutingGraphGenerator.GenerateNode(0, 0);
        var sharedNode = RoutingGraphGenerator.GenerateNode(2, 0);
        var nodeB = RoutingGraphGenerator.GenerateNode(4, 0);
        var edgeA = RoutingGraphGenerator.GenerateEdge(nodeA, sharedNode);
        var edgeB = RoutingGraphGenerator.GenerateEdge(sharedNode, nodeB);
        var graph = RoutingGraphGenerator.GenerateGraph([nodeA, sharedNode, nodeB], [edgeA, edgeB]);

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeA).Id, ResourceType.Edge, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeB).Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_TwoOverlappingEdgesWithoutSharedNode_ReturnsOneConflict()
    {
        var nodeA = RoutingGraphGenerator.GenerateNode(0, 0);
        var nodeB = RoutingGraphGenerator.GenerateNode(2, 0);
        var nodeC = RoutingGraphGenerator.GenerateNode(0, 2);
        var nodeD = RoutingGraphGenerator.GenerateNode(2, 2);
        var edgeA = RoutingGraphGenerator.GenerateEdge(nodeA, nodeB);
        var edgeB = RoutingGraphGenerator.GenerateEdge(nodeC, nodeD);
        var graph = RoutingGraphGenerator.GenerateGraph([nodeA, nodeB, nodeC, nodeD], [edgeA, edgeB]);

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeA).Id, ResourceType.Edge, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeB).Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Detect_ThreeBoxesOnePairOverlaps_ReturnsOneConflict()
    {
        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 10, 10, 1),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 1, 0, 1))
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Detect_ContainedPolygon_ReturnedAsConflict()
    {
        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 3.0),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1))
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Detect_NonConvexOverlap_ReturnedAsConflict()
    {
        IReadOnlyList<PolygonPoint> lShape =
        [
            new(0, 0), new(3, 0), new(3, 1), new(1, 1), new(1, 3), new(0, 3)
        ];

        var boxA = BoundingBoxGenerator.PolygonBox(Guid.NewGuid(), ResourceType.Node, lShape);
        var boxB = BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 1.5, 0.5, 0.3);

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(boxA, boxB)
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void DetectFor_OnlyReturnsPairsInvolvingTarget()
    {
        var targetId = Guid.NewGuid();
        var neighbourId = Guid.NewGuid();

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(targetId, ResourceType.Node, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(neighbourId, ResourceType.Node, 1, 0, 1.0),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 10, 10, 1.0),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 11, 10, 1.0))
            .Detector.DetectFor(targetId);

        result.Count.ShouldBe(1);
        result[0].ResourceA.ResourceId.ShouldBe(targetId);
        result[0].ResourceB.ResourceId.ShouldBe(neighbourId);
    }

    [TestMethod]
    public void DetectFor_AdjacentEdges_AreExcluded()
    {
        var nodeA = RoutingGraphGenerator.GenerateNode(0, 0);
        var sharedNode = RoutingGraphGenerator.GenerateNode(2, 0);
        var nodeB = RoutingGraphGenerator.GenerateNode(4, 0);
        var edgeA = RoutingGraphGenerator.GenerateEdge(nodeA, sharedNode);
        var edgeB = RoutingGraphGenerator.GenerateEdge(sharedNode, nodeB);
        var graph = RoutingGraphGenerator.GenerateGraph([nodeA, sharedNode, nodeB], [edgeA, edgeB]);

        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeA).Id, ResourceType.Edge, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(RoutingGraphGenerator.GetRoutingEdge(edgeB).Id, ResourceType.Edge, 1, 0, 1.0))
            .Detector.DetectFor(RoutingGraphGenerator.GetRoutingEdge(edgeA).Id);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void DetectFor_UnknownResource_ReturnsEmpty()
    {
        var result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 1, 0, 1.0))
            .Detector.DetectFor(Guid.NewGuid());

        result.ShouldBeEmpty();
    }
}