using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions.Charging;

/// <summary>
/// Tests for <see cref="BaseAgvChargingServiceTest"/>
/// </summary>
[TestClass]
public class BaseAgvChargingServiceTest
{
    #region Setup
    private const int BatteryLevelCritical = 10;
    private const int BatteryLevelLow = 40;
    private const int BatteryLevelGood = 80;
    private const int BatteryLevelFull = 100;

    private static readonly AgvSettings _agvSettings = new()
    {
        BatteryLevelCritical = BatteryLevelCritical,
        BatteryLevelFull = BatteryLevelFull,
        BatteryLevelGood = BatteryLevelGood,
        BatteryLevelLow = BatteryLevelLow,
        AgvOfflineTimeoutMsWhileIdle = 15000,
        AgvOfflineTimeoutMsWhileBusy = 20000
    };

    private BaseAgvChargingService GetBaseAgvChargingService(int firstAgvBatteryCharge = BatteryLevelLow, bool isCharging = false)
    {
        var logger = new Mock<ILogger<BaseAgvChargingService>>();
        Mock<IAgvRegistry> agvRegistry = AgvRegistryMock.GetAgvRegistry();
        InitAgvRegistryMock(agvRegistry, firstAgvBatteryCharge, isCharging);

        var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
        settingsProviderMock.Setup(m => m.GetSettings()).Returns(() => _agvSettings);

        return new BaseAgvChargingService(logger.Object, agvRegistry.Object, settingsProviderMock.Object);
    }

    private IAgv CreateAgv(string agvId, int batteryCharge, bool charging)
    {
        var correlationId = agvId.ToGuid();
        var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
        return new AgvSaga(correlationId, agvId, charging ? nameof(AgvStateMachine.Charging) : nameof(AgvStateMachine.Idle), agvStatus);
    }

    private void InitAgvRegistryMock(Mock<IAgvRegistry> agvRegistry, int firstAgvBatteryCharge, bool isCharging = false)
    {
        var agv1 = agvRegistry.Object.GetAgv("AGV001".ToGuid()) as AgvSaga;
        SetBatteryCharge(agv1!, firstAgvBatteryCharge, isCharging);

        var agv2 = agvRegistry.Object.GetAgv("AGV002".ToGuid()) as AgvSaga;
        SetBatteryCharge(agv2!, BatteryLevelGood, isCharging);

        var agv3 = agvRegistry.Object.GetAgv("AGV003".ToGuid()) as AgvSaga;
        agv3!.AgvStatus = null; // Test case for AGV without status
    }

    private void SetBatteryCharge(AgvSaga agv, int batteryCharge, bool isCharging = false)
    {
        if (agv.AgvStatus != null)
        {
            agv.AgvStatus = new AgvStatus(
                agv.AgvStatus.CorrelationId,
                agv.AgvStatus.OrderId,
                agv.AgvStatus.Busy,
                agv.AgvStatus.Automatic,
                batteryCharge,
                isCharging,
                agv.AgvStatus.Disturbed,
                agv.AgvStatus.LastNodeId);
        }
    }
    #endregion

    [TestMethod]
    [DataRow(BatteryLevelCritical, true)]
    [DataRow(BatteryLevelLow, false)]
    [DataRow(BatteryLevelGood, false)]
    [DataRow(BatteryLevelFull, false)]
    public void Test_AgvNeedsCharging(int batteryCharge, bool needsCharging)
    {
        var baseAgvChargingService = GetBaseAgvChargingService();
        var agv = CreateAgv("AGV001", batteryCharge, false);
        baseAgvChargingService.AgvNeedsCharging(agv).ShouldBe(needsCharging);
    }

    [TestMethod]
    [DataRow(BatteryLevelCritical, true)]
    [DataRow(BatteryLevelLow, true)]
    [DataRow(BatteryLevelGood, false)]
    [DataRow(BatteryLevelFull, false)]
    public void Test_AgvIsEligibleForCharging(int batteryCharge, bool needsCharging)
    {
        var baseAgvChargingService = GetBaseAgvChargingService();
        var agv = CreateAgv("AGV001", batteryCharge, false);
        baseAgvChargingService.AgvIsEligibleForCharging(agv).ShouldBe(needsCharging);
    }

    [TestMethod]
    [DataRow(BatteryLevelCritical, new[] { "AGV002" })]
    [DataRow(BatteryLevelLow, new[] { "AGV002" })]
    [DataRow(BatteryLevelGood, new[] { "AGV001", "AGV002" })]
    [DataRow(BatteryLevelFull, new[] { "AGV001", "AGV002" })]
    public void Test_GetChargingAgvsWithGoodBatteryLevel(int firstAgvBatteryCharge, string[] chargedAgvs)
    {
        var baseAgvChargingService = GetBaseAgvChargingService(firstAgvBatteryCharge, true);

        List<IAgv> agvList = baseAgvChargingService.GetChargingAgvsWithGoodBatteryLevel();
        agvList.Count.ShouldBe(chargedAgvs.Length);

        for (int i = 0; i < agvList.Count; i++)
        {
            agvList[i].AgvId.ShouldBe(chargedAgvs[i]);
        }
    }
}