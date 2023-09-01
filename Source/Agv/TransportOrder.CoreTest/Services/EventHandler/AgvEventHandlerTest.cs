using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Messages.Commands.ActionGroup;
using TransportOrder.Messages.Commands.Agv;
using TransportOrder.Messages.Events.ActionGroup;
using TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsNotCarryingLoadAndNeedsCharging_PublishesCreateChargingActionGroup()
    {
        var fixture =  new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy:true, needsCharging:true, isCarryingLoad:false, needsMaintenance:false);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateChargingActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsNotCarryingLoadDoesntNeedsChargingNeedsMaintenance_PublishesCreateMaintenanceActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: true);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateMaintenanceActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsLocked_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = true;
        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: false);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: true, needsMaintenance: false);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsNotCarryingLoadDoesntNeedsChargingOrNeedMaintanace_PublishesTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: false);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvNotReadyForBusy_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        
        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: false, needsCharging: false, isCarryingLoad: false, needsMaintenance: true);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateMaintenanceActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<CreateChargingActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadAndNeedsCharging_PublishesCreateChargingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: true, isCarryingLoad: false, needsMaintenance: false);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateChargingActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadDoesntNeedsChargingNeedsMaintanace_PublishesCreateMaintenanceActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: true);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateMaintenanceActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsLocked_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = true;
        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: false);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;
        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: true, needsMaintenance: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadDoesntNeedsChargingOrNeedMaintanace_PublishesTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsLocked = false;

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: true, needsCharging: false, isCarryingLoad: false, needsMaintenance: false);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeTrue();

    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvNotReadyForBusy_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();

        fixture.AddAgv(agv);
        fixture.SetupAgvStates(isReadyForBusy: false, needsCharging: false, isCarryingLoad: false, needsMaintenance: true);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness!.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateMaintenanceActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<CreateChargingActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvsCanHandleActionsAgainEvent_ValidData_PublishesTryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness!.Bus.Publish(new ChargingAgvsCanHandleActionsAgainEvent() );
        (await fixture.ConsumerHarness!.Consumed.Any<ChargingAgvsCanHandleActionsAgainEvent>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvCanStartIdleAction_AgvIsEligibleForCharging_PublishesCreateChargingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.SetAgvEligibleForCharging(isEligibleForCharging: true);
        await fixture.TestHarness!.Bus.Publish(new AgvCanStartIdleAction(agv));
        (await fixture.ConsumerHarness!.Consumed.Any<AgvCanStartIdleAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateChargingActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvCanStartIdleAction_AgvIsNotEligibleForCharging_PublishesCreateParkingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.SetAgvEligibleForCharging(isEligibleForCharging:false);
        await fixture.TestHarness!.Bus.Publish(new AgvCanStartIdleAction(agv));
        (await fixture.ConsumerHarness!.Consumed.Any<AgvCanStartIdleAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateParkingActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvsCanHandleActionsAgainEvent_ValidData_TryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness!.Bus.Publish(new ChargingAgvsCanHandleActionsAgainEvent());
        (await fixture.ConsumerHarness!.Consumed.Any<ChargingAgvsCanHandleActionsAgainEvent>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    public async Task Consume_AgvBatteryChargeChanged_publish_AgvNeedsCharging(int oldCharge, int newCharge)
    {
        string agvId = "AGV001";
        var correlationId = agvId.ToGuid();

        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

        bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
        consumeCheck.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelGood)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelFull)]
    public async Task Consume_AgvBatteryChargeChanged_publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
    {
        string agvId = "AGV001";
        var correlationId = agvId.ToGuid();

        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

        bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
        consumeCheck.ShouldBeTrue();

        bool publishCheck = await fixture.TestHarness.Published.Any<ChargingAgvsCanHandleActionsAgainEvent>();
        publishCheck.ShouldBeTrue();
    }
}