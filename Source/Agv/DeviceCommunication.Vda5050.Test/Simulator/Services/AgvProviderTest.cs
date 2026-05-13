using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class AgvProviderTest
{
    private static Options GetOptions(int numberOfAgvs = 1) => new()
    {
        Agvs = numberOfAgvs,
        SerialNumberBase = "simagv",
        MapId = "map",
        Manufacturer = "gebhardt",
        InitialBatteryCharge = 75,
        InitialBatteryChargeDeviation = 0,
        BatteryChargeRate = 1.0,
        BatteryDischargeRate = 0.1,
        VisualizationUpdateInterval = 60_000,
        StateUpdateInterval = 60_000
    };

    [TestMethod]
    public async Task AgvProvider_Execute_CreateAgv_Published()
    {
        var options = GetOptions();
        var fixture = new AgvProviderTestFixture()
            .WithOptions(options)
            .Build();

        await fixture.RunBackgroundService();

        (await fixture.SagaHarness.Consumed.Any<CreateAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task TwoAgvs_Execute_CreateAgvPublished_AgvIdUnique_Successful()
    {
        var options = GetOptions(2);
        var fixture = new AgvProviderTestFixture()
            .WithOptions(options)
            .Build();

        await fixture.RunBackgroundService();

        (await fixture.SagaHarness.Consumed.Any<CreateAgv>()).ShouldBeTrue();

        for (int i = 0; i < options.Agvs; i++)
        {
            string serialNumber = $"{options.SerialNumberBase}{i}";
            var correlationId = serialNumber.ToGuid();

            var createdSaga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            createdSaga.CorrelationId.ShouldBe(correlationId);
            createdSaga.Configuration.SerialNumber.ShouldBe(serialNumber);
        }
    }
}
