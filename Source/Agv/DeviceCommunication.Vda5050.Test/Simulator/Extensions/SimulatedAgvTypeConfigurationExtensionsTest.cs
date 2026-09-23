using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Extensions.SimulatedAgvTypeConfigurationExtensionsTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Extensions;

[TestClass]
public class SimulatedAgvTypeConfigurationExtensionsTest
{
    [TestMethod]
    public void ToAgvConfiguration_ValidData_CopiesConfigurationValues()
    {
        var configuration = new Fixture().Sut;

        var agvConfiguration = configuration.ToAgvConfiguration(Fixture.FirstSerialNumber, Fixture.MapId);

        agvConfiguration.ShouldSatisfyAllConditions(
            () => agvConfiguration.SerialNumber.ShouldBe(Fixture.FirstSerialNumber),
            () => agvConfiguration.MapId.ShouldBe(Fixture.MapId),
            () => agvConfiguration.Manufacturer.ShouldBe(Fixture.Manufacturer),
            () => agvConfiguration.AgvTypeId.ShouldBe(configuration.Id),
            () => agvConfiguration.StateUpdateIntervalMs.ShouldBe(Fixture.StateUpdateIntervalMs),
            () => agvConfiguration.VisualizationUpdateIntervalMs.ShouldBe(Fixture.VisualizationUpdateIntervalMs),
            () => agvConfiguration.BatteryDischargeRate.ShouldBe(Fixture.BatteryDischargeRate),
            () => agvConfiguration.BatteryChargeRate.ShouldBe(Fixture.BatteryChargeRate));
    }

    [TestMethod]
    [DataRow(Fixture.FirstSerialNumber, Fixture.FirstSpreadBatteryCharge)]
    [DataRow(Fixture.SecondSerialNumber, Fixture.SecondSpreadBatteryCharge)]
    [DataRow(Fixture.ThirdSerialNumber, Fixture.ThirdSpreadBatteryCharge)]
    [DataRow(Fixture.FourthSerialNumber, Fixture.FourthSpreadBatteryCharge)]
    public void ToAgvConfiguration_ConsecutiveSerialNumbers_SpreadsInitialBatteryChargeAroundConfiguredCharge(string serialNumber, int expectedInitialBatteryCharge)
    {
        var configuration = new Fixture().Sut;

        var agvConfiguration = configuration.ToAgvConfiguration(serialNumber, Fixture.MapId);

        agvConfiguration.InitialBatteryCharge.ShouldBe(expectedInitialBatteryCharge);
    }

    [TestMethod]
    public void ToAgvConfiguration_SerialNumberBeyondSpread_RestartsSpreadAtConfiguredCharge()
    {
        var configuration = new Fixture().Sut;

        var agvConfiguration = configuration.ToAgvConfiguration(Fixture.SerialNumberBeyondSpread, Fixture.MapId);

        agvConfiguration.InitialBatteryCharge.ShouldBe(Fixture.FirstSpreadBatteryCharge);
    }

    [TestMethod]
    public void ToAgvConfiguration_SerialNumberWithoutIndex_UsesConfiguredInitialBatteryCharge()
    {
        var configuration = new Fixture().Sut;

        var agvConfiguration = configuration.ToAgvConfiguration(Fixture.SerialNumberWithoutIndex, Fixture.MapId);

        agvConfiguration.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge);
    }

    [TestMethod]
    public void ToAgvConfiguration_NoInitialBatteryChargeDeviation_UsesConfiguredInitialBatteryChargeForEveryAgv()
    {
        var configuration = new Fixture()
            .WithInitialBatteryChargeDeviation(Fixture.NoBatteryChargeDeviation)
            .Sut;

        var agvConfiguration = configuration.ToAgvConfiguration(Fixture.SecondSerialNumber, Fixture.MapId);

        agvConfiguration.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge);
    }

    [TestMethod]
    public void ApplySettings_AllSettings_AppliesEverySetting()
    {
        var configuration = new Fixture().Sut;

        var updated = configuration.ApplySettings(new AgvTypeSimulationSettings
        {
            NoOfAgvs = Fixture.ChangedNoOfAgvs,
            StateUpdateIntervalMs = Fixture.ChangedStateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = Fixture.ChangedVisualizationUpdateIntervalMs,
            BatteryDischargeRate = Fixture.ChangedBatteryDischargeRate,
            BatteryChargeRate = Fixture.ChangedBatteryChargeRate,
            InitialBatteryCharge = Fixture.ChangedInitialBatteryCharge,
            InitialBatteryChargeDeviation = Fixture.ChangedInitialBatteryChargeDeviation
        });

        updated.ShouldSatisfyAllConditions(
            () => updated.NoOfAgvs.ShouldBe(Fixture.ChangedNoOfAgvs),
            () => updated.StateUpdateIntervalMs.ShouldBe(Fixture.ChangedStateUpdateIntervalMs),
            () => updated.VisualizationUpdateIntervalMs.ShouldBe(Fixture.ChangedVisualizationUpdateIntervalMs),
            () => updated.BatteryDischargeRate.ShouldBe(Fixture.ChangedBatteryDischargeRate),
            () => updated.BatteryChargeRate.ShouldBe(Fixture.ChangedBatteryChargeRate),
            () => updated.InitialBatteryCharge.ShouldBe(Fixture.ChangedInitialBatteryCharge),
            () => updated.InitialBatteryChargeDeviation.ShouldBe(Fixture.ChangedInitialBatteryChargeDeviation));
    }

    [TestMethod]
    public void ApplySettings_OmittedSettings_KeepsCurrentValues()
    {
        var configuration = new Fixture()
            .WithNoOfAgvs(Fixture.NoOfAgvs)
            .Sut;

        var updated = configuration.ApplySettings(new AgvTypeSimulationSettings());

        updated.ShouldBe(configuration);
    }

    [TestMethod]
    public void ApplySettings_SingleSetting_KeepsOmittedValues()
    {
        var configuration = new Fixture()
            .WithNoOfAgvs(Fixture.NoOfAgvs)
            .Sut;

        var updated = configuration.ApplySettings(new AgvTypeSimulationSettings { NoOfAgvs = Fixture.ChangedNoOfAgvs });

        updated.ShouldSatisfyAllConditions(
            () => updated.NoOfAgvs.ShouldBe(Fixture.ChangedNoOfAgvs),
            () => updated.StateUpdateIntervalMs.ShouldBe(Fixture.StateUpdateIntervalMs),
            () => updated.VisualizationUpdateIntervalMs.ShouldBe(Fixture.VisualizationUpdateIntervalMs),
            () => updated.BatteryDischargeRate.ShouldBe(Fixture.BatteryDischargeRate),
            () => updated.BatteryChargeRate.ShouldBe(Fixture.BatteryChargeRate),
            () => updated.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge),
            () => updated.InitialBatteryChargeDeviation.ShouldBe(Fixture.InitialBatteryChargeDeviation));
    }

    [TestMethod]
    public void ApplySettings_ValidData_KeepsAgvTypeIdentity()
    {
        var configuration = new Fixture().Sut;

        var updated = configuration.ApplySettings(new AgvTypeSimulationSettings { NoOfAgvs = Fixture.ChangedNoOfAgvs });

        updated.Id.ShouldBe(configuration.Id);
        updated.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
    }
}