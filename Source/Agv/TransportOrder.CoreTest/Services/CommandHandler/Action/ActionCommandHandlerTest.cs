using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartAction_ValidData_Publishes_StartActionCommandAndActionGroupChanged()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var mapper = ActionCommandHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes:new[]{"Node1", "Node2"});
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup);
        fixture.AgvIsReadyForBusy();

        var startAction = new StartAction(mapper.Map<AgvAction>(actionGroup.Actions.First()), agv.CorrelationId);

        await fixture.TestHarness!.Bus.Publish(startAction);
        (await fixture.ConsumerHarness!.Consumed.Any<StartAction>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartAction_FirstPickAction_Publishes_StartActionCommandAndActionGroupChangedActionGroupStarted()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var mapper = ActionCommandHandlerTestFixture.GetMapper();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes: new[] { "Node1", "Node2" });
        fixture.SetBusyThrowsException(false);
        fixture.AgvIsReadyForBusy();
        fixture.AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartAction(mapper.Map<AgvAction>(pickAction), agv.CorrelationId);

        await fixture.TestHarness!.Bus.Publish(startAction);
        (await fixture.ConsumerHarness!.Consumed.Any<StartAction>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupStarted>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartManualAction_ValidData_PublishesStartActionCommandAndActionGroupChanged()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = true;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes: new[] { "Node1", "Node2" });
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup);
        fixture.AgvIsReadyForBusy();

        var command = new TryStartManualAction(agv);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<TryStartManualAction>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartManualAction_AgvIsNotLocked_StartActionCommandAndActionGroupChangedNotPublished()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes: new[] { "Node1", "Node2" });
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup);

        var command = new TryStartManualAction(agv);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<TryStartManualAction>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeFalse();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_ValidData_PublishesStartActionCommandAndActionGroupChanged()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes: new[] { "Node1", "Node2" });
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: true);
        fixture.AgvIsReadyForBusy();

        var command = new TryStartNextActionInActionGroup(agv);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_NoUnfinishedAction_StartActionCommandAndActionGroupChangedNotPublished()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = true;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes: new[] { "Node1", "Node2" });
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction:false);

        var command = new TryStartNextActionInActionGroup(agv);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeFalse();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CompleteAction_ActionGroupNotCompleteNotChargingAction_PublishesActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv:agv.CorrelationId);
        var fixture = new ActionCommandHandlerTestFixture();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteAction(actionGroup.Actions.First().CorrelationId);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<CompleteAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCompleted>()).ShouldBeFalse();
        fixture.AgvSagaCommunicationService.Verify(x =>x.SetIdleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_CompleteAction_ActionGroupNotCompleteChargingAction_PublishesActionGroupChangedSetIdleNotCalled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionCommandHandlerTestFixture();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: false);

        var command = new CompleteAction(actionGroup.Actions.First().CorrelationId);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<CompleteAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCompleted>()).ShouldBeFalse();
        fixture.AgvSagaCommunicationService.Verify(x => x.SetIdleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_PublishesActionGroupCancelled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionCommandHandlerTestFixture();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup);

        var command = new CancelActionGroup(actionGroup.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_UnknownActionGroup_PublishesActionGroupCancelFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ActionCommandHandlerTestFixture();
        fixture.AddAgv(agv);

        var command = new CancelActionGroup(actionGroup.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelled>()).ShouldBeFalse();
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
        agv.IsLocked = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        actionGroup.GetType().GetProperty(nameof(actionGroup.State))!.SetValue(actionGroup, Enum.Parse<ActionGroupState>(invalidState));
        var fixture = new ActionCommandHandlerTestFixture();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup);

        var command = new CancelActionGroup(actionGroup.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(command);
        (await fixture.ConsumerHarness!.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelled>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
    }
}