using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DoppelspielTriggerPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        dropHostOrder.SetDoppelspielPartner(nextPickOrder.CorrelationId);
        nextPickOrder.SetDoppelspielPartner(dropHostOrder.CorrelationId);
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasLowBatteryLevel(false)
                .WithHostOrders(dropHostOrder, nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeTrue();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerAlreadyQueued_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var previousPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        dropHostOrder.SetDoppelspielPartner(previousPickOrder.CorrelationId);
        previousPickOrder.SetDoppelspielPartner(dropHostOrder.CorrelationId);
        previousPickOrder.IsQueued = true;
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasLowBatteryLevel(false)
                .WithHostOrders(dropHostOrder, previousPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerLowBattery_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        dropHostOrder.SetDoppelspielPartner(nextPickOrder.CorrelationId);
        nextPickOrder.SetDoppelspielPartner(dropHostOrder.CorrelationId);
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasLowBatteryLevel(true)
                .WithHostOrders(dropHostOrder, nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_Picking_PublishesPickingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var pickOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var pickHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(pickOrderDetails);
        var actionGroup = pickHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, pickOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
       
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasLowBatteryLevel(false)
                .WithHostOrders(pickHostOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Target.NodeId!);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        var pickingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<PickingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        pickingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        pickingAgvPassedDoppelspielTriggerPoint.PickActionTarget.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Target.NodeId!);
        pickingAgvPassedDoppelspielTriggerPoint.HostOrderCorrelationId.ShouldBe(pickHostOrder.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_NotPickDropActionGroup_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agvData.CorrelationId);

        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture();

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First().Target.NodeId!);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DifferentTarget_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var pickOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var pickHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(pickOrderDetails);
        var actionGroup = pickHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, pickOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;

        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasLowBatteryLevel(false)
            .WithHostOrders(pickHostOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], "different station with Doppelspiel");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }
}