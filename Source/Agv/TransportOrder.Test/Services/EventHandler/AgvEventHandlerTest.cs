using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using ParkingNodeSelectedForAgv = Gebhardt.Acc.TransportOrder.Messages.Events.Agv.ParkingNodeSelectedForAgv;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsInUserMode_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.SetUserMode();
        fixture.AddAgv(agv);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);
        
        await fixture.ShouldHavePublishedAsync<TryStartManualAction>();
    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsInMaintenanceMode_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.SetMaintenanceMode(null);
        fixture.AddAgv(agv);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);

        await fixture.ShouldHavePublishedAsync<TryStartManualAction>();
    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvNotReadyForBusy_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);
        
        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartManualAction>();
        await fixture.ShouldNotHavePublishedAsync<StartChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsLocked_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock();
        fixture.AddAgv(agv);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartManualAction>();
        await fixture.ShouldNotHavePublishedAsync<StartChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsInMaintenanceMode_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.SetMaintenanceMode(null);
        fixture.AddAgv(agv);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartManualAction>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsInUserMode_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.SetUserMode();
        fixture.AddAgv(agv);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartManualAction>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        fixture.AddAgv(agv);
        fixture.AgvHasUnfinishedDropAction(agv.CorrelationId);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartNextActionInActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_LockedAgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.Lock();
        fixture.AddAgv(agv);
        fixture.AgvHasUnfinishedDropAction(agv.CorrelationId);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartNextActionInActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadDoesNotNeedsChargingOrNeedMaintenance_PublishesTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        fixture.AddAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);
        
        await fixture.ShouldHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingNeedCharging_DoesNotPublishTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId", batteryCharge: 10);
        fixture.AddAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartManualAction>();
        await fixture.ShouldNotHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvCanHandleActionsAgain_ValidData_PublishesTryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();

        await fixture.PublishMessageAsync(new ChargingAgvCanHandleActionsAgain(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    public async Task Consume_AgvCanStartIdleAction_AgvWithBatteryLevelGood_PublishesPrepareParkingOfAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.PublishMessageAsync(new AgvCanStartIdleAction(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<PrepareParkingOfAgv>();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvCanHandleActionsAgain_ValidData_TryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.PublishMessageAsync(new ChargingAgvCanHandleActionsAgain(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    public async Task Consume_AgvBatteryChargeChanged_Publish_AgvNeedsCharging(int oldCharge, int newCharge)
    {
        var fixture = new AgvEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge, new AgvBatteryStatus(new AgvStatus(), AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<AgvNeedsCharging>();
        await fixture.ShouldNotHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargeChanged_Publish_AbortChargeAction()
    {
        var fixture = new AgvEventHandlerTestFixture();

        const double oldCharge = AgvEventHandlerTestFixture.MaxChargeUp - 1;
        const double newCharge = AgvEventHandlerTestFixture.MaxChargeUp;
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge, new AgvBatteryStatus(new AgvStatus { BatteryCharge = newCharge }, AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.MinChargeUp)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.MaxChargeUp)]
    public async Task Consume_AgvBatteryChargeChanged_Publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge, new AgvBatteryStatus(new AgvStatus { BatteryCharge = newCharge }, AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    public async Task Consume_AgvMapReceived_ValidData_PublishesSaveMap()
    {
        const string agvSerialNumber = "AGV001";
        var agvCorrelationId = agvSerialNumber.ToGuid();
        var map = new Map(new Guid(), "", DateTime.MaxValue, "", 0, 0, 0.5f, 0.5f, 0.5f);
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgvGraph(AgvGraphGenerator.CreateDefaultGraph());

        await fixture.PublishMessageAsync(new AgvMapReceived(agvCorrelationId, agvSerialNumber, map));

        (await fixture.TestHarness.Published.Any<SaveMap>(f => f.Context.Message.Map.CorrelationId == map.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStatusReceived_AgvInRepository_PublishesUpdateAgvStatus()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.PublishMessageAsync(new AgvStatusReceived(agv.CorrelationId, agv.SerialNumber, fixture.Mapper.Map<AgvStatus>(agv.AgvStatus!)));

        await fixture.ShouldHavePublishedAsync<UpdateAgvStatus>();
    }

    [TestMethod]
    public async Task Consume_ScheduledAgvWentOffline_ValidData_PublishesGoOffline()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        ScheduledMessage<AgvWentOffline> offlineMessage = await fixture.ScheduleMessage(TimeSpan.FromMilliseconds(0), new AgvWentOffline(agv.CorrelationId, DateTime.UtcNow));
        agv.SetOnline(offlineMessage.TokenId);
        fixture.AddAgv(agv);
        await fixture.ShouldHaveConsumedAsync<AgvWentOffline>();
        await fixture.ShouldHavePublishedAsync<GoOffline>();
        var goOfflineCommand = fixture.TestHarness.Published.Select<GoOffline>().First().Context.Message;
        goOfflineCommand.OfflineTokenId.ShouldBe(offlineMessage.TokenId);
    }

    [TestMethod]
    public async Task Consume_AgvPreparedForStartingAction_ValidData_StartActionOfActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        await fixture.PublishMessageAsync(new AgvPreparedForStartingAction(agv.CorrelationId, actionGroup.Actions.First()));

        await fixture.ShouldHavePublishedAsync<StartActionOfActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLoadStatusReceived_UpdateAgvLoadStatus()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var agvLoadStatusReceived = new AgvLoadStatusReceived(agvCorrelationId, cargos.ToList().AsReadOnly());

        await fixture.PublishMessageAsync(agvLoadStatusReceived);

        await fixture.ShouldHavePublishedAsync<UpdateCargo>();
        var updateLoadStatus = fixture.TestHarness.Published.Select<UpdateCargo>().First().Context.Message;
        updateLoadStatus.CorrelationId.ShouldBe(agvCorrelationId);
        updateLoadStatus.Cargos.ShouldBeEquivalentTo(cargos);
    }

    [TestMethod]
    public async Task Consume_ParkingNodeSelectedForAgv_PublishesCreateActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.PublishMessageAsync(new ParkingNodeSelectedForAgv(Guid.NewGuid(), "a parking node"));
        await fixture.ShouldHavePublishedAsync<CreateParkingActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ChargingNodeSelectedForAgv_PublishesStartChargingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.PublishMessageAsync(new ChargingNodeSelectedForAgv(Guid.NewGuid(), "a charging node"));
        await fixture.ShouldHavePublishedAsync<StartChargingActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_NormalModeNoPausedActionGroup_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_UserModeStartedActionGroup_PublishesPauseActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetUserMode();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions: [], ActionGroupState.Started);
        actionGroup.Assign(agv);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_UserModeNoStartedActionGroup_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetUserMode();
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_MaintenanceModeStartedActionGroup_PublishesPauseActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetMaintenanceMode("reason");
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions: [], ActionGroupState.Started);
        actionGroup.Assign(agv);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldHavePublishedAsync<PauseActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_MaintenanceModeNoAssignedActionGroup_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetMaintenanceMode("reason");
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<ResetActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_MaintenanceModeAssignedCreatedNonManualActionGroup_PublishesResetActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetMaintenanceMode("reason");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv:agv.CorrelationId);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldHavePublishedAsync<ResetActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_MaintenanceModeAssignedCreatedManualActionGroup_CancelManualActions_PublishesCancelActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetMaintenanceMode("reason");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode, true));
        await fixture.ShouldNotHavePublishedAsync<ResetActionGroup>();
        await fixture.ShouldHavePublishedAsync<CancelActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_MaintenanceModeAssignedCreatedManualActionGroup_NoCancelManualActions_PublishesCancelActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetMaintenanceMode("reason");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_LockedNoStartedActionGroup_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock("reason");
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new AgvModeChanged(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
    }
}