using FluentValidation.Results;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using ResetActionGroup = Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup.ResetActionGroup;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

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
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
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
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
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
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
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
        await fixture.ShouldHaveConsumedAsync<TryStartManualAction>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
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
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
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
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
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
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
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
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotComplete_DoesNotPublishActionGroupCompleted()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, dropActionState: ActionState.Created, assignedAgv: Guid.NewGuid());
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Never());
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupComplete_PublishesActionGroupCompleted()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: Guid.NewGuid());
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Last().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCompleted>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
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
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCancelFailed>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_RespondsActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        Response<ActionGroupCanceled> response = await fixture.GetResponseAsync<ActionGroupCanceled, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
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
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
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
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
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
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
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
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ActionsInInvalidState_PublishesActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Invalid, dropActionState: ActionState.Invalid);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeTrue();
        fixture.ActionGroupRepository.Verify(a => a.UpdateActionAndGroupStateAsync(actionGroup.Actions[0], null, ActionState.Canceled, It.Is<Reason>(r=>r.Template.Equals("ActionGroup was canceled manually"))));
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

        var abortedMessage = await fixture.PublishAndGetFirstMessageAsync<AbortActionGroup, ActionAborted>(
            new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedAbortedAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedAbortedAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
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
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
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
            new List<AgvAction> {parkAction},
            ActionGroupCreator.TransportOrder,
            createdDate: DateTime.UtcNow, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Park);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
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
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Never());
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
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        fixture.VerifyOrderCompletionMetricsCollected(Times.Never());
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroupDoNotThrowException_SavesInvalidActionGroupAndPublishesActionGroupCreatedAndActionGroupInvalid()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> {new("propertyName", failureReason)}));

        var actionGroupCreatedEvent = await fixture.PublishAndGetFirstMessageAsync<QueueActionGroup, ActionGroupCreated>(
            new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), false));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        actionGroupCreatedEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupCreatedEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        var invalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        invalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        invalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        await fixture.ShouldHavePublishedAsync<ActionGroupInvalid>();
        var actionGroupInvalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        actionGroupInvalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupInvalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);
        actionGroupInvalidEvent.ValidationErrors.ShouldBe(new List<string> {failureReason});

        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();

        fixture.IsActionGroupSaved(Times.Once());
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroupThrowException_DoesNotPublishEventsDoNotSaveActionGroup()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> {new("propertyName", failureReason)}));

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));
        
        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        fixture.IsActionGroupSaved(Times.Never());
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_AssignedActionGroup_PublishesActionGroupCreatedForAgv()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));
        
        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreatedForAgv>();

        fixture.IsActionGroupSaved(Times.Once());
    }

    [TestMethod]
    public async Task Consume_SelectNodesForAction_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"])
            .AddActionGroup(actionGroup);

        var selectNodesForAction = new SelectNodesForAction(fixture.Mapper.Map<Common.Messages.ActionGroups.AgvAction>(actionGroup.Actions.First()), agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(selectNodesForAction);
        await fixture.ShouldHaveConsumedAsync<SelectNodesForAction>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
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
            .AddActionGroupRetrieval(headingTo: false);

        var command = new StartChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, chargeNodeName);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<StartChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreatedForAgv>();
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
            .AddActionGroupRetrieval(headingTo: false);

        var command = new StartChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, chargeNodeName);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<StartChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreatedForAgv>();
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
            .AddActionGroupRetrieval(headingTo: false);

        var command = new StartChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, chargeNodeName);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<StartChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreatedForAgv>();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_ValidAgv_PublishedActionGroupCreatedAndActionGroupCreatedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var parkActions = new List<AgvAction> {AgvActionGenerator.GenerateParkAction(targetNodeId: parkNodeName)};
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false);

        var command = new CreateParkingActionGroup(Guid.NewGuid(), agv.CorrelationId, parkNodeName);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<CreateParkingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreatedForAgv>();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvMustCharge_NodesSelectedForActionNotPublished()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: fixture.SettingsReadModel.BatteryLevelCritical - 1);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));
        
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
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
        
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
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

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
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
        
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
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
        
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
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
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();
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
        
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();

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
        
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }
}