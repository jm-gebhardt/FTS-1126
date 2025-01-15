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
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionCompletedEvent = new ActionCompleted(actionGroup.Actions.First().CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldHavePublishedAsync<CompleteActionOfActionGroup>();
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
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionAbortionCompleted>();
        await fixture.ShouldHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionAbortionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldNotHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ActionCanceled>();
        await fixture.ShouldHavePublishedAsync<HandleActionCompletion>();
    }

    [TestMethod]
    public async Task Consume_AutomaticActionWithdrawn_ValidData_FreeNode()
    {
        var fixture = new ActionEventHandlerTestFixture();
        await fixture.TestHarness.Bus.Publish(new AutomaticActionWithdrawn("Charge-0"));
        await fixture.ShouldHaveConsumedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<FreeNode>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_AssignedAgv_PublishesAgvBecameIdle()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCreated(new ActionGroup(), Guid.NewGuid(), "assignedAgv".ToGuid()));
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_NoAssignedAgv_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new ActionEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCreated(new ActionGroup(), Guid.NewGuid(), null));

        await fixture.ShouldHaveConsumedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }
}