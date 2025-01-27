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
    public void UpdateAgvStatus_BatteryChanged_StatusUpdated()
    {
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double noticeableBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold * 1.01d;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge - noticeableBatteryChange, false, false);

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryChargeChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryChangedAndVoltageCurrentTemperature_StatusUpdated()
    {
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, null, 10d, 30.5d, 20.2d);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double noticeableBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold * 1.01d;
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge - noticeableBatteryChange, false, false, null, 15d, 35.5d, 25.2d);

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryChargeChanged.ShouldBeTrue();
        lastNodeIdChanged.ShouldBeFalse();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryLevelChangedInsignificantly_StatusUpdated()
    {
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        const double insignificantBatteryChange = TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold * 0.99d;
        var newStatus = new AgvStatus(agv.CorrelationId, "orderId", true, false, agv.AgvStatus!.BatteryCharge - insignificantBatteryChange, true, true);

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryChargeChanged.ShouldBeFalse();
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
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false, lastNodeId: null);
        var agv = AgvGenerator.GenerateIdleAgv(previousStatus);
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge, false, false, "node", estimatedFullyChargedTime: DateTime.UtcNow, lastBatteryReplacement: DateTime.UtcNow.AddHours(-5));

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryChargeChanged.ShouldBeFalse();
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
}