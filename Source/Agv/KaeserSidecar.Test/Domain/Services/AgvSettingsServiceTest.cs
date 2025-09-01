using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Services;

[TestClass]
public class AgvSettingsServiceTest
{
    [TestMethod]
    [DataRow(-1, true)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    public async Task CheckAgvHasCriticalBatteryLevel_BatteryLevelSmallerThanOrEqualLevelCritical_ReturnsExpectedValue(int increment, bool expectedResult)
    {
        const int critical = 10;

        var agvSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: 12,
            agvOfflineTimeoutMsWhileIdle: 12,
            maxChargeUp: 100,
            minChargeUp: 20,
            batteryLevelLow: 40,
            batteryLevelCritical: critical);
        var agv = AgvGenerator.GenerateIdleAgv(1, null, critical + increment);

        var fixture = new AgvSettingsServiceTestFixture()
            .WithAgv(agv)
            .WithSettings(agvSettings);

        (await fixture.Service.CheckAgvHasCriticalBatteryLevel(agv.CorrelationId)).ShouldBe(expectedResult);
    }
}