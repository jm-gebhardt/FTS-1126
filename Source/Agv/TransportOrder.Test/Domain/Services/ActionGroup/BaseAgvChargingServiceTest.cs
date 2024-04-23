using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

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

    private static readonly AgvSettingsReadModel _agvSettingsReadModel = new(1500, 2000, BatteryLevelCritical, BatteryLevelLow, MinChargeUp, MaxChargeUp);

    private Agv CreateAgv(string serialNumber, int batteryCharge, bool charging)
    {
        var correlationId = serialNumber.ToGuid();
        var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
        var agv = new Agv(correlationId, Guid.NewGuid(), serialNumber, agvStatus);
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
        agv.CheckMustCharge(_agvSettingsReadModel).ShouldBe(needsCharging);
    }

    [TestMethod]
    [DataRow(BatteryLevelCritical, true)]
    [DataRow(BatteryLevelLow, true)]
    [DataRow(MaxChargeUp, false)]
    public void Test_AgvIsEligibleForCharging(int batteryCharge, bool needsCharging)
    {
        var agv = CreateAgv("AGV001", batteryCharge, false);
        agv.CheckShouldCharge(_agvSettingsReadModel).ShouldBe(needsCharging);
    }
}