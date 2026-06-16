using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.GraphAnalysis;

[TestClass]
public class BoundingBoxConflictDetectorTests
{
    [TestMethod]
    public void Detect_TwoOverlappingBoxes_ReturnsOneConflict()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
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
        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 5, 5, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_AdjacentNodeAndEdge_ExcludesConflict()
    {
        var edge = EdgeGenerator.Generate();
        var graph = GraphGenerator.Generate(edges: [edge]);

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(edge.FromNodeId, ResourceType.Node, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(edge.Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_TwoEdgesSharingANode_ExcludesConflict()
    {
        var edgeA = EdgeGenerator.Generate("a", "shared");
        var edgeB = EdgeGenerator.Generate("shared", "b");
        var graph = GraphGenerator.Generate(edges: [edgeA, edgeB]);

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(edgeA.Id, ResourceType.Edge, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(edgeB.Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void Detect_TwoOverlappingEdgesWithoutSharedNode_ReturnsOneConflict()
    {
        var edgeA = EdgeGenerator.Generate("a", "b");
        var edgeB = EdgeGenerator.Generate("c", "d");
        var graph = GraphGenerator.Generate(edges: [edgeA, edgeB]);

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(edgeA.Id, ResourceType.Edge, 0, 0, 1),
                BoundingBoxGenerator.SquareBox(edgeB.Id, ResourceType.Edge, 1, 0, 1))
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Detect_ThreeBoxesOnePairOverlaps_ReturnsOneConflict()
    {
        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
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
        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
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

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(boxA, boxB)
            .Detector.Detect();

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public void DetectFor_OnlyReturnsPairsInvolvingTarget()
    {
        var targetId = Guid.NewGuid();
        var neighbourId = Guid.NewGuid();

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
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
        const string fromNode = "a";
        const string sharedNode = "shared";
        const string toNode = "b";
        var edgeA = EdgeGenerator.Generate(fromNode, sharedNode);
        var edgeB = EdgeGenerator.Generate(sharedNode, toNode);
        var graph = GraphGenerator.Generate(edges: [edgeA, edgeB]);

        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithGraph(graph)
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(edgeA.Id, ResourceType.Edge, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(edgeB.Id, ResourceType.Edge, 1, 0, 1.0))
            .Detector.DetectFor(edgeA.Id);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void DetectFor_UnknownResource_ReturnsEmpty()
    {
        IReadOnlyList<BoundingBoxConflict> result = new BoundingBoxConflictDetectorTestFixture()
            .WithBoxes(
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1.0),
                BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 1, 0, 1.0))
            .Detector.DetectFor(Guid.NewGuid());

        result.ShouldBeEmpty();
    }
}