using FluentValidation.Results;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

[TestClass]
public class CreateActionGroupRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_AssignedAgvNotFound_ThrowsAgvNotFoundException()
    {
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("a known source", "a known target", "unknown agv".ToGuid(), false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_AssignedAgvCanNotStartManualPickDropAction_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.UserMode));
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("a known source", "a known target", agv.CorrelationId, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotFound_ThrowsNodeNotFoundException()
    {
        var targetNode = NodeGenerator.GenerateNode(nodeId:"a known target");
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("an unknown source", targetNode.Id, null, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotPickStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Charge);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotFound_ThrowsNodeNotFoundException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, "an unknown target", null, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotDropStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Charge);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_InvalidActionGroup_ThrowsActionGroupInvalidException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", "reason") }))
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupInvalidException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null, false, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithAssignedAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        Response<ActionGroupCreated> response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, agv.CorrelationId, false, ActionGroupCreator.TransportOrderWebApi));

        response.Message.ActionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        response.Message.ActionGroup.HasPriority.ShouldBeFalse();
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeId.ShouldBe(sourceNode.Id);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeId.ShouldBe(targetNode.Id);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithoutAssignedAgv()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeId:"a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        Response<ActionGroupCreated> response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, null, false, ActionGroupCreator.TransportOrderWebApi));

        response.Message.ActionGroup.AssignedAgv.ShouldBeNull();
        response.Message.ActionGroup.HasPriority.ShouldBeFalse();
        response.Message.ActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeId.ShouldBe(sourceNode.Id);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeId.ShouldBe(targetNode.Id);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_WithPriority_PublishesActionGroupCreatedWithHasPriorityTrue()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var sourceNode = new GraphNode("a known source") { Type = AgvGraph.NodeType.Pick };
        var targetNode = new GraphNode("a known target") { Type = AgvGraph.NodeType.Drop };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        Response<ActionGroupCreated> response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, agv.CorrelationId, true, ActionGroupCreator.TransportOrderWebApi));

        response.Message.ActionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        response.Message.ActionGroup.HasPriority.ShouldBeTrue();
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeId.ShouldBe(sourceNode.Id);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeId.ShouldBe(targetNode.Id);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }
}