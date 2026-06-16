using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.GraphAnalysis;

[TestClass]
public class BoundingBoxConflictRequestHandlerTests
{
    [TestMethod]
    public async Task Consume_NoAgvData_ReturnsEmpty()
    {
        var fixture = new BoundingBoxConflictRequestHandlerTestFixture().Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.GetResponseAsync<GotBoundingBoxConflicts>(new GetBoundingBoxConflicts(Guid.NewGuid()));

        response.Message.Conflicts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_OverlappingBoxesForLargestAgvType_ReturnsConflict()
    {
        const string smallAgvType = "Small";
        const string largeAgvType = "Large";
        var small = AgvDataGenerator.GenerateAgvData(type: smallAgvType, width: 0.5, length: 0.5);
        var large = AgvDataGenerator.GenerateAgvData(type: largeAgvType, width: 2, length: 2.0);

        var boxA = BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1.2, largeAgvType);
        var boxB = BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 2, 0, 1.2, largeAgvType);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithAgvData(small, large)
            .WithBoxesByAgvType(largeAgvType, boxA, boxB)
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.GetResponseAsync<GotBoundingBoxConflicts>(new GetBoundingBoxConflicts(Guid.NewGuid()));

        response.Message.Conflicts.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_NonOverlappingBoxes_ReturnsEmpty()
    {
        const string agvType = "T1";

        var agvData = AgvDataGenerator.GenerateAgvData(type: agvType, width: 1.0, length: 1.0);

        var boxA = BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 0, 0, 1);
        var boxB = BoundingBoxGenerator.SquareBox(Guid.NewGuid(), ResourceType.Node, 10, 10, 1);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithAgvData(agvData)
            .WithBoxesByAgvType(agvType, boxA, boxB)
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.GetResponseAsync<GotBoundingBoxConflicts>(new GetBoundingBoxConflicts(Guid.NewGuid()));

        response.Message.Conflicts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_NodeRequest_ReturnsOnlyConflictsInvolvingThatNode()
    {
        const string agvType = "T1";
        const string targetNode = "target";
        const string otherNode = "other";

        var node = NodeGenerator.GenerateNode(nodeName: targetNode);
        var other = NodeGenerator.GenerateNode(nodeName: otherNode);
        var graph = GraphGenerator.Generate(nodes: [node, other]);
        var agvData = AgvDataGenerator.GenerateAgvData(type: agvType, width: 1.0, length: 1.0);

        var boxA = BoundingBoxGenerator.SquareBox(node.Id, ResourceType.Node, 0, 0, 1);
        var boxB = BoundingBoxGenerator.SquareBox(other.Id, ResourceType.Node, 1, 0, 1);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithGraph(graph)
            .WithAgvData(agvData)
            .WithBoxesByAgvType(agvType, boxA, boxB)
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), node.Id));

        response.Message.Conflicts.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_NodeRequest_NoOverlap_ReturnsEmpty()
    {
        const string agvType = "T1";
        const string targetNode = "target";
        const string otherNode = "other";

        var node = NodeGenerator.GenerateNode(nodeName: targetNode);
        var other = NodeGenerator.GenerateNode(nodeName: otherNode);
        var graph = GraphGenerator.Generate(nodes: [node, other]);
        var agvData = AgvDataGenerator.GenerateAgvData(type: agvType, width: 1.0, length: 1.0);

        var boxA = BoundingBoxGenerator.SquareBox(node.Id, ResourceType.Node, 0, 0, 1);
        var boxB = BoundingBoxGenerator.SquareBox(other.Id, ResourceType.Node, 10, 10, 1);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithGraph(graph)
            .WithAgvData(agvData)
            .WithBoxesByAgvType(agvType, boxA, boxB)
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), node.Id));

        response.Message.Conflicts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_NodeRequest_UnknownNode_Throws()
    {
        const string knownNode = "known";

        var graph = GraphGenerator.Generate(nodes: [NodeGenerator.GenerateNode(nodeName: knownNode)]);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithGraph(graph)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(() => fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_NodeRequest_WithEdgeId_Throws()
    {
        const string knownNode = "known";
        var edge = EdgeGenerator.Generate();
        var graph = GraphGenerator.Generate(nodes: [NodeGenerator.GenerateNode(nodeName: knownNode)], edges: [edge]);

        var fixture = new BoundingBoxConflictRequestHandlerTestFixture()
            .WithGraph(graph)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(() => fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), edge.Id)));
    }
}