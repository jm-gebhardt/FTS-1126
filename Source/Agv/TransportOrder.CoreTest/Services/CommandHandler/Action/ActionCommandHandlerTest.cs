using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Messages.Commands.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartAction_ValidData_Publishes_StartActionCommandAndActionGroupChanged()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddRoute(nodes:new[]{"Node1", "Node2"});
        fixture.SetBusyThrowsException(false);
        fixture.AddActionGroup(actionGroup);
       
        var startAction = new StartAction(actionGroup.Actions.First(), agv.CorrelationId);

        await fixture.TestHarness!.Bus.Publish(startAction);
        (await fixture.ConsumerHarness!.Consumed.Any<StartAction>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<StartActionCommand>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
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
}