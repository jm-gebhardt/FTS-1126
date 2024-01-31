using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

/// <summary>
/// Tests for <see cref="BaseAgvChargingServiceTest"/>
/// </summary>
[TestClass]
public class BaseAgvChargingServiceTest
{
    #region Setup
    private const int BatteryLevelCritical = 10;
    private const int BatteryLevelLow = 40;
    private const int MinChargeUp = 80;
    private const int MaxChargeUp = 100;

    private static readonly AgvSettings _agvSettings = new()
    {
        BatteryLevelCritical = BatteryLevelCritical,
        MaxChargeUp = MaxChargeUp,
        BatteryLevelLow = BatteryLevelLow,
        AgvOfflineTimeoutMsWhileIdle = 15000,
        AgvOfflineTimeoutMsWhileBusy = 20000
    };

    private TransportOrder.Domain.Models.Agv.Agv CreateAgv(string serialNumber, int batteryCharge, bool charging)
    {
        var correlationId = serialNumber.ToGuid();
        var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
        var agv = new TransportOrder.Domain.Models.Agv.Agv(correlationId, serialNumber, agvStatus);
        agv = AgvGenerator.SetAgvState(charging ? AgvState.Busy : AgvState.Idle, agv);
        return agv;
    }
    #endregion

    [TestMethod]
    [DataRow(BatteryLevelCritical, true)]
    [DataRow(BatteryLevelLow, false)]
    [DataRow(MinChargeUp, false)]
    [DataRow(MaxChargeUp, false)]
    public void Test_AgvNeedsCharging(int batteryCharge, bool needsCharging)
    {
        var agv = CreateAgv("AGV001", batteryCharge, false);
        agv.CheckMustCharge(_agvSettings).ShouldBe(needsCharging);
    }

    [TestMethod]
    [DataRow(BatteryLevelCritical, true)]
    [DataRow(BatteryLevelLow, true)]
    [DataRow(MaxChargeUp, false)]
    public void Test_AgvIsEligibleForCharging(int batteryCharge, bool needsCharging)
    {
        var agv = CreateAgv("AGV001", batteryCharge, false);
        agv.CheckShouldCharge(_agvSettings).ShouldBe(needsCharging);
    }
}