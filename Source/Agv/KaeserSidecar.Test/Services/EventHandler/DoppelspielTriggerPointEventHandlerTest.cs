using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DoppelspielTriggerPointEventHandlerTest
{
    [DataRow(ActionState.Started)]
    [DataRow(ActionState.Completed)]
    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupNotOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint(ActionState actionState)
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = actionState;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithinAklAgvLimit(true)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeTrue();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldNotBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupOccupied_AklAgvLimitNotUsed_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };

        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId : dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithinAklAgvLimit(true)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeTrue();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldNotBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]); 
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithinAklAgvLimit(false)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupWillBeOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithinAklAgvLimit(false)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerAlreadyQueued_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var previousPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        dropHostOrder.RegisterForDoppelspiel();
        previousPickOrder.RegisterForDoppelspiel();
        previousPickOrder.IsQueued = true;
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithinAklAgvLimit(true)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(previousPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerCriticalBattery_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: dropHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[dropOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(true)
            .WithinAklAgvLimit(true)
            .WithHostOrder(dropHostOrder)
            .WithNextPickOrder(nextPickOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<DroppingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        droppingAgvPassedDoppelspielTriggerPoint.ShouldPickNext.ShouldBeFalse();
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.PickNode.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_Picking_PublishesPickingAgvPassedDoppelspielTriggerPoint()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var pickOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var pickHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(pickOrderDetails);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(groupId: pickHostOrder.CorrelationId, assignedAgv: agvData.CorrelationId, source: OrderGenerator.KaeserConfiguration
            .StationMapping[pickOrderDetails.Source], destination: OrderGenerator.KaeserConfiguration
            .StationMapping[pickOrderDetails.Destination]);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;

        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithHostOrder(pickHostOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        var pickingAgvPassedDoppelspielTriggerPoint = fixture.TestHarness.Published.Select<PickingAgvPassedDoppelspielTriggerPoint>().First().Context.Message;
        pickingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agvData.CorrelationId);
        pickingAgvPassedDoppelspielTriggerPoint.PickActionTarget.Id.ShouldBe(actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Target.Node!.Id);
        pickingAgvPassedDoppelspielTriggerPoint.HostOrderCorrelationId.ShouldBe(pickHostOrder.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_NotPickDropActionGroup_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agvData.CorrelationId);

        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture();

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], actionGroup.Actions.First().Target.Node!.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DifferentTarget_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus { CorrelationId = agvData.CorrelationId };
        var pickOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var pickHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(pickOrderDetails);
        var actionGroup = pickHostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;

        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
            .WithAgvHasCriticalBatteryLevel(false)
            .WithHostOrder(pickHostOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], "different station with Doppelspiel");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }
}