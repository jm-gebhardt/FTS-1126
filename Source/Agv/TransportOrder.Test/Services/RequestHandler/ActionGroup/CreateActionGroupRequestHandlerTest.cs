using FluentValidation.Results;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
                new CreatePickDropActionGroup("a known source".ToGuid(), "a known target".ToGuid(), false, false, null, "unknown agv".ToGuid(), null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_AssignedAgvCanNotStartManualPickDropAction_ThrowsActionNotAllowedInModeException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.UserMode));
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithAgvs([agv])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, agv.CorrelationId, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotFound_ThrowsValidationException()
    {
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target");
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup("an unknown source".ToGuid(), targetNode.Id, false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_SourceNodeNotPickStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Charge);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotFound_ThrowsValidationException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, "an unknown target".ToGuid(), false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_TargetNodeNotDropStation_ThrowsInvalidNodeTypeException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Charge);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_InvalidActionGroup_ThrowsActionGroupInvalidException()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", "reason") }))
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupInvalidException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithAssignedAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        var response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, agv.CorrelationId, null, null, ActionGroupCreator.TransportOrderWebApi, "user"));

        response.Message.ActionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        response.Message.ActionGroup.IsQueued.ShouldBeTrue();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeName.ShouldBe(sourceNode.Name);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeName.ShouldBe(targetNode.Name);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_CreatePickDropActionGroup_ValidData_PublishesActionGroupCreatedWithoutAssignedAgv()
    {
        var sourceNode = NodeGenerator.GenerateNode(nodeName: "a known source", nodeType: AgvGraph.NodeType.Pick);
        var targetNode = NodeGenerator.GenerateNode(nodeName: "a known target", nodeType: AgvGraph.NodeType.Drop);
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([sourceNode, targetNode])
            .WithValidationResult(new ValidationResult())
            .Build();

        var response = await fixture.GetResponseAsync<ActionGroupCreated>(new CreatePickDropActionGroup(sourceNode.Id, targetNode.Id, false, false, null, null, null, null, ActionGroupCreator.TransportOrderWebApi, "user"));

        response.Message.ActionGroup.AssignedAgv.ShouldBeNull();
        response.Message.ActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        response.Message.ActionGroup.IsQueued.ShouldBeTrue();
        var pickAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.TargetNodeName.ShouldBe(sourceNode.Name);
        var dropAction = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).Last();
        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.TargetNodeName.ShouldBe(targetNode.Name);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }

    [DataRow(AgvGraph.NodeType.Pick, "source", null)]
    [DataRow(AgvGraph.NodeType.Drop, null, "target")]
    [TestMethod]
    public async Task CreatePickDropActionGroup_OnlyPickOrDrop_NoAssignedAgv_ThrowsAgvNotAssignedToActionGroupException(AgvGraph.NodeType nodeType, string? source, string? target)
    {
        var node = new GraphNode("a known node".ToGuid(), "a known node") { Type = [nodeType] };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([node])
            .WithValidationResult(new ValidationResult())
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotAssignedToActionGroupException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(source != null ? node.Id : source?.ToGuid(),
                    target != null ? node.Id : target?.ToGuid(),
                    false,
                    false,
                    "Hu",
                    null,
                    null,
                    null,
                    ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task CreatePickDropActionGroup_NoSourceOrTargetGiven_ThrowsActionGroupInvalidException()
    {
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithNodes([])
            .WithValidationResult(new ValidationResult())
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupInvalidException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(null,
                    null,
                    false,
                    false,
                    "Hu",
                    null,
                    null,
                    null,
                    ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [DataRow(AgvGraph.NodeType.Pick, "source", null)]
    [DataRow(AgvGraph.NodeType.Drop, null, "target")]
    [TestMethod]
    public async Task CreatePickDropActionGroup_OnlyPickOrDrop_AssignedAgvNotInOrPendingMaintenanceMode_ThrowsActionNotAllowedInModeException(AgvGraph.NodeType nodeType, string? source, string? target)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var node = new GraphNode("a known node".ToGuid(), "a known node") { Type = [nodeType] };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .WithNodes([node])
            .WithValidationResult(new ValidationResult())
            .Build();

        agv.IsInMaintenance.ShouldBeFalse();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () =>
            await fixture.GetResponseAsync<ActionGroupCreated>(
                new CreatePickDropActionGroup(source != null ? node.Id : source?.ToGuid(),
                    target != null ? node.Id : target?.ToGuid(),
                    false,
                    false,
                    "Hu",
                    agv.CorrelationId,
                    null,
                    null,
                    ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [DataRow(AgvGraph.NodeType.Pick, "source", null)]
    [DataRow(AgvGraph.NodeType.Drop, null, "target")]
    [TestMethod]
    public async Task CreatePickDropActionGroup_OnlyPickOrDrop_ValidData_PublishesActionGroupCreated(AgvGraph.NodeType nodeType, string? source, string? target)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.PrepareMaintenanceMode(null, null);
        agv.SwitchMode();
        var node = new GraphNode("a known node".ToGuid(), "a known node") { Type = [nodeType] };
        var fixture = new CreateActionGroupRequestHandlerTestFixture()
            .WithAgvs([agv])
            .WithNodes([node])
            .WithValidationResult(new ValidationResult())
            .Build();

        agv.IsInMaintenance.ShouldBeTrue();

        var response = await fixture.GetResponseAsync<ActionGroupCreated>(
            new CreatePickDropActionGroup(source != null ? node.Id : source?.ToGuid(),
                target != null ? node.Id : target?.ToGuid(),
                false,
                false,
                "Hu",
                agv.CorrelationId,
                null,
                null,
                ActionGroupCreator.TransportOrderWebApi, "user"));

        response.Message.ActionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        response.Message.ActionGroup.IsManual.ShouldBeTrue();
        response.Message.ActionGroup.IsQueued.ShouldBeTrue();
        response.Message.ActionGroup.Actions.Count.ShouldBe(1);
        var action = response.Message.ActionGroup.Actions.OrderBy(a => a.Sequence).First();
        action.ActionType.ShouldBe(nodeType == AgvGraph.NodeType.Pick ? ActionType.Pick : ActionType.Drop);
        action.TargetNodeName.ShouldBe(node.Name);
        (await fixture.MassTransitTestHarness.Published.Any<Common.Messages.ActionGroups.Events.ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
    }
}