using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvMode = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvMode;
using AgvStatus = Gebhardt.Acc.Common.Domain.Models.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using ParkingNodeSelectedForAgv = Gebhardt.Acc.TransportOrder.Messages.Events.Agv.ParkingNodeSelectedForAgv;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvNotReadyForBusy_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
       
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsLocked_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvNeedsCharging);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
        fixture.AgvHasUnfinishedDropAction(agv.CorrelationId);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartNextActionInActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_LockedAgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId", mode: new AgvMode(ModeType.Locked));
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
        fixture.AgvHasUnfinishedDropAction(agv.CorrelationId);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartNextActionInActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadDoesNotNeedsChargingOrNeedMaintenance_PublishesTryStartNewActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadNeedsCharging_DoesPublishTryStartNewActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId", batteryCharge: 10);
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryStartNewActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadNeedsCharging_AgvHasPendingModeChange_DoesNotPublishTryStartNewActionGroup()
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance);
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId", batteryCharge: 10, mode: mode);
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);
        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);

        await fixture.PublishMessageAsync(agvBecameIdle);

        await fixture.ShouldNotHavePublishedAsync<TryStartNextActionInActionGroup>();
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
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv); 

        await fixture.PublishMessageAsync(new AgvCanStartIdleAction(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<PrepareIdleActionOfAgv>();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvCanHandleActionsAgain_ValidData_TryReactivateChargingAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new ChargingAgvCanHandleActionsAgain(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    public async Task Consume_AgvBatteryChargeChanged_Publish_AgvNeedsCharging(int oldCharge, int newCharge)
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv)
            .WithInfiniteCharging(false);

        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge,
            new AgvBatteryStatus(new AgvStatus { CorrelationId = agv.CorrelationId },
                AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<AgvNeedsCharging>();
        await fixture.ShouldNotHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargeChanged_Publish_AbortChargeAction()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv)
            .WithInfiniteCharging(false);

        const double oldCharge = AgvEventHandlerTestFixture.MaxChargeUp - 1;
        const double newCharge = AgvEventHandlerTestFixture.MaxChargeUp;
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge,
            new AgvBatteryStatus(new AgvStatus { CorrelationId = agv.CorrelationId, BatteryCharge = newCharge },
                AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargeChanged_MinChargeUpReachedStillCharging_Publish_AgvBatteryChargedToMinimum()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv)
            .WithInfiniteCharging(false);

        const double oldCharge = AgvEventHandlerTestFixture.MinChargeUp - 1;
        const double newCharge = AgvEventHandlerTestFixture.MinChargeUp;
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge,
            new AgvBatteryStatus(new AgvStatus { CorrelationId = agv.CorrelationId, BatteryCharge = newCharge },
             AgvEventHandlerTestFixture.AgvSettingsMessage)));

        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldHavePublishedAsync<AgvBatteryChargedToMinimum>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargeChanged_InfiniteCharging_DoesNotPublish_AbortChargeAction()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv)
            .WithInfiniteCharging(true);

        const double oldCharge = AgvEventHandlerTestFixture.MaxChargeUp - 1;
        const double newCharge = AgvEventHandlerTestFixture.MaxChargeUp;
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge,
            new AgvBatteryStatus(new AgvStatus { CorrelationId = agv.CorrelationId, BatteryCharge = newCharge },
                AgvEventHandlerTestFixture.AgvSettingsMessage)));

        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.MinChargeUp)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.MaxChargeUp)]
    public async Task Consume_AgvBatteryChargeChanged_Publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv)
            .WithInfiniteCharging(false);
        
        await fixture.PublishMessageAsync(new AgvBatteryChargeChanged(oldCharge,
            new AgvBatteryStatus(new AgvStatus { CorrelationId = agv.CorrelationId, BatteryCharge = newCharge },
                AgvEventHandlerTestFixture.AgvSettingsMessage)));
        
        await fixture.ShouldHavePublishedAsync<ChargingAgvCanHandleActionsAgain>();
        await fixture.ShouldNotHavePublishedAsync<AgvNeedsCharging>();
    }

    [TestMethod]
    public async Task Consume_AgvMapReceived_ValidData_PublishesSaveMap()
    {
        const string agvSerialNumber = "AGV001";
        var agvCorrelationId = agvSerialNumber.ToGuid();
        var map = new Map(Guid.Empty, "", DateTime.MaxValue, "", 0, 0, 0.5f, 0.5f, 0.5f);
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgvGraph(AgvGraphGenerator.CreateDefaultGraph());

        await fixture.PublishMessageAsync(new AgvMapReceived(agvCorrelationId, agvSerialNumber, map));

        (await fixture.TestHarness.Published.Any<SaveMap>(f => f.Context.Message.Map.CorrelationId == map.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStatusReceived_AgvInRepository_PublishesUpdateAgvStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AgvStatusReceived(agv.CorrelationId, agv.SerialNumber, fixture.Mapper.Map<AgvStatus>(agv.AgvStatus!)));

        await fixture.ShouldHavePublishedAsync<UpdateAgvStatus>();
    }

    [TestMethod]
    public async Task Consume_AgvLastWillReceived_ValidData_PublishesGoOffline()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline();
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AgvLastWillReceived(agv.CorrelationId));
        await fixture.ShouldHavePublishedAsync<GoOffline>();
        var goOfflineCommand = fixture.TestHarness.Published.Select<GoOffline>().First().Context.Message;
        goOfflineCommand.CorrelationId.ShouldBe(agv.CorrelationId);
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
    public async Task Consume_ChargingNodeSelectedForAgv_PublishesCreateChargingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.PublishMessageAsync(new ChargingNodeSelectedForAgv(Guid.NewGuid(), "a charging node"));
        await fixture.ShouldHavePublishedAsync<CreateChargingActionGroup>();
    }

    [TestMethod]
    public async Task Consume_QueueNodeSelectedForAgv_PublishesCreateQueueingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        await fixture.PublishMessageAsync(new QueueNodeSelectedForAgv(Guid.NewGuid(), "a queue node"));
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvModeChangePending_NormalMode_NothingPublished()
    {
        var mode = new AgvMode(ModeType.Maintenance);
        mode.PrepareSwitch(ModeType.Normal);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new AgvModeChangePending(agv.CorrelationId, agv.Mode));
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ResetActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
    }

    [DataRow(CurrentActionGroupPolicy.FinishActionGroup, false)]
    [DataRow(CurrentActionGroupPolicy.FinishAction, false)]
    [DataRow(CurrentActionGroupPolicy.Pause, true)]
    [DataTestMethod]
    public async Task Consume_AgvModeChangePending_StartedActionGroup_PublishesPauseActionGroupDependingOnCurrentActionGroupPolicy(CurrentActionGroupPolicy policy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, currentActionPolicy: policy);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId, [], []);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChangePending(agv.CorrelationId, agv.Mode));
        (await fixture.TestHarness.Published.Any<PauseActionGroup>()).ShouldBe(shouldPublish);
    }

    [DataRow(AssignedActionGroupPolicy.LeaveAssigned, false)]
    [DataRow(AssignedActionGroupPolicy.Reset, true)]
    [DataTestMethod]
    public async Task Consume_AgvModeChangePending_CreatedAutomaticActionGroup_PublishesResetActionGroupDependingOnAssignedActionGroupsPolicy(AssignedActionGroupPolicy policy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, assignedActionPolicy: policy);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChangePending(agv.CorrelationId, agv.Mode));
        (await fixture.TestHarness.Published.Any<ResetActionGroup>()).ShouldBe(shouldPublish);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public async Task Consume_AgvModeChangePending_CreatedAutomaticActionGroup_PublishesResetActionGroupDependingOnCancelManualActionsPolicy(bool cancelManualActions)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, cancelManualActions: cancelManualActions);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChangePending(agv.CorrelationId, agv.Mode));
        (await fixture.TestHarness.Published.Any<CancelActionGroup>()).ShouldBe(cancelManualActions);
    }

    [TestMethod]
    public async Task Consume_AgvModeChangePending_NoStartedActionGroups_PublishesSwitchMode()
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, cancelManualActions: false);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvEventHandlerTestFixture()
            .WithActionGroups([actionGroup]);

        await fixture.PublishMessageAsync(new AgvModeChangePending(agv.CorrelationId, agv.Mode));
        await fixture.ShouldHavePublishedAsync<SwitchMode>();
    }

    [TestMethod]
    public async Task Consume_CriticalAgvHasNoCharger_ChargerAvailable_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger("charger")
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new CriticalAgvHasNoCharger(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvs>();
    }

    [TestMethod]
    public async Task Consume_CriticalAgvHasNoCharger_NoChargerNoAgvWithMinBatteryLevel_DoesNotPublish()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger(null)
            .WithMinBatteryAgvs([])
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new CriticalAgvHasNoCharger(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvs>();
    }

    [TestMethod]
    public async Task Consume_CriticalAgvHasNoCharger_NoChargerAgvWithMinBatteryLevel_PublishesSwitchChargingAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var highestChargingAgv = AgvGenerator.GenerateChargingAgv(2, batteryCharge: 85);
        var lowerChargingAgv = AgvGenerator.GenerateChargingAgv(1, batteryCharge: 55);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger(null)
            .WithMinBatteryAgvs([lowerChargingAgv, highestChargingAgv])
            .WithAgv(agv);

        var switchChargingAgvs = await fixture.PublishAndGetFirstMessageAsync<CriticalAgvHasNoCharger, SwitchChargingAgvs>(new CriticalAgvHasNoCharger(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        switchChargingAgvs.CorrelationId.ShouldBe(highestChargingAgv.CorrelationId);
        switchChargingAgvs.NonChargingAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargedToMinimum_NoAgvWithCriticalBatteryLevel_DoesNotPublish()
    {
        var fixture = new AgvEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvBatteryChargedToMinimum(Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvs>();
    }

    [TestMethod]
    public async Task Consume_AgvBatteryChargedToMinimum_AgvWithCriticalBatteryLevel_PublishesSwitchChargingAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 65);
        var fixture = new AgvEventHandlerTestFixture()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AgvBatteryChargedToMinimum(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<SwitchChargingAgvs>();
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvsFailed_OtherCandidatesAvailable_PublishesSwitchChargingAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var highestChargingAgv = AgvGenerator.GenerateChargingAgv(2, batteryCharge: 85);
        var lowerChargingAgv = AgvGenerator.GenerateChargingAgv(1, batteryCharge: 55);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger(null)
            .WithMinBatteryAgvs([lowerChargingAgv, highestChargingAgv])
            .WithAgv(agv);

        var switchChargingAgvs = await fixture.PublishAndGetFirstMessageAsync<SwitchChargingAgvsFailed, SwitchChargingAgvs>(new SwitchChargingAgvsFailed(agv.CorrelationId, highestChargingAgv.CorrelationId, 0));

        switchChargingAgvs.CorrelationId.ShouldBe(lowerChargingAgv.CorrelationId);
        switchChargingAgvs.NonChargingAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvsFailed_NoOtherCandidatesAvailable_DoesNotPublishSwitchChargingAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var highestChargingAgv = AgvGenerator.GenerateChargingAgv(2, batteryCharge: 85);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger(null)
            .WithMinBatteryAgvs([highestChargingAgv])
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new SwitchChargingAgvsFailed(agv.CorrelationId, highestChargingAgv.CorrelationId, 0));

        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvs>();
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvsFailed_ChargerAvailable_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);
        var highestChargingAgv = AgvGenerator.GenerateChargingAgv(2, batteryCharge: 85);
        var fixture = new AgvEventHandlerTestFixture()
            .WithReservedCharger("charger")
            .WithMinBatteryAgvs([highestChargingAgv])
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new SwitchChargingAgvsFailed(agv.CorrelationId, highestChargingAgv.CorrelationId, 0));

        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvs>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }
}