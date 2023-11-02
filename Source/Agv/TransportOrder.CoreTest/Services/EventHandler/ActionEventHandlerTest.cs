using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCreatedForAgv_ValidData_PublishesSelectNodesForAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var mapper = ActionEventHandlerTestFixture.GetMapper();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(mapper.Map<ActionGroup>(actionGroup), Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SelectNodesForAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionCompletedEvent_ValidData_PublishesCompleteAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionCompletedEvent = new ActionCompleted(Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(actionCompletedEvent);

        (await fixture.ConsumerHarness!.Consumed.Any<ActionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CompleteAction>(f =>
            f.Context.Message == new CompleteAction(actionCompletedEvent.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartingActionForAgvFailed_ValidData_PublishesHandleAgvActionStartFailure()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var startingActionForAgvFailed = new StartingActionForAgvFailed(Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(startingActionForAgvFailed);

        (await fixture.ConsumerHarness!.Consumed.Any<StartingActionForAgvFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleAgvActionStartFailure>(f =>
            f.Context.Message == new HandleAgvActionStartFailure(startingActionForAgvFailed.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NonChargingActionCompleted_ValidData_PublishesHandleActionCompletion()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var nonChargingActionCompleted = new NonChargingActionCompleted(Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(nonChargingActionCompleted);

        (await fixture.ConsumerHarness!.Consumed.Any<NonChargingActionCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleActionCompletion>(f =>
            f.Context.Message == new HandleActionCompletion(nonChargingActionCompleted.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_NodesSelectedForAction_ValidData_PublishesPrepareAgvForStartingAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var nodesSelectedForAction = new NodesSelectedForAction(actionGroup.Actions.First(), Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(nodesSelectedForAction);

        (await fixture.ConsumerHarness!.Consumed.Any<NodesSelectedForAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<PrepareAgvForStartingAction>()).ShouldBeTrue();
    }
}