using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Agv;

[TestClass]
public class AgvTest
{
    [TestMethod]
    public void GoOffline_ValidData_ResetsIsOnlineAndOfflineToken()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public void GoOffline_WrongToken_TokenAndIsOnlineUnchanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline();

        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public void SetOnline_ValidData_SetsIsOnlineAndOfflineToken()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline();
        agv.IsOnline.ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateAgvStatus_WasNull_StatusUpdated()
    {
        var status = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, lastNodeId: "node");
        var agv = new TransportOrder.Domain.Models.Agv.Agv();

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(status);

        batteryStatusChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeTrue();
        agv.AgvStatus.ShouldBe(status);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryChanged_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double noticeableBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold * 1.01d;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge - noticeableBatteryChange, false, false, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeTrue();
        batteryChargeChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryChangedAndVoltageCurrentTemperature_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, null, 10d, 30.5d, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double noticeableBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold * 1.01d;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge - noticeableBatteryChange, false, false, null, 15d, 35.5d, 25.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_VoltageChanged_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, null, 10d, 30.5d, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        double newVoltage = TransportOrder.Domain.Models.Agv.Agv.BatteryVoltageThreshold * 1.01d + previousStatus.BatteryVoltage;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, 100.0f, false, false, null, newVoltage, 30.5d, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_StationIdsChanged_StatusUpdated()
    {
        var stations = new List<string> { "PS01" };
        var previousStatus = AgvStatusGenerator.Generate(stationIds: []);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        var newStatus = AgvStatusGenerator.Generate(stationIds: stations);

        agv.UpdateAgvStatus(newStatus);

        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_CurrentChanged_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, null, 10d, 30.5d, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        double newCurrent = TransportOrder.Domain.Models.Agv.Agv.BatteryCurrentIdleThreshold * 1.01d + previousStatus.BatteryCurrent;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, 100.0f, false, false, null, 10d, newCurrent, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_TemperatureChanged_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, null, 10d, 30.5d, 20.2d, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        double newTemperature = TransportOrder.Domain.Models.Agv.Agv.BatteryTemperatureThreshold * 1.01d + previousStatus.BatteryTemperature;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, 100.0f, false, false, null, 10d, 30.5d, newTemperature, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryLevelChangedInsignificantly_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, true, false, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double insignificantBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold * 0.99d;
        var newStatus = new AgvStatus(agv.CorrelationId, "orderId", true, false, agv.AgvStatus!.BatteryCharge - insignificantBatteryChange, true, true, estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeFalse();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.BatteryCharge.ShouldBe(previousStatus.BatteryCharge);
        agv.AgvStatus.Busy.ShouldBe(newStatus.Busy);
        agv.AgvStatus.Automatic.ShouldBe(newStatus.Automatic);
        agv.AgvStatus.OrderId.ShouldBe(newStatus.OrderId);
        agv.AgvStatus.Disturbed.ShouldBe(newStatus.Disturbed);
        agv.AgvStatus.Charging.ShouldBe(newStatus.Charging);
    }

    [TestMethod]
    public void UpdateAgvStatus_LastNodeIdChanged_StatusUpdated()
    {
        var now = DateTime.UtcNow;
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, lastNodeId: null, estimatedFullyChargedTime: now, lastBatteryReplacement: now);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge, false, false, "node", estimatedFullyChargedTime: now, lastBatteryReplacement: now);

