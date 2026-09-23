using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles.AgvTypeConfigurationProfileTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles;

[TestClass]
public class AgvTypeConfigurationProfileTest
{
    [TestMethod]
    public void Map_SimulatedAgvTypeConfigurationToDto_MapsConfigurationValues()
    {
        var fixture = new Fixture();
        var configuration = fixture.Configuration;

        var dto = fixture.Mapper.Map<AgvTypeConfigurationDto>(configuration);

        dto.ShouldSatisfyAllConditions(
            () => dto.AgvTypeId.ShouldBe(configuration.Id),
            () => dto.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier),
            () => dto.Manufacturer.ShouldBe(Fixture.Manufacturer),
            () => dto.SeriesName.ShouldBe(Fixture.SeriesName),
            () => dto.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs),
            () => dto.StateUpdateIntervalMs.ShouldBe(Fixture.StateUpdateIntervalMs),
            () => dto.VisualizationUpdateIntervalMs.ShouldBe(Fixture.VisualizationUpdateIntervalMs),
            () => dto.BatteryDischargeRate.ShouldBe(Fixture.BatteryDischargeRate),
            () => dto.BatteryChargeRate.ShouldBe(Fixture.BatteryChargeRate),
            () => dto.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge),
            () => dto.InitialBatteryChargeDeviation.ShouldBe(Fixture.InitialBatteryChargeDeviation));
    }

    [TestMethod]
    public void Map_SimulatedAgvTypeConfigurationToDto_MapsSerialNumbersOfSimulatedAgvs()
    {
        var fixture = new Fixture();

        var dto = fixture.Mapper.Map<AgvTypeConfigurationDto>(fixture.Configuration);

        dto.SerialNumbers.ShouldBe([Fixture.FirstSerialNumber, Fixture.SecondSerialNumber]);
    }

    [TestMethod]
    public void Map_SimulatedAgvTypeConfigurationToDto_NoAgvs_MapsEmptySerialNumbers()
    {
        var fixture = new Fixture().WithNoOfAgvs(Fixture.NoAgvs);

        var dto = fixture.Mapper.Map<AgvTypeConfigurationDto>(fixture.Configuration);

        dto.SerialNumbers.ShouldBeEmpty();
    }

    [TestMethod]
    public void Map_AddAgvTypeConfigurationDtoToSettings_MapsEverySetting()
    {
        var fixture = new Fixture();

        var settings = fixture.Mapper.Map<AgvTypeSimulationSettings>(new AddAgvTypeConfigurationDto
        {
            AgvTypeIdentifier = Fixture.AgvTypeIdentifier,
            NoOfAgvs = Fixture.NoOfAgvs,
            StateUpdateIntervalMs = Fixture.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = Fixture.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = Fixture.BatteryDischargeRate,
            BatteryChargeRate = Fixture.BatteryChargeRate,
            InitialBatteryCharge = Fixture.InitialBatteryCharge,
            InitialBatteryChargeDeviation = Fixture.InitialBatteryChargeDeviation
        });

        settings.ShouldSatisfyAllConditions(
            () => settings.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs),
            () => settings.StateUpdateIntervalMs.ShouldBe(Fixture.StateUpdateIntervalMs),
            () => settings.VisualizationUpdateIntervalMs.ShouldBe(Fixture.VisualizationUpdateIntervalMs),
            () => settings.BatteryDischargeRate.ShouldBe(Fixture.BatteryDischargeRate),
            () => settings.BatteryChargeRate.ShouldBe(Fixture.BatteryChargeRate),
            () => settings.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge),
            () => settings.InitialBatteryChargeDeviation.ShouldBe(Fixture.InitialBatteryChargeDeviation));
    }

    [TestMethod]
    public void Map_UpdateAgvTypeConfigurationDtoToSettings_MapsEverySetting()
    {
        var fixture = new Fixture();

        var settings = fixture.Mapper.Map<AgvTypeSimulationSettings>(new UpdateAgvTypeConfigurationDto
        {
            NoOfAgvs = Fixture.NoOfAgvs,
            StateUpdateIntervalMs = Fixture.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = Fixture.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = Fixture.BatteryDischargeRate,
            BatteryChargeRate = Fixture.BatteryChargeRate,
            InitialBatteryCharge = Fixture.InitialBatteryCharge,
            InitialBatteryChargeDeviation = Fixture.InitialBatteryChargeDeviation
        });

        settings.ShouldSatisfyAllConditions(
            () => settings.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs),
            () => settings.StateUpdateIntervalMs.ShouldBe(Fixture.StateUpdateIntervalMs),
            () => settings.VisualizationUpdateIntervalMs.ShouldBe(Fixture.VisualizationUpdateIntervalMs),
            () => settings.BatteryDischargeRate.ShouldBe(Fixture.BatteryDischargeRate),
            () => settings.BatteryChargeRate.ShouldBe(Fixture.BatteryChargeRate),
            () => settings.InitialBatteryCharge.ShouldBe(Fixture.InitialBatteryCharge),
            () => settings.InitialBatteryChargeDeviation.ShouldBe(Fixture.InitialBatteryChargeDeviation));
    }

    [TestMethod]
    public void Map_UpdateAgvTypeConfigurationDtoToSettings_OmittedProperties_MapsUnsetSettings()
    {
        var fixture = new Fixture();

        var settings = fixture.Mapper.Map<AgvTypeSimulationSettings>(new UpdateAgvTypeConfigurationDto { NoOfAgvs = Fixture.NoOfAgvs });

        settings.ShouldSatisfyAllConditions(
            () => settings.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs),
            () => settings.StateUpdateIntervalMs.ShouldBeNull(),
            () => settings.VisualizationUpdateIntervalMs.ShouldBeNull(),
            () => settings.BatteryDischargeRate.ShouldBeNull(),
            () => settings.BatteryChargeRate.ShouldBeNull(),
            () => settings.InitialBatteryCharge.ShouldBeNull(),
            () => settings.InitialBatteryChargeDeviation.ShouldBeNull());
    }
}