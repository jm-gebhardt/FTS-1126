using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

[TestClass]
public class CreateActionGroupRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_AssignedAgvNotFound_ThrowsAgvNotFoundException()
    {
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvExists(false)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("a known source", "a known target", "unknown agv".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotFound_ThrowsNodeNotFoundException()
    {
        var targetNode = new GraphNode("a known target");
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("an unknown source", targetNode.Id, null)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotPickStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Charge };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotFound_ThrowsNodeNotFoundException()
    {
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, "an unknown target", null)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotDropStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Charge };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_InvalidActionGroup_ThrowsActionGroupInvalidException()
    {
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", "reason") }))
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupInvalidException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithAssignedAgv()
    {
        var agv = Guid.NewGuid();
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvExists(true)
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        Response<ActionGroupCreated> response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, agv));

        response.Message.ActionGroup.AssignedAgv.ShouldBe(agv);
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeId.ShouldBe(sourceNode.Id);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeId.ShouldBe(targetNode.Id);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithoutAssignedAgv()
    {
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvExists(true)
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        Response<ActionGroupCreated> response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null));

        response.Message.ActionGroup.AssignedAgv.ShouldBeNull();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeId.ShouldBe(sourceNode.Id);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeId.ShouldBe(targetNode.Id);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }
}