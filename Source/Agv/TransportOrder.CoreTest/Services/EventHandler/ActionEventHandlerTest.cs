using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCreatedForAgv_PickDropActionGroup_PublishesSelectNodesForAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddActionGroup(actionGroup);
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreatedForAgv_ChargeActionGroup_PublishesSelectNodesForAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        fixture.AddActionGroup(actionGroup);
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ValidData_PublishesCompleteAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionCompletedEvent = new ActionCompleted(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        (await fixture.ConsumerHarness.Consumed.Any<ActionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CompleteAction>(f =>
            f.Context.Message == new CompleteAction(actionCompletedEvent.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartingActionForAgvFailed_ValidData_PublishesHandleAgvActionStartFailure()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var startingActionForAgvFailed = new StartingActionForAgvFailed(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(startingActionForAgvFailed);

        (await fixture.ConsumerHarness.Consumed.Any<StartingActionForAgvFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleAgvActionStartFailure>(f =>
            f.Context.Message == new HandleAgvActionStartFailure(startingActionForAgvFailed.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NonChargingActionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var nonChargingActionCompleted = new NonChargingActionCompleted(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(nonChargingActionCompleted);

        (await fixture.ConsumerHarness.Consumed.Any<NonChargingActionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>(f =>
            f.Context.Message == new HandleActionCompletion(nonChargingActionCompleted.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NodesSelectedForAction_ValidData_PublishesPrepareAgvForStartingAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var nodesSelectedForAction = new NodesSelectedForAction(actionGroup.Actions.First(), Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(nodesSelectedForAction);

        (await fixture.ConsumerHarness.Consumed.Any<NodesSelectedForAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<PrepareAgvForStartingAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupHasNoEstimatedDuration_PublishesEstimateActionGroupDuration()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvId = Guid.NewGuid();
        var actionGroupHasNoEstimatedDuration = new ActionGroupHasNoEstimatedDuration(Guid.NewGuid(), actionGroup.Actions.First(), 1.5, agvId, actionGroup.CorrelationId);

        await fixture.PublishMessageAsync(actionGroupHasNoEstimatedDuration);

        var estimateActionGroupDuration = await fixture.GetFirstPublishedMessageAsync<EstimateActionGroupDuration>();
        estimateActionGroupDuration.FirstAction.ShouldBeEquivalentTo(actionGroupHasNoEstimatedDuration.FirstAction);
        estimateActionGroupDuration.FirstActionCosts.ShouldBe(actionGroupHasNoEstimatedDuration.FirstActionCosts);
        estimateActionGroupDuration.AgvId.ShouldBe(actionGroupHasNoEstimatedDuration.AgvId);
        estimateActionGroupDuration.ActionGroupId.ShouldBe(actionGroupHasNoEstimatedDuration.ActionGroupId);
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        fixture.AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        fixture.AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        fixture.AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        fixture.AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AutomaticActionWithdrawn_ValidData_FreeNode()
    {
        var fixture = new ActionEventHandlerTestFixture();
        await fixture.TestHarness.Bus.Publish(new AutomaticActionWithdrawn("Charge-0"));
        (await fixture.ConsumerHarness.Consumed.Any<AutomaticActionWithdrawn>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FreeNode>()).ShouldBeTrue();
    }
}