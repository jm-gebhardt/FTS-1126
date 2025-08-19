using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ValidData_PublishesCompleteActionOfActionGroup()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionCompletedEvent = new ActionCompleted(actionGroup.Actions.First().CorrelationId, agv);
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldHavePublishedAsync<CompleteActionOfActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ActionAborted_DoesNotPublishCompleteActionOfActionGroup()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var action = actionGroup.Actions.First();
        actionGroup.AbortAction(action.CorrelationId, []);
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionCompletedEvent = new ActionCompleted(action.CorrelationId, agv);
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<CompleteActionOfActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ActionCompleted_DoesNotPublishCompleteActionOfActionGroup()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var action = actionGroup.Actions.First();
        actionGroup.CompleteAction(action.CorrelationId);
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionCompletedEvent = new ActionCompleted(action.CorrelationId, agv);
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<CompleteActionOfActionGroup>();
    }

    [TestMethod]
    public async Task Consume_StartingActionForAgvFailed_ValidData_PublishesHandleAgvActionStartFailure()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var startingActionForAgvFailed = new StartingActionForAgvFailed(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(startingActionForAgvFailed);

        await fixture.ShouldHaveConsumedAsync<StartingActionForAgvFailed>();
        (await fixture.TestHarness.Published.Any<HandleAgvActionStartFailure>(f =>
            f.Context.Message == new HandleAgvActionStartFailure(startingActionForAgvFailed.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NonChargingActionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var nonChargingActionCompleted = new NonChargingActionCompleted(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(nonChargingActionCompleted);

        await fixture.ShouldHaveConsumedAsync<NonChargingActionCompleted>();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>(f =>
            f.Context.Message == new HandleActionCompletion(nonChargingActionCompleted.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NodesSelectedForAction_ValidData_PublishesEstimateActionGroupDuration()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var nodesSelectedForAction = new NodesSelectedForAction(actionGroup.Actions.First(), Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(nodesSelectedForAction);

        await fixture.ShouldHaveConsumedAsync<NodesSelectedForAction>();
        await fixture.ShouldHavePublishedAsync<EstimateActionGroupDuration>();
    }

    [TestMethod]
    public async Task Consume_ActionDurationEstimated_ValidData_PublishesPrepareAgvForStartingAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvId = Guid.NewGuid();
        var actionDurationEstimated = new ActionDurationEstimated(actionGroup.Actions.First(), agvId, actionGroup.CorrelationId);

        var firstPublishedMessage = await fixture.PublishAndGetFirstMessageAsync<ActionDurationEstimated, PrepareAgvForStartingAction>(actionDurationEstimated);

        firstPublishedMessage.Action.ShouldBeEquivalentTo(actionDurationEstimated.Action);
        firstPublishedMessage.CorrelationId.ShouldBe(actionDurationEstimated.AgvId);
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var agvId = Guid.NewGuid();
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(agvId);
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid(), agvId));
        await fixture.ShouldHaveConsumedAsync<ActionAbortionCompleted>();
        await fixture.ShouldHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid(), Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionAbortionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_ActionGroupReassigned_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid(), Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionAbortionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(actionGroup.Actions.First().CorrelationId, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_ReassignedToDifferentAgv_DoesNotPublishHandleActionCompletion()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(actionGroup.Actions.First().CorrelationId, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_WhenActionAborted_PublishesHandleActionAbort()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Aborted);
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldHavePublishedAsync<HandleActionAbortion>();
    }

    [DataRow(ActionState.Completed)]
    [DataRow(ActionState.Canceled)]
    [DataTestMethod]
    public async Task Consume_ActionCanceled_ActionNotStarted_DoesNotPublishHandleActionCompletion(ActionState state)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: state);
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_AutomaticActionWithdrawn_AgvOnTargetNode_DoesNotPublishFreeNode()
    {
        const string node = "Charge-0";
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new ActionEventHandlerTestFixture()
            .WithAgv(agv);
        await fixture.TestHarness.Bus.Publish(new AutomaticActionWithdrawn(agv.CorrelationId, node));
        await fixture.ShouldHaveConsumedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldNotHavePublishedAsync<FreeNode>();
    }

    [TestMethod]
    public async Task Consume_AutomaticActionWithdrawn_AgvNotOnTargetNode_PublishesFreeNode()
    {
        const string node = "Charge-0";
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "a waypoint");
        var fixture = new ActionEventHandlerTestFixture()
            .WithAgv(agv);
        await fixture.TestHarness.Bus.Publish(new AutomaticActionWithdrawn(agv.CorrelationId, node));
        await fixture.ShouldHaveConsumedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<FreeNode>();
    }

    [TestMethod]
    public async Task Consume_AutomaticActionWithdrawn_NoAgvGiven_PublishesFreeNode()
    {
        const string node = "Charge-0";
        var fixture = new ActionEventHandlerTestFixture();
        await fixture.TestHarness.Bus.Publish(new AutomaticActionWithdrawn(null, node));
        await fixture.ShouldHaveConsumedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<FreeNode>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupResumed_ValidData_SetNormalMode()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new ActionGroupResumed(Guid.NewGuid(), Guid.NewGuid(), "user"));

        await fixture.ShouldHaveConsumedAsync<ActionGroupResumed>();
        await fixture.ShouldHavePublishedAsync<SetNormalMode>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_AssignedAgvIsNotCharging_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ActionEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new ActionGroupCreated(new ActionGroup(), Guid.NewGuid(), "assignedAgv".ToGuid()));

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_AssignedAgvIsChargingInfinitely_PublishesAbortChargeAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ActionEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.TestHarness.Bus.Publish(new ActionGroupCreated(new ActionGroup { IsQueued = true }, Guid.NewGuid(), agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_NoAssignedAgv_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new ActionGroupCreated(new ActionGroup(), Guid.NewGuid(), null));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_NotQueuedPickDropActionGroup_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = Common.Testing.Helpers.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        await fixture.PublishMessageAsync(new ActionGroupCreated(actionGroup, Guid.NewGuid(), null));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_QueuedPickDropActionGroup_PublishesTryUpdateIdleAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = Common.Testing.Helpers.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true);
        await fixture.PublishMessageAsync(new ActionGroupCreated(actionGroup, Guid.NewGuid(), null));

        await fixture.ShouldNotHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<TryUpdateIdleAction>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupQueued_NoAssignedAgv_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new ActionGroupQueued(new ActionGroup(), Guid.NewGuid(), null));

        await fixture.ShouldHaveConsumedAsync<ActionGroupQueued>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupQueued_AssignedAgv_PublishesAgvBecameIdle()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new ActionGroupQueued(new ActionGroup(), Guid.NewGuid(), "assignedAgv".ToGuid()));

        await fixture.ShouldHaveConsumedAsync<ActionGroupQueued>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }
}