using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Services;

[TestClass]
public class AgvSettingsServiceTest
{
    [DataTestMethod]
    [DataRow(-1, true)]
    [DataRow(0, false)]
    [DataRow(1, false)]
    public async Task CheckAgvHasLowBatteryLevel_BatteryLevelSmallerThanLevelLow_ReturnsExpectedValue(int increment, bool expectedResult)
    {
        const int low = 30;

        var agvSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: 12,
            agvOfflineTimeoutMsWhileIdle: 12,
            maxChargeUp: 100,
            minChargeUp: 20,
            batteryLevelLow: low,
            batteryLevelCritical: 1);
        var agv = AgvGenerator.GenerateIdleAgv(1, null, low + increment);

        var fixture = new AgvSettingsServiceTestFixture()
            .WithAgv(agv)
            .WithSettings(agvSettings);

        (await fixture.Service.CheckAgvHasLowBatteryLevel(agv.CorrelationId)).ShouldBe(expectedResult);
    }
}