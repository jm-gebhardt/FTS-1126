using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Models.Agv;

[TestClass]
public class AgvTest
{
    [TestMethod]
    public void GoOffline_ValidData_ResetsIsOnlineAndOfflineToken()
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId");
        agv.SetOnline(Guid.NewGuid());

        agv.GoOffline(agv.AgvOfflineTokenId!.Value);
        agv.IsOnline.ShouldBeFalse();
        agv.AgvOfflineTokenId.ShouldBeNull();
    }

    [TestMethod]
    public void GoOffline_WrongToken_TokenAndIsOnlineUnchanged()
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId");
        agv.SetOnline(Guid.NewGuid());

        agv.GoOffline(Guid.NewGuid());
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldNotBeNull();
    }

    [TestMethod]
    public void SetOnline_ValidData_SetsIsOnlineAndOfflineToken()
    {
        var offlineToken = Guid.NewGuid();
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId");
        agv.SetOnline(offlineToken);
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldBe(offlineToken);
    }

    [TestMethod]
    public void UpdateAgvStatus_BatteryChanged_StatusUpdated()
    {
        var previousStatus = new AgvStatus(Guid.NewGuid(), null, false, false, 100.0d, false, false);
        var agv = new TransportOrder.Domain.Models.Agv.Agv(previousStatus.CorrelationId, "agvId", previousStatus);
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
        var agv = new TransportOrder.Domain.Models.Agv.Agv(previousStatus.CorrelationId, "agvId", previousStatus);
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
        var agv = new TransportOrder.Domain.Models.Agv.Agv(previousStatus.CorrelationId, "agvId", previousStatus);
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
        var agv = new TransportOrder.Domain.Models.Agv.Agv(previousStatus.CorrelationId, "agvId", previousStatus);
        var newStatus = new AgvStatus(agv.CorrelationId, null, false, false, agv.AgvStatus!.BatteryCharge, false, false, "node", estimatedFullyChargedTime: DateTime.UtcNow, lastBatteryReplacement: DateTime.UtcNow.AddHours(-5));

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(newStatus);

        batteryChargeChanged.ShouldBeFalse();
        lastNodeIdChanged.ShouldBeTrue();
        agv.AgvStatus.ShouldBe(newStatus);
    }

    [TestMethod]
    public void IsAvailable_IsReadyIdleAndUnlocked_IsAvailable()
    {
        var agv = AgvGenerator.GenerateIdleAgv(isLocked: false, lastNodeId: "node");
        agv.IsAvailable(ignoreLocked: false).ShouldBeTrue();
    }

    [DataRow(true, true)]
    [DataRow(false, false)]
    [TestMethod]
    public void IsAvailable_IsReadyIdleAndLocked_IsAvailableIfLockedIsIgnored(bool ignoreLocked, bool expectedIsAvailable)
    {
        var agv = AgvGenerator.GenerateIdleAgv(isLocked: true, lastNodeId: "node");
        agv.IsAvailable(ignoreLocked: ignoreLocked).ShouldBe(expectedIsAvailable);
    }

    [TestMethod]
    public void IsAvailable_IsNotReady_NotAvailable()
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId");

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
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
        {
            DataExist = false,
            AgvDataRequestedLastTime = null
        };
        agv.ShouldRequestData().ShouldBeTrue();
        agv.AgvDataRequestedLastTime.ShouldNotBeNull();
    }

    [TestMethod]
    public void ShouldRequestGraph_GraphNeverRequestedNoGraph_ShouldRequestGraph()
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
        {
            GraphExists = false,
            AgvGraphRequestedLastTime = null,
            AgvStatus = new AgvStatus()
        };
        agv.ShouldRequestGraph().ShouldBeTrue();
        agv.AgvGraphRequestedLastTime.ShouldNotBeNull();
    }
}