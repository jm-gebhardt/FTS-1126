using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
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
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreatedForAgv_ChargeActionGroup_PublishesSelectNodesForAction()
    {
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AbortChargeAction>()).ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(40, 40, true)]
    [DataRow(39, 40, false)]
    [DataRow(41, 40, true)]
    public async Task Consume_ActionGroupCreatedForAgv_IfChargeActionIsRunningAndAboveThresholdForAbort_PublishesAbortChargeAction(int agvCharge, int threshold, bool expectedToAbortChargeAction)
    {
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateChargingAgv(batteryCharge: agvCharge);
        var chargeAction = AgvActionGenerator.GenerateChargeAction();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgvs(agv)
            .WithCurrentAgvAction(chargeAction)
            .WithAgvSettings(new AgvSettingsReadModel (minChargeUp : threshold));
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();

        if (expectedToAbortChargeAction)
        {
            (await fixture.TestHarness.Published.Any<AbortChargeAction>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeFalse();
        }
        else
        {
            (await fixture.TestHarness.Published.Any<AbortChargeAction>()).ShouldBeFalse();
            (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
        }
    }

    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ValidData_PublishesCompleteActionOfActionGroup()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var actionCompletedEvent = new ActionCompleted(actionGroup.Actions.First().CorrelationId);
        await fixture.TestHarness.Bus.Publish(actionCompletedEvent);

        (await fixture.ConsumerHarness.Consumed.Any<ActionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CompleteActionOfActionGroup>()).ShouldBeTrue();
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
    public async Task Consume_NodesSelectedForAction_ValidData_PublishesEstimateActionGroupDuration()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var nodesSelectedForAction = new NodesSelectedForAction(actionGroup.Actions.First(), Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(nodesSelectedForAction);

        (await fixture.ConsumerHarness.Consumed.Any<NodesSelectedForAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<EstimateActionGroupDuration>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionDurationEstimated_ValidData_PublishesPrepareAgvForStartingAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvId = Guid.NewGuid();
        var actionDurationEstimated = new ActionDurationEstimated(actionGroup.Actions.First(), agvId, actionGroup.CorrelationId);

        await fixture.PublishMessageAsync(actionDurationEstimated);

        var firstPublishedMessage = await fixture.GetFirstPublishedMessageAsync<PrepareAgvForStartingAction>();
        firstPublishedMessage.Action.ShouldBeEquivalentTo(actionDurationEstimated.Action);
        firstPublishedMessage.CorrelationId.ShouldBe(actionDurationEstimated.AgvId);
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionAbortionCompleted_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionAbortionCompleted(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_NoAssignedAgv_DoesNotPublishHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(null);
        await fixture.TestHarness.Bus.Publish(new ActionCanceled(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<ActionCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionCanceled_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture()
            .AddAssignedAgv(Guid.NewGuid());
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