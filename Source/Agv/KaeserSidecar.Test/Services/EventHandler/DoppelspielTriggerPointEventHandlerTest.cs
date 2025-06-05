using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DoppelspielTriggerPointEventHandlerTest
{
    [DataRow(ActionState.Started)]
    [DataRow(ActionState.Completed)]
    [DataTestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupNotOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint(ActionState actionState)
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(true);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = actionState;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(false)
                .WithStationGroup(false, false)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(nextPickOrder);

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
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupOccupied_AklAgvLimitNotUsed_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(false);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(false)
                .WithStationGroup(true, false)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(nextPickOrder);

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
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(true);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(false)
                .WithStationGroup(true, false)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(nextPickOrder);

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
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartner_StationGroupWillBeOccupied_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(true);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(false)
                .WithStationGroup(false, true)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(nextPickOrder);

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
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerAlreadyQueued_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(true);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var previousPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        dropHostOrder.RegisterForDoppelspiel();
        previousPickOrder.RegisterForDoppelspiel();
        previousPickOrder.IsQueued = true;
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(false)
                .WithStationGroup(false, false)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(previousPickOrder);

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
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DroppingDoppelspielPartnerCriticalBattery_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var systemStatus = new SystemStatus(Guid.NewGuid(), true, 0, string.Empty);
        systemStatus.SetUseAklAgvLimit(true);
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = new AgvStatus() { CorrelationId = agvData.CorrelationId };
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        actionGroup.AssignedAgv = agvData.CorrelationId;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var fixture = new DoppelspielTriggerPointEventHandlerTestFixture()
                .WithAgvHasCriticalBatteryLevel(true)
                .WithStationGroup(false, false)
                .WithSystemStatus(systemStatus)
                .WithHostOrder(dropHostOrder)
                .WithNextPickOrder(nextPickOrder);

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
                .WithAgvHasCriticalBatteryLevel(false)
                .WithHostOrder(pickHostOrder);

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
            .WithAgvHasCriticalBatteryLevel(false)
            .WithHostOrder(pickHostOrder);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, actionGroup, [], [], "different station with Doppelspiel");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
    }
}