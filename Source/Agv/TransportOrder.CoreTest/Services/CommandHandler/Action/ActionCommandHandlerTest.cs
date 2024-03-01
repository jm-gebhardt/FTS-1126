using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Models.Graphs.GraphNode;
using ResetActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup.ResetActionGroup;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_ValidData_Publishes_StartActionCommandAndActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);

        var startAction = new StartActionOfActionGroup(actionGroup.Actions.First(), agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        (await fixture.ConsumerHarness.Consumed.Any<StartActionOfActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_FirstPickAction_Publishes_StartActionCommandAndActionGroupChangedActionGroupStarted()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        (await fixture.ConsumerHarness.Consumed.Any<StartActionOfActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupStarted>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_AgvInMessageIsNotAssignedAgv_PublishesStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var anotherAgv = AgvGenerator.GenerateIdleAgv(index: 2, lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(anotherAgv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, anotherAgv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        (await fixture.ConsumerHarness.Consumed.Any<StartActionOfActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupStarted>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<StartingActionForAgvFailed>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartManualAction_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(isLocked: true, lastNodeId: "NodeId");

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);

        var command = new TryStartManualAction(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<TryStartManualAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartManualAction_AgvIsNotLocked_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);

        var command = new TryStartManualAction(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<TryStartManualAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_NoUnfinishedAction_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv(isLocked: true);
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup, hasUnfinishedAction: false, hasNotStartedAction: false);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotCompleteNotChargingAction_PublishesActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CompleteActionOfActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCompleted>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotCompleteChargingAction_PublishesActionGroupChangedSetIdleNotCalled()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CompleteActionOfActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCompleted>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_PublishesActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_RespondsActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<CancelActionGroup>())
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        Response<ActionGroupCanceled> response = await fixture.GetResponseAsync<ActionGroupCanceled, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_UnknownActionGroup_PublishesActionGroupCancelFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_UnknownActionGroup_RespondsActionGroupCancelFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        Response<ActionGroupCancelFailed> response = await fixture.GetResponseAsync<ActionGroupCancelFailed, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        response.Message.ErrorCode.ShouldBe(HostOrderCancelFailedErrorCode.OrderDoesNotExist);
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(nameof(ActionGroupState.Started))]
    [DataRow(nameof(ActionGroupState.Completed))]
    [DataRow(nameof(ActionGroupState.Failed))]
    [DataRow(nameof(ActionGroupState.Canceled))]
    public async Task Consume_CancelActionGroup_InvalidState_PublishesActionGroupCancelFailed(string invalidState)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        actionGroup.GetType().GetProperty(nameof(actionGroup.State))!.SetValue(actionGroup, Enum.Parse<ActionGroupState>(invalidState));
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(nameof(ActionGroupState.Started), HostOrderCancelFailedErrorCode.OrderAlreadyStarted)]
    [DataRow(nameof(ActionGroupState.Completed), HostOrderCancelFailedErrorCode.OrderAlreadyFinished)]
    [DataRow(nameof(ActionGroupState.Failed), HostOrderCancelFailedErrorCode.OrderAlreadyFailed)]
    [DataRow(nameof(ActionGroupState.Canceled), HostOrderCancelFailedErrorCode.OrderAlreadyCanceled)]
    public async Task Consume_CancelActionGroup_InvalidState_RespondsActionGroupCancelFailed(string invalidState, HostOrderCancelFailedErrorCode error)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        actionGroup.GetType().GetProperty(nameof(actionGroup.State))!.SetValue(actionGroup, Enum.Parse<ActionGroupState>(invalidState));
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        Response<ActionGroupCancelFailed> response = await fixture.GetResponseAsync<ActionGroupCancelFailed, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        response.Message.ErrorCode.ShouldBe(error);
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ValidData_PublishesActionAbortedAndActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var expectedAbortedAction = actionGroup.Actions.First(a => a.State == ActionState.Started);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        (await fixture.ConsumerHarness.Consumed.Any<AbortActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeTrue();
        var abortedMessage = fixture.TestHarness.Published.Select<ActionAborted>().First().Context.Message;
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedAbortedAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedAbortedAction.CorrelationId);
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ChargeActionGroup_PublishesActionAbortedAndActionGroupChangedAutomaticActionWithdrawn()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, actionStateMove: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        (await fixture.ConsumerHarness.Consumed.Any<AbortActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ParkActionGroup_PublishesActionAbortedAndActionGroupChangedAutomaticActionWithdrawn()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroupId = Guid.NewGuid();
        var parkAction = AgvActionGenerator.GenerateParkAction(parentId: actionGroupId, actionState: ActionState.Started);
        var actionGroup = new ActionGroup(actionGroupId,
            string.Empty,
            new List<AgvAction> { parkAction },
            ActionGroupCreator.TransportOrder,
            createdDate: DateTime.UtcNow, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Park);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        (await fixture.ConsumerHarness.Consumed.Any<AbortActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_NoAssignedAgv_DoesNotPublishActionAborted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        (await fixture.ConsumerHarness.Consumed.Any<AbortActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_NoActionInStatusStarted_DoesNotPublishActionAborted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        (await fixture.ConsumerHarness.Consumed.Any<AbortActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroupDoNotThrowException_SavesInvalidActionGroupAndPublishesActionGroupCreatedAndActionGroupInvalid()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var mapper = ActionCommandHandlerTestFixture.GetMapper();
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", failureReason) }));

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), false));

        (await fixture.ConsumerHarness.Consumed.Any<QueueActionGroup>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        var actionGroupCreatedEvent = fixture.TestHarness.Published.Select<ActionGroupCreated>().First().Context.Message;
        actionGroupCreatedEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupCreatedEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        var invalidEvent = fixture.TestHarness.Published.Select<ActionGroupInvalid>().First().Context.Message;
        invalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        invalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        (await fixture.TestHarness.Published.Any<ActionGroupInvalid>()).ShouldBeTrue();
        var actionGroupInvalidEvent = fixture.TestHarness.Published.Select<ActionGroupInvalid>().First().Context.Message;
        actionGroupInvalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupInvalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);
        actionGroupInvalidEvent.ValidationErrors.ShouldBe(new List<string> { failureReason });

        (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupStarted>()).ShouldBeFalse();

        fixture.IsActionGroupSaved(Times.Once());
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroupThrowException_DoesNotPublishEventsDoNotSaveActionGroup()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var mapper = ActionCommandHandlerTestFixture.GetMapper();
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", failureReason) }));

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));
        (await fixture.ConsumerHarness.Consumed.Any<QueueActionGroup>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupInvalid>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupStarted>()).ShouldBeFalse();

        fixture.IsActionGroupSaved(Times.Never());
    }

    [TestMethod]
    public async Task Consume_SelectNodesForAction_ValidData_PublishesNodesSelectedForAction()
    {
        var mapper = ActionCommandHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);

        var selectNodesForAction = new SelectNodesForAction(mapper.Map<Common.Messages.ActionGroups.AgvAction>(actionGroup.Actions.First()), agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(selectNodesForAction);
        (await fixture.ConsumerHarness.Consumed.Any<SelectNodesForAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartChargingActionGroup_BatteryCritical_PublishedActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        const string chargeNodeName = "Charge-1";

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddChargingPointManager(chargeGraphNode: new GraphNode(chargeNodeName));

        var command = new StartChargingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<StartChargingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartChargingActionGroup_AgvLocked_DoesNotPublishActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        agv.Lock();

        const string chargeNodeName = "Charge-1";
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddChargingPointManager(chargeGraphNode: new GraphNode(chargeNodeName));

        var command = new StartChargingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<StartChargingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_StartChargingActionGroup_AgvIsCharging_DoesNotPublishActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateChargingAgv(batteryCharge: 10);

        const string chargeNodeName = "Charge-1";
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddChargingPointManager(chargeGraphNode: new GraphNode(chargeNodeName));

        var command = new StartChargingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<StartChargingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_ValidAgv_PublishedActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var parkActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeId: parkNodeName) };
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new CreateParkingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CreateParkingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_BatteryIsLow_DoesNotPublishActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 20);
        const string parkNodeName = "Park-1";

        var parkActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeId: parkNodeName) };
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new CreateParkingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CreateParkingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_AgvLocked_DoesNotPublishActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock();
        const string parkNodeName = "Park-1";

        var parkActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeId: parkNodeName) };
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new CreateParkingActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CreateParkingActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCreatedForAgv>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvMustCharge_NodesSelectedForActionNotPublished()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: fixture.Settings.BatteryLevelCritical - 1);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        (await fixture.ConsumerHarness.Consumed.Any<TryStartNewActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvIsLocked_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        (await fixture.ConsumerHarness.Consumed.Any<TryStartNewActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvHasUnfinishedDropAction_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(ignoreLocked: false);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        (await fixture.ConsumerHarness.Consumed.Any<TryStartNewActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "A node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .AddRoute(["A node", actionGroup.Actions.First().TargetNodeId]);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        (await fixture.ConsumerHarness.Consumed.Any<TryStartNewActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeTrue();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvIsNotAvailable_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .AddRoute(["A node", actionGroup.Actions.First().TargetNodeId]);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        (await fixture.ConsumerHarness.Consumed.Any<TryStartNewActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldNotBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ResetActionGroup_StartedPickDropActionGroup_PublishesActionAbortedActionGroupChangedRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var expectedResetAction = actionGroup.Actions.First(a => a.State == ActionState.Started);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId));

        var abortedMessage = await fixture.GetFirstPublishedMessageAsync<ActionAborted>();
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedResetAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedResetAction.CorrelationId);
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Success>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeFalse();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_ResetActionGroup_CompletedPickDropActionGroup_PublishesActionGroupChangedRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(false);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Success>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeFalse();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_ResetActionGroup_ActionGroupWithNodeReservation_PublishesAutomaticActionWithdrawn()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionAborted>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Success>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }
}