        (bool batteryStatusChanged, _, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryStatusChanged.ShouldBeFalse();
        lastNodeIdChanged.ShouldBeTrue();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void IsAvailable_IsReadyIdleAndUnlocked_IsAvailable()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "node");
        agv.IsAvailable(ignoreLocked: false).ShouldBeTrue();
    }

    [DataRow(true, true)]
    [DataRow(false, false)]
    [TestMethod]
    public void IsAvailable_IsReadyIdleAndLocked_IsAvailableIfLockedIsIgnored(bool ignoreLocked, bool expectedIsAvailable)
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "node", mode: new AgvMode(ModeType.Locked));
        agv.IsAvailable(ignoreLocked: ignoreLocked).ShouldBe(expectedIsAvailable);
    }

    [TestMethod]
    public void IsAvailable_IsNotReady_NotAvailable()
    {
        var agv = AgvGenerator.GenerateRawAgv();

        agv.IsAvailable().ShouldBeFalse();
        agv.IsReady.ShouldBeFalse();
    }

    [TestMethod]
    public void IsReady_WhenHasError_ShouldBeFalse()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsReady.ShouldBeTrue();
        agv.HasError = true;
        agv.IsReady.ShouldBeFalse();
    }

    [TestMethod]
    public void ShouldRequestData_DataNeverRequestedNoData_ShouldRequestData()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.DataExist = false;
        agv.AgvDataRequestedLastTime = null;

        agv.ShouldRequestData().ShouldBeTrue();
        agv.AgvDataRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void ShouldRequestGraph_GraphNeverRequestedNoGraph_ShouldRequestGraph()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.GraphExists = false;
        agv.AgvGraphRequestedLastTime = null;
        agv.AgvStatus = new AgvStatus();

        agv.ShouldRequestGraph().ShouldBeTrue();
        agv.AgvGraphRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void ShouldRequestGraph_GraphRequestedBeforeIntervalNoGraph_ShouldRequestGraph()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.GraphExists = false;
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow.AddSeconds(-10);
        agv.AgvStatus = new AgvStatus();

        agv.ShouldRequestGraph().ShouldBeTrue();
        agv.AgvGraphRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void ShouldRequestGraph_GraphRequestedInIntervalNoGraph_ShouldNotRequestGraph()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.GraphExists = false;
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow.AddSeconds(-9);
        agv.AgvStatus = new AgvStatus();

        agv.ShouldRequestGraph().ShouldBeFalse();
        agv.AgvGraphRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void ShouldRequestGraph_GraphRequestedBeforeIntervalWithGraph_ShouldRequestGraph()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.GraphExists = true;
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow.AddDays(-1);
        agv.AgvStatus = new AgvStatus();

        agv.ShouldRequestGraph().ShouldBeTrue();
        agv.AgvGraphRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void PrepareLockedMode_ReasonInitiatingUserAndPoliciesGiven_ModeSwitchPreparedWithGivenProperties()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        const string initiatingUser = "a name";
        const LockReason lockReason = LockReason.Other;
        const string reasonDescription = "a reason description";
        const AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.LeaveAssigned;
        const CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.Pause;
        const bool cancelManualActions = true;
        agv.PrepareLockedMode(lockReason, reasonDescription, initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.Locked);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.LockReason.ShouldBe(lockReason);
        agv.Mode.MaintenanceReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBe(reasonDescription);
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(assignedActionPolicy);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(currentActionPolicy);
        agv.Mode.CancelManualActions.ShouldBe(cancelManualActions);
        agv.Mode.ChangeTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void PrepareLockedMode_ReasonInitiatingUserAndPoliciesNotGiven_ModeSwitchPreparedWithDefaults()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.PrepareLockedMode();

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.PendingType.ShouldBe(ModeType.Locked);
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBeNull();
        agv.Mode.LockReason.ShouldBeNull();
        agv.Mode.MaintenanceReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBeNull();
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(AssignedActionGroupPolicy.Reset);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(CurrentActionGroupPolicy.FinishAction);
        agv.Mode.CancelManualActions.ShouldBeFalse();
    }

    [TestMethod]
    public void Unlock_ValidData_ModeSwitched()
    {
        var agv = AgvGenerator.GenerateRawAgv(mode: new AgvMode(ModeType.Locked));
        agv.IsLocked.ShouldBeTrue();
        agv.Mode.Type.ShouldBe(ModeType.Locked);

        const string initiatingUser = "a name";
        agv.Unlock(initiatingUser);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.PendingType.ShouldBeNull();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.LockReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBeNull();
        agv.Mode.ChangeTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void PrepareUserMode_InitiatingUserAndPoliciesGiven_ModeSwitchPreparedWithGivenProperties()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        const string initiatingUser = "a name";
        const AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.Reset;
        const CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.FinishActionGroup;
        const bool cancelManualActions = true;
        agv.PrepareUserMode(initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.UserMode);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.LockReason.ShouldBeNull();
        agv.Mode.MaintenanceReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBeNull();
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(assignedActionPolicy);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(currentActionPolicy);
        agv.Mode.CancelManualActions.ShouldBe(cancelManualActions);
    }

    [TestMethod]
    public void PrepareUserMode_InitiatingUserAndPoliciesGiven_ModeSwitchPreparedWithDefaults()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.PrepareUserMode();

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.UserMode);
        agv.Mode.InitiatingUser.ShouldBeNull();
        agv.Mode.LockReason.ShouldBeNull();
        agv.Mode.MaintenanceReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBeNull();
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(AssignedActionGroupPolicy.LeaveAssigned);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(CurrentActionGroupPolicy.Pause);
        agv.Mode.CancelManualActions.ShouldBeFalse();
        agv.Mode.ChangeTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void PrepareMaintenanceMode_ReasonInitiatingUserAndPoliciesGiven_ModeSwitchPreparedWithGivenProperties()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        const string initiatingUser = "a name";
        const MaintenanceReason maintenanceReason = MaintenanceReason.Other;
        const string reasonDescription = "a reason";
        const AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.LeaveAssigned;
        const CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.FinishActionGroup;
        const bool cancelManualActions = true;
        agv.PrepareMaintenanceMode(maintenanceReason, reasonDescription, initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.Maintenance);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.MaintenanceReason.ShouldBe(maintenanceReason);
        agv.Mode.ReasonDescription.ShouldBe(reasonDescription);
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(assignedActionPolicy);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(currentActionPolicy);
        agv.Mode.CancelManualActions.ShouldBe(cancelManualActions);
    }

    [TestMethod]
    public void PrepareMaintenanceMode_InitiatingUserAndPoliciesNotGiven_ModeSwitchPreparedWithDefault()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        const MaintenanceReason maintenanceReason = MaintenanceReason.Other;
        const string reasonDescription = "reason description";
        agv.PrepareMaintenanceMode(maintenanceReason, reasonDescription);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.Maintenance);
        agv.Mode.InitiatingUser.ShouldBeNull();
        agv.Mode.MaintenanceReason.ShouldBe(maintenanceReason);
        agv.Mode.ReasonDescription.ShouldBe(reasonDescription);
        agv.Mode.AssignedActionGroupPolicy.ShouldBe(AssignedActionGroupPolicy.Reset);
        agv.Mode.CurrentActionGroupPolicy.ShouldBe(CurrentActionGroupPolicy.Pause);
        agv.Mode.CancelManualActions.ShouldBe(false);
        agv.Mode.ChangeTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void SetNormalMode_ValidData_ModeSwitched()
    {
        var agv = AgvGenerator.GenerateRawAgv(mode: new AgvMode(ModeType.Locked));
        agv.IsLocked.ShouldBeTrue();
        agv.Mode.Type.ShouldBe(ModeType.Locked);

        const string initiatingUser = "a name";
        agv.SetNormalMode(initiatingUser);

        agv.IsLocked.ShouldBeFalse();
        agv.Mode.PendingType.ShouldBeNull();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.LockReason.ShouldBeNull();
        agv.Mode.MaintenanceReason.ShouldBeNull();
        agv.Mode.ReasonDescription.ShouldBeNull();
        agv.Mode.ChangeTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void SwitchMode_PendingMode_ModeSwitched()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.PrepareLockedMode();
        agv.IsLocked.ShouldBeFalse();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.PendingType.ShouldBe(ModeType.Locked);
        agv.HasPendingModeSwitch.ShouldBeTrue();

        agv.SwitchMode();

        agv.IsLocked.ShouldBeTrue();
        agv.Mode.Type.ShouldBe(ModeType.Locked);
        agv.Mode.PendingType.ShouldBeNull();
        agv.HasPendingModeSwitch.ShouldBeFalse();
        agv.CurrentAgvState.ShouldBe(AgvState.Idle);
    }

    [DataRow(ModeType.Normal, null, false)]
    [DataRow(ModeType.Normal, ModeType.Maintenance, true)]
    [DataRow(ModeType.Normal, ModeType.UserMode, true)]
    [DataRow(ModeType.UserMode, null, true)]
    [DataRow(ModeType.UserMode, ModeType.Normal, true)]
    [DataRow(ModeType.UserMode, ModeType.Maintenance, true)]
    [DataRow(ModeType.UserMode, ModeType.Locked, true)]
    [DataRow(ModeType.Maintenance, null, true)]
    [DataRow(ModeType.Maintenance, ModeType.Normal, true)]
    [DataRow(ModeType.Maintenance, ModeType.UserMode, true)]
    [DataRow(ModeType.Maintenance, ModeType.Locked, true)]
    [DataTestMethod]
    public void ShouldStartManualAction_ValidData_ValueDependsOnPendingAndCurrentMode(ModeType currentMode, ModeType? pendingMode, bool expectedValue)
    {
        var mode = new AgvMode(currentMode, Guid.NewGuid());
        if (pendingMode != null)
        {
            mode.PrepareSwitch(pendingMode.Value);
        }
        var agv = AgvGenerator.GenerateIdleAgv(mode : mode);

        agv.ShouldStartManualAction.ShouldBe(expectedValue);
    }

    [TestMethod]
    public void TryTransitionToBusy_ValidData_CurrentStateUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var transitionTime = agv.TimeOfStateTransition;

        agv.TryTransitionToBusy(true).ShouldBeTrue();
        agv.TimeOfStateTransition.ShouldNotBe(transitionTime);
        agv.CurrentAgvState.ShouldBe(AgvState.Busy);
    }

    [TestMethod]
    public void ResetZones_ValidData_ZonesReset()
    {
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: ["z1", "z2"]);
        agv.AgvStatus!.ResetZones();

        agv.AgvStatus!.ZoneIds.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveZone_ValidData_ZoneRemoved()
    {
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: ["z1", "z2"]);
        bool res = agv.AgvStatus!.RemoveZone("z1");

        res.ShouldBeTrue();
        agv.AgvStatus!.ZoneIds!.Count.ShouldBe(1);
        agv.AgvStatus!.ZoneIds!.First().ShouldBe("z2");
    }

    [TestMethod]
    public void RemoveZone_ZoneDoesNotExist_NoZoneRemoved()
    {
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: ["z1", "z2"]);
        bool res = agv.AgvStatus!.RemoveZone("z3");

        res.ShouldBeFalse();
        agv.AgvStatus!.ZoneIds!.Count.ShouldBe(2);
        agv.AgvStatus!.ZoneIds!.ShouldContain("z1");
        agv.AgvStatus!.ZoneIds!.ShouldContain("z2");
    }
